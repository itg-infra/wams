.PHONY: run build test test-coverage watch migrate-add migrate-up docker-up docker-down clean swagger-gen run-e2e-api e2e-reset e2e e2e-full e2e-full-published e2e-seed e2e-full-volume e2e-stability e2e-concurrency e2e-concurrency-ap e2e-concurrency-apdp e2e-concurrency-create e2e-concurrency-approval e2e-picker-consistency

run:
	@set -a && . ./.env && set +a && dotnet run --project src/WAMS.Api --no-launch-profile

# Run API with optional e2e/app.env overrides (e.g., test DB connection).
# e2e/app.env should define ConnectionStrings__DefaultConnection for test database.
# Does NOT reset the database - run `make e2e-reset` first for a clean DB.
run-e2e-api:
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && dotnet run --project src/WAMS.Api --no-launch-profile

# Reset + migrate + seed the e2e Postgres database via the standalone tool.
# Must run before run-e2e-api / any e2e-full* or e2e-concurrency* target now
# that the API no longer resets its own database on startup.
e2e-reset:
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	dotnet run --project tools/WAMS.E2eReset --no-launch-profile

# Run e2e tests (API must already be running via run-e2e-api)
# Usage: make e2e
#        make e2e SKIP=18          # skip 18-sync.hurl
#        make e2e SKIP=18-sync     # same, more explicit
e2e:
	@./e2e/run.sh $(if $(SKIP),--skip=$(SKIP))

# Full e2e cycle: start API, wait for health, run tests, stop API
# Usage: make e2e-full
#        make e2e-full SKIP=18
e2e-full:
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	( dotnet run --project src/WAMS.Api --no-launch-profile & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	./e2e/run.sh $(if $(SKIP),--skip=$(SKIP)); CODE=$$?; kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; exit $$CODE )

# Full e2e cycle using a published binary (ReadyToRun applies - no JIT cold-start).
# Usage: make e2e-full-published SKIP=18
e2e-full-published:
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	dotnet publish src/WAMS.Api -c Release -r linux-x64 --self-contained false -o /tmp/wams-e2e-publish --verbosity minimal /p:PublishReadyToRun=true && \
	( dotnet /tmp/wams-e2e-publish/WAMS.Api.dll & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	./e2e/run.sh $(if $(SKIP),--skip=$(SKIP)); CODE=$$?; kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; exit $$CODE )

# Seed volume data into e2e DB (API must NOT be running - direct psql to test DB)
# Usage: make e2e-seed SCALE=medium
#        make e2e-seed SCALE=large DRY_RUN=true
#        make e2e-seed SCALE=medium FORCE=true
e2e-seed:
	@SCALE=$(SCALE) ./e2e/seed_volume.sh \
	$(if $(filter true,$(DRY_RUN)),--dry-run) \
	$(if $(filter true,$(FORCE)),--force)

# Full e2e cycle with volume seed: reset → publish → start API → health check → seed → run hurl
# Report written to e2e/reports/volume.md (seed counts prepended, then hurl results).
# SCALE defaults to medium if not specified.
# Usage: make e2e-full-volume SCALE=large
e2e-full-volume:
	@fuser -k 8080/tcp 2>/dev/null || true; sleep 1
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	dotnet publish src/WAMS.Api -c Release -r linux-x64 --self-contained false -o /tmp/wams-e2e-publish --verbosity minimal /p:PublishReadyToRun=true && \
	( dotnet /tmp/wams-e2e-publish/WAMS.Api.dll & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	SCALE=$(or $(SCALE),medium) ./e2e/seed_volume.sh --summary-out=/tmp/wams-seed-summary.md && \
	./e2e/run.sh --out=./e2e/reports/volume.md $(if $(SKIP),--skip=$(SKIP)); CODE=$$?; \
	if [ -f /tmp/wams-seed-summary.md ] && [ -f ./e2e/reports/volume.md ]; then \
	  cat /tmp/wams-seed-summary.md ./e2e/reports/volume.md > /tmp/wams-vol-final.md && \
	  mv /tmp/wams-vol-final.md ./e2e/reports/volume.md; \
	fi; \
	kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; exit $$CODE )

# Show stability report across all recorded e2e runs
e2e-stability:
	@python3 e2e/show_history.py e2e/reports/history.jsonl

# Concurrency race test: N parallel POST /purchase-orders/{id}/generate on the
# same draft PO, checking for the TOCTOU double-SAP-submit race. Publishes,
# starts API, runs a small volume seed (only needed for its shared vendor/
# item/uom/warehouse shadow rows), applies a minimal race fixture on top (one
# Approved budget plan + item under the GCU company -- the volume seed's own
# budget plans land under a separate "VOL" company and are invisible to a
# GCU login), runs the k6 race script, then tears down.
# Usage: make e2e-concurrency RACE_VUS=20
e2e-concurrency:
	@fuser -k 8080/tcp 2>/dev/null || true; sleep 1
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	dotnet publish src/WAMS.Api -c Release -r linux-x64 --self-contained false -o /tmp/wams-e2e-publish --verbosity minimal /p:PublishReadyToRun=true && \
	( dotnet /tmp/wams-e2e-publish/WAMS.Api.dll & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	SCALE=small ./e2e/seed_volume.sh --force && \
	CONN=$$(grep -E '^ConnectionStrings__DefaultConnection=' ./e2e/app.env | cut -d= -f2- | tr -d '"'); \
	PGHOST=$$(echo "$$CONN" | grep -oP 'Host=\K[^;]+'); \
	PGPORT=$$(echo "$$CONN" | grep -oP 'Port=\K[^;]+'); \
	PGDATABASE=$$(echo "$$CONN" | grep -oP 'Database=\K[^;]+'); \
	PGUSER=$$(echo "$$CONN" | grep -oP 'Username=\K[^;]+'); \
	export PGPASSWORD=$$(echo "$$CONN" | grep -oP 'Password=\K[^;]+'); \
	psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -v ON_ERROR_STOP=1 -f e2e/concurrency/seed_race_fixture.sql && \
	VARS_ENV=./e2e/vars.env; \
	get_var() { grep -E "^$$1=" "$$VARS_ENV" | head -1 | cut -d= -f2-; }; \
	k6 run \
	  -e BASE_URL=$$(get_var BASE_URL) \
	  -e SA_EMAIL=$$(get_var SA_EMAIL) \
	  -e SA_PASSWORD=$$(get_var SA_PASSWORD) \
	  -e E2E_COMPANY_CODE=$$(get_var E2E_COMPANY_CODE) \
	  -e RACE_VUS=$(or $(RACE_VUS),10) \
		e2e/concurrency/po-generate-race.js; CODE=$$?; \
		kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; exit $$CODE )

# Concurrency race test: N parallel POST /account-payables/{id}/generate on
# the same draft AP. The RFBA mode first creates a generated PO and its
# standalone APDP, so this test now exercises only the AP invoice claim race.
# APDP claim concurrency is covered by e2e-concurrency-apdp below.
# Usage: make e2e-concurrency-ap RACE_VUS=20
e2e-concurrency-ap:
	@fuser -k 8080/tcp 2>/dev/null || true; sleep 1
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	dotnet publish src/WAMS.Api -c Release -r linux-x64 --self-contained false -o /tmp/wams-e2e-publish --verbosity minimal /p:PublishReadyToRun=true && \
	( dotnet /tmp/wams-e2e-publish/WAMS.Api.dll & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	CONN=$$(grep -E '^ConnectionStrings__DefaultConnection=' ./e2e/app.env | cut -d= -f2- | tr -d '"'); \
	PGHOST=$$(echo "$$CONN" | grep -oP 'Host=\K[^;]+'); \
	PGPORT=$$(echo "$$CONN" | grep -oP 'Port=\K[^;]+'); \
	PGDATABASE=$$(echo "$$CONN" | grep -oP 'Database=\K[^;]+'); \
	PGUSER=$$(echo "$$CONN" | grep -oP 'Username=\K[^;]+'); \
	export PGPASSWORD=$$(echo "$$CONN" | grep -oP 'Password=\K[^;]+'); \
	until [ "$$(psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -Atc 'select count(*) from item_shadows' 2>/dev/null)" -gt 0 ] && [ "$$(psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -Atc 'select count(*) from vendor_shadows' 2>/dev/null)" -gt 0 ]; do sleep 1; done; \
	if ! psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -v ON_ERROR_STOP=1 -f e2e/concurrency/seed_ap_race_fixture.sql; then exit 1; fi; \
	VARS_ENV=./e2e/vars.env; \
	get_var() { grep -E "^$$1=" "$$VARS_ENV" | head -1 | cut -d= -f2-; }; \
	K6ARGS="-e BASE_URL=$$(get_var BASE_URL) -e SA_EMAIL=$$(get_var SA_EMAIL) -e SA_PASSWORD=$$(get_var SA_PASSWORD) -e E2E_COMPANY_CODE=$$(get_var E2E_COMPANY_CODE) -e RACE_VUS=$(or $(RACE_VUS),10)"; \
	echo "=== isRfba=false ==="; \
	k6 run $$K6ARGS -e ISRFBA=false e2e/concurrency/ap-generate-race.js; CODE1=$$?; \
	echo "=== isRfba=true ==="; \
	k6 run $$K6ARGS -e ISRFBA=true e2e/concurrency/ap-generate-race.js; CODE2=$$?; \
	kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; \
	if [ $$CODE1 -ne 0 ] || [ $$CODE2 -ne 0 ]; then exit 99; fi )

# Concurrency race test: N parallel POST /purchase-orders/{id}/generate-apdp
# on the same generated RFBA PO. The endpoint is idempotent: concurrent callers
# may receive 409 while the winner is processing or 200 with the same persisted
# APDP. Uses the same isolated E2E database and waits for startup master-data
# sync to restore the shared shadow rows before applying its fixture.
# Usage: make e2e-concurrency-apdp RACE_VUS=20
e2e-concurrency-apdp:
	@fuser -k 8080/tcp 2>/dev/null || true; sleep 1
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	dotnet publish src/WAMS.Api -c Release -r linux-x64 --self-contained false -o /tmp/wams-e2e-publish --verbosity minimal /p:PublishReadyToRun=true && \
	( dotnet /tmp/wams-e2e-publish/WAMS.Api.dll & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	CONN=$$(grep -E '^ConnectionStrings__DefaultConnection=' ./e2e/app.env | cut -d= -f2- | tr -d '"'); \
	PGHOST=$$(echo "$$CONN" | grep -oP 'Host=\K[^;]+'); \
	PGPORT=$$(echo "$$CONN" | grep -oP 'Port=\K[^;]+'); \
	PGDATABASE=$$(echo "$$CONN" | grep -oP 'Database=\K[^;]+'); \
	PGUSER=$$(echo "$$CONN" | grep -oP 'Username=\K[^;]+'); \
	export PGPASSWORD=$$(echo "$$CONN" | grep -oP 'Password=\K[^;]+'); \
	until [ "$$(psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -Atc 'select count(*) from item_shadows' 2>/dev/null)" -gt 0 ] && [ "$$(psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -Atc 'select count(*) from vendor_shadows' 2>/dev/null)" -gt 0 ]; do sleep 1; done && \
	psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -v ON_ERROR_STOP=1 -f e2e/concurrency/seed_ap_race_fixture.sql && \
	VARS_ENV=./e2e/vars.env; \
	get_var() { grep -E "^$$1=" "$$VARS_ENV" | head -1 | cut -d= -f2-; }; \
	k6 run \
	  -e BASE_URL=$$(get_var BASE_URL) \
	  -e SA_EMAIL=$$(get_var SA_EMAIL) \
	  -e SA_PASSWORD=$$(get_var SA_PASSWORD) \
	  -e E2E_COMPANY_CODE=$$(get_var E2E_COMPANY_CODE) \
	  -e RACE_VUS=$(or $(RACE_VUS),10) \
		e2e/concurrency/apdp-generate-race.js; CODE=$$?; \
		kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; exit $$CODE )

# Concurrency race test: N parallel POST /purchase-orders, all requesting
# the same BudgetPlanItem, checking the Fix-3 double-attachment race.
# Usage: make e2e-concurrency-create RACE_VUS=20
e2e-concurrency-create:
	@fuser -k 8080/tcp 2>/dev/null || true; sleep 1
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	dotnet publish src/WAMS.Api -c Release -r linux-x64 --self-contained false -o /tmp/wams-e2e-publish --verbosity minimal /p:PublishReadyToRun=true && \
	( dotnet /tmp/wams-e2e-publish/WAMS.Api.dll & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	SCALE=small ./e2e/seed_volume.sh --force && \
	CONN=$$(grep -E '^ConnectionStrings__DefaultConnection=' ./e2e/app.env | cut -d= -f2- | tr -d '"'); \
	PGHOST=$$(echo "$$CONN" | grep -oP 'Host=\K[^;]+'); \
	PGPORT=$$(echo "$$CONN" | grep -oP 'Port=\K[^;]+'); \
	PGDATABASE=$$(echo "$$CONN" | grep -oP 'Database=\K[^;]+'); \
	PGUSER=$$(echo "$$CONN" | grep -oP 'Username=\K[^;]+'); \
	export PGPASSWORD=$$(echo "$$CONN" | grep -oP 'Password=\K[^;]+'); \
	psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -v ON_ERROR_STOP=1 -f e2e/concurrency/seed_create_race_fixture.sql && \
	psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -v ON_ERROR_STOP=1 -f e2e/concurrency/seed_ap_create_race_fixture.sql && \
	VARS_ENV=./e2e/vars.env; \
	get_var() { grep -E "^$$1=" "$$VARS_ENV" | head -1 | cut -d= -f2-; }; \
	K6ARGS="-e BASE_URL=$$(get_var BASE_URL) -e SA_EMAIL=$$(get_var SA_EMAIL) -e SA_PASSWORD=$$(get_var SA_PASSWORD) -e E2E_COMPANY_CODE=$$(get_var E2E_COMPANY_CODE) -e RACE_VUS=$(or $(RACE_VUS),10)"; \
	echo "=== PO create race ==="; \
	k6 run $$K6ARGS e2e/concurrency/po-create-race.js; CODE1=$$?; \
	echo "=== AP create race ==="; \
	k6 run $$K6ARGS e2e/concurrency/ap-create-race.js; CODE2=$$?; \
	kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; \
	if [ $$CODE1 -ne 0 ] || [ $$CODE2 -ne 0 ]; then exit 99; fi )

# Races one POST /budget-plans/{id}/approve against one POST
# /budget-plans/{id}/reject on the same seeded budget plan, to check that
# UnitOfWork.CommitAsync's DbUpdateConcurrencyException -> ConflictException
# translation (Task 10) turns the loser of the race into a clean 409 instead
# of an unhandled 500.
# Usage: make e2e-concurrency-approval
e2e-concurrency-approval:
	@fuser -k 8080/tcp 2>/dev/null || true; sleep 1
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	dotnet publish src/WAMS.Api -c Release -r linux-x64 --self-contained false -o /tmp/wams-e2e-publish --verbosity minimal /p:PublishReadyToRun=true && \
	( dotnet /tmp/wams-e2e-publish/WAMS.Api.dll & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	SCALE=small ./e2e/seed_volume.sh --force && \
	CONN=$$(grep -E '^ConnectionStrings__DefaultConnection=' ./e2e/app.env | cut -d= -f2- | tr -d '"'); \
	PGHOST=$$(echo "$$CONN" | grep -oP 'Host=\K[^;]+'); \
	PGPORT=$$(echo "$$CONN" | grep -oP 'Port=\K[^;]+'); \
	PGDATABASE=$$(echo "$$CONN" | grep -oP 'Database=\K[^;]+'); \
	PGUSER=$$(echo "$$CONN" | grep -oP 'Username=\K[^;]+'); \
	export PGPASSWORD=$$(echo "$$CONN" | grep -oP 'Password=\K[^;]+'); \
	psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -v ON_ERROR_STOP=1 -f e2e/concurrency/seed_approve_reject_race_fixture.sql && \
	VARS_ENV=./e2e/vars.env; \
	get_var() { grep -E "^$$1=" "$$VARS_ENV" | head -1 | cut -d= -f2-; }; \
	K6ARGS="-e BASE_URL=$$(get_var BASE_URL) -e SA_EMAIL=$$(get_var SA_EMAIL) -e SA_PASSWORD=$$(get_var SA_PASSWORD) -e E2E_COMPANY_CODE=$$(get_var E2E_COMPANY_CODE)"; \
	echo "=== Budget plan approve/reject race ==="; \
	k6 run $$K6ARGS e2e/concurrency/budget-plan-approve-reject-race.js; CODE=$$?; \
	kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; \
	if [ $$CODE -ne 0 ]; then exit 99; fi )

# End-to-end proof that the /available-items picker and the create/update
# validation path agree over real Postgres (the SQLite parity tests prove
# the two LINQ trees agree; this proves the HTTP layer wires
# excludePurchaseOrderId through). Single-VU, sequential, no race -- reuses
# the create-race fixture (one Approved budget plan + item under GCU).
# Usage: make e2e-picker-consistency
e2e-picker-consistency:
	@fuser -k 8080/tcp 2>/dev/null || true; sleep 1
	@set -a && . ./.env && if [ -f ./e2e/app.env ]; then . ./e2e/app.env; fi && set +a && \
	$(MAKE) e2e-reset && \
	dotnet publish src/WAMS.Api -c Release -r linux-x64 --self-contained false -o /tmp/wams-e2e-publish --verbosity minimal /p:PublishReadyToRun=true && \
	( dotnet /tmp/wams-e2e-publish/WAMS.Api.dll & API_PID=$$!; \
	until curl -sf http://localhost:8080/health >/dev/null 2>&1; do sleep 1; done && \
	SCALE=small ./e2e/seed_volume.sh --force && \
	CONN=$$(grep -E '^ConnectionStrings__DefaultConnection=' ./e2e/app.env | cut -d= -f2- | tr -d '"'); \
	PGHOST=$$(echo "$$CONN" | grep -oP 'Host=\K[^;]+'); \
	PGPORT=$$(echo "$$CONN" | grep -oP 'Port=\K[^;]+'); \
	PGDATABASE=$$(echo "$$CONN" | grep -oP 'Database=\K[^;]+'); \
	PGUSER=$$(echo "$$CONN" | grep -oP 'Username=\K[^;]+'); \
	export PGPASSWORD=$$(echo "$$CONN" | grep -oP 'Password=\K[^;]+'); \
	psql -h "$$PGHOST" -p "$$PGPORT" -U "$$PGUSER" -d "$$PGDATABASE" -v ON_ERROR_STOP=1 -f e2e/concurrency/seed_create_race_fixture.sql && \
	VARS_ENV=./e2e/vars.env; \
	get_var() { grep -E "^$$1=" "$$VARS_ENV" | head -1 | cut -d= -f2-; }; \
	k6 run \
	  -e BASE_URL=$$(get_var BASE_URL) \
	  -e SA_EMAIL=$$(get_var SA_EMAIL) \
	  -e SA_PASSWORD=$$(get_var SA_PASSWORD) \
	  -e E2E_COMPANY_CODE=$$(get_var E2E_COMPANY_CODE) \
		e2e/concurrency/po-picker-consistency.js; CODE=$$?; \
		kill $$API_PID 2>/dev/null || true; wait $$API_PID 2>/dev/null || true; exit $$CODE )

build:
	dotnet build

test:
	dotnet test tests/WAMS.Application.Tests/WAMS.Application.Tests.csproj
	dotnet test tests/WAMS.Api.Tests/WAMS.Api.Tests.csproj
	dotnet test tests/WAMS.Infrastructure.Tests/WAMS.Infrastructure.Tests.csproj

test-coverage:
	dotnet test tests/WAMS.Application.Tests/WAMS.Application.Tests.csproj \
		--collect:"XPlat Code Coverage" --results-directory ./coverage/application
	dotnet test tests/WAMS.Api.Tests/WAMS.Api.Tests.csproj \
		--collect:"XPlat Code Coverage" --results-directory ./coverage/api

watch:
	@set -a && . ./.env && set +a && dotnet watch run --project src/WAMS.Api --no-launch-profile

migrate-add:
	@set -a && . ./.env && set +a && \
	read -p "Migration name: " name && \
	dotnet ef migrations add $$name \
		--project src/WAMS.Infrastructure \
		--startup-project src/WAMS.Api

migrate-up:
	@set -a && . ./.env && set +a && \
	dotnet ef database update \
		--project src/WAMS.Infrastructure \
		--startup-project src/WAMS.Api

migrate-down:
	@set -a && . ./.env && set +a && \
	dotnet ef migrations remove \
		--project src/WAMS.Infrastructure \
		--startup-project src/WAMS.Api

migrate-script:
	@set -a && . ./.env && set +a && \
	dotnet ef migrations script \
		--project src/WAMS.Infrastructure \
		--startup-project src/WAMS.Api

docker-up:
	docker compose up -d

docker-down:
	docker compose down

clean:
	dotnet clean
	find . -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true

swagger-gen:
	@set -a && . ./.env && set +a && \
	dotnet build src/WAMS.Api --configuration Release -o /tmp/wams-swagger-build && \
	dotnet swagger tofile --output openapi/swagger.json \
		/tmp/wams-swagger-build/WAMS.Api.dll v1
	@echo "swagger.json written to openapi/swagger.json"

help:
	@echo "Available commands:"
	@echo "  make run          - Run the API"
	@echo "  make e2e-reset    - Reset/migrate/seed the e2e DB via tools/WAMS.E2eReset (safety-locked)"
	@echo "  make run-e2e-api  - Run API against the e2e DB (does not reset it - run e2e-reset first)"
	@echo "  make e2e          - Run e2e tests (API must be running via run-e2e-api)"
	@echo "  make e2e-full               - Full cycle: start API (dotnet run), run e2e tests, stop API"
	@echo "  make e2e-full-published     - Full cycle: publish first (ReadyToRun), run e2e tests, stop API"
	@echo "  make e2e-seed SCALE=medium          - Seed volume data into e2e DB"
	@echo "  make e2e-full-volume SCALE=medium   - Full cycle: publish, reset, seed, run e2e"
	@echo "  make e2e-stability - Show slow/okay endpoint stability across all runs"
	@echo "  make build        - Build solution"
	@echo "  make test         - Run all tests"
	@echo "  make test-coverage - Run tests with coverage collection"
	@echo "  make watch        - Run with hot reload"
	@echo "  make migrate-add  - Create a new migration"
	@echo "  make migrate-up   - Apply pending migrations"
	@echo "  make migrate-down - Remove last migration"
	@echo "  make docker-up    - Start PostgreSQL"
	@echo "  make docker-down  - Stop infrastructure"
	@echo "  make clean        - Clean build artifacts"
	@echo "  make swagger-gen  - Export swagger.json to openapi/swagger.json"

# Warehouse Activity Management System - WAMS

WAMS is maintained as a monorepo with independently deployable backend and frontend applications.

## Repository layout

- `backend/` — ASP.NET Core API, database integrations, tests, Dockerfile, and API Portainer stack.
- `frontend/` — Vite/React application, Dockerfile, and frontend Portainer stack.
- `.github/workflows/backend.yml` — builds and publishes only when backend files change.
- `.github/workflows/frontend.yml` — builds and publishes only when frontend files change.
- `DEPLOYMENT_HANDOFF.md` — deployment and handoff instructions.

The backend and frontend remain separate runtime services, GHCR images, Portainer stacks, and rollback units. The monorepo only centralizes source and deployment documentation.

## Local development

Backend commands run from `backend/`:

```bash
dotnet restore WAMS.sln
dotnet build WAMS.sln
dotnet test WAMS.sln
```

Frontend commands run from `frontend/`:

```bash
npm ci
npm run dev
npm run build
```

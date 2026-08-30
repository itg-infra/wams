# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY src/WAMS.Domain/WAMS.Domain.csproj             src/WAMS.Domain/
COPY src/WAMS.Application/WAMS.Application.csproj   src/WAMS.Application/
COPY src/WAMS.Infrastructure/WAMS.Infrastructure.csproj src/WAMS.Infrastructure/
COPY src/WAMS.Api/WAMS.Api.csproj                   src/WAMS.Api/

RUN dotnet restore src/WAMS.Api/WAMS.Api.csproj \
    -r linux-musl-x64 \
    /p:PublishReadyToRun=true

COPY src/ src/

RUN dotnet publish src/WAMS.Api/WAMS.Api.csproj \
    -c Release \
    -r linux-musl-x64 \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Install ICU libraries for globalization support
RUN apk add --no-cache icu-libs tzdata

RUN addgroup -S appgroup && adduser -S appuser -G appgroup

COPY --from=build --chown=appuser:appgroup /app/publish .

RUN mkdir -p /app/storage && chown appuser:appgroup /app/storage

USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

HEALTHCHECK --interval=60s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WAMS.Api.dll"]

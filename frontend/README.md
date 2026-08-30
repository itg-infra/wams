# WAMS Frontend

This directory contains the Vite/React frontend for WAMS. It is part of the WAMS monorepo, but it remains independently buildable and deployable from the backend.

## Development

```bash
npm ci
npm run dev
```

Useful checks:

```bash
npm run lint
npm run build
```

The production API URL is supplied through the `VITE_API_URL` GitHub repository variable during the frontend image build. It is not read from the running container at runtime.

## Deployment

- Workflow: `.github/workflows/frontend.yml` from the repository root.
- Image: `ghcr.io/itg-infra/wams-prod-fe:production`.
- Portainer stack: `frontend/portainer-stack.yml`.
- Stack name: `wams-fe`.

Changes under `frontend/**` trigger the frontend workflow only. Backend changes do not rebuild this image.

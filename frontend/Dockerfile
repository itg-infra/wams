FROM node:24-alpine AS build

WORKDIR /app

ARG VITE_API_URL=/
ARG VITE_API_URL_TEST=/
ARG VITE_WAMS_API_URL=/

ENV VITE_API_URL=${VITE_API_URL} \
    VITE_API_URL_TEST=${VITE_API_URL_TEST} \
    VITE_WAMS_API_URL=${VITE_WAMS_API_URL}

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM caddy:2-alpine AS runtime

COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/dist /srv

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1

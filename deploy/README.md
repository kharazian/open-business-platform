# Deployment Kit

This directory is a reusable deployment blueprint for projects that use Open Business Platform as their core. It is intentionally split from the root `docker-compose.yml`, which remains focused on local development.

## What Lives Here

- `compose.yml`: generic server runtime with `web`, `api`, `migrator`, `postgres`, and `redis`.
- `compose.proxy.yml`: optional Caddy edge proxy for HTTP/HTTPS traffic.
- `compose.stage.example.yml` and `compose.prod.example.yml`: private-project override templates.
- `env/*.env.example`: environment templates with placeholder values only.
- `proxy/Caddyfile.example`: same-origin routing for `/api`, `/health`, and the React app.
- `github-actions/*.example`: inactive deploy workflow examples for private projects.
- `scripts/deploy.example.sh`: source-build deployment example for a server with Docker.
- `.github/workflows/deploy-stage.yml` and `.github/workflows/deploy-prod.yml`: active stage/prod deployment workflows.

## Core Repo Versus Private Project

Keep generic configuration here. Put real deployment values in the private project or directly on the server.

Core repo:

```txt
deploy/compose.yml
deploy/compose.proxy.yml
deploy/env/*.env.example
deploy/proxy/Caddyfile.example
deploy/github-actions/*.example
.github/workflows/deploy-stage.yml
.github/workflows/deploy-prod.yml
```

Private or server-only values:

```txt
deploy/compose.stage.yml
deploy/compose.prod.yml
deploy/env/stage.env
deploy/env/prod.env
deploy/proxy/Caddyfile
```

Do not commit real `stage.env`, `prod.env`, private SSH keys, production bootstrap passwords, or database passwords.

## First Server Shape

For a single Docker host such as `10.10.50.60`, run staging and production as separate Compose projects:

```txt
obp-stage
  proxy, web, api, postgres, redis

obp-prod
  proxy, web, api, postgres, redis
```

Use real domains when possible:

```txt
stage.example.com -> 10.10.50.60
app.example.com   -> 10.10.50.60
```

For temporary IP-based testing, use different ports in the env files, such as `8080` for staging and `80` for production.

## Source-Build Deployment

This kit does not require publishing Docker images yet. The server can build from source:

```bash
cp deploy/env/stage.env.example deploy/env/stage.env
cp deploy/compose.stage.example.yml deploy/compose.stage.yml

docker compose \
  --profile migrate \
  --env-file deploy/env/stage.env \
  -f deploy/compose.yml \
  -f deploy/compose.stage.yml \
  -f deploy/compose.proxy.yml \
  build

docker compose \
  --env-file deploy/env/stage.env \
  -f deploy/compose.yml \
  -f deploy/compose.stage.yml \
  -f deploy/compose.proxy.yml \
  up -d postgres redis

docker compose \
  --env-file deploy/env/stage.env \
  -f deploy/compose.yml \
  -f deploy/compose.stage.yml \
  -f deploy/compose.proxy.yml \
  stop api web

docker compose \
  --profile migrate \
  --env-file deploy/env/stage.env \
  -f deploy/compose.yml \
  -f deploy/compose.stage.yml \
  -f deploy/compose.proxy.yml \
  run --rm migrator

docker compose \
  --env-file deploy/env/stage.env \
  -f deploy/compose.yml \
  -f deploy/compose.stage.yml \
  -f deploy/compose.proxy.yml \
  up -d --remove-orphans
```

The example script wraps those commands:

```bash
sh deploy/scripts/deploy.example.sh stage
```

The `migrator` service is a one-shot EF Core migration runner. It uses the same database environment as the API, waits for PostgreSQL to be healthy, applies migrations, and exits. The deploy sequence keeps PostgreSQL and Redis running, stops the app containers before applying migrations, then starts the app on the new images. Keep the migrator behind the `migrate` Compose profile so normal `up` commands do not leave it running.

## GitHub Actions

The active `.github/workflows/ci.yml` workflow tests and builds this core repo. The active deploy workflows are split by environment:

```txt
.github/workflows/deploy-stage.yml
.github/workflows/deploy-prod.yml
```

`deploy-stage.yml` runs on pushes to `develop` and can also be started manually. `deploy-prod.yml` is manual-only.

Deploy workflows do not use SSH. They require a self-hosted GitHub Actions runner installed on the Docker server with labels matching:

```txt
self-hosted
linux
obp-deploy
```

The runner connects outbound to GitHub, receives the job, and runs Docker commands locally on the server. This keeps deployment close to the CI style while avoiding inbound SSH access from GitHub.

Private projects can copy:

```txt
deploy/github-actions/deploy-stage.yml.example
deploy/github-actions/deploy-prod.yml.example
```

to:

```txt
.github/workflows/deploy-stage.yml
.github/workflows/deploy-prod.yml
```

The deploy workflows assume the server has checked-out repositories under stable paths:

```txt
/opt/open-business-platform/stage  # deploys origin/develop
/opt/open-business-platform/prod   # deploys origin/main
```

The self-hosted runner user must be able to:

- read and write those checkout directories
- run `git fetch`, `git checkout`, and `git merge --ff-only`
- run `docker compose`

Use GitHub environments named `stage` and `prod`. The workflows can either use env files already present on the server or refresh them from GitHub environment secrets named `STAGE_ENV_FILE` and `PROD_ENV_FILE`. When those secrets are set, the workflow writes them with `umask 077` to:

```txt
deploy/env/stage.env
deploy/env/prod.env
```

inside the stable server checkout before running Compose. Keep long-running bind-mounted config, such as proxy config, in the stable checkout or a host-owned path such as `/etc/open-business-platform/stage`; do not depend on the self-hosted runner `_work` directory for files that running containers need after the deploy job ends.

## Later Registry Flow

When the private/core boundary is cleaner, switch deployment from source builds to registry pulls:

```txt
GitHub Actions builds images
GitHub Actions pushes to GHCR
Server pulls exact version tags
Compose restarts services
```

Until then, this kit keeps the deployment configuration reusable while avoiding premature image publishing.

## Local Docker And Shell Swaps

Use the local deployment env when you want to test the production-like Docker stack on your machine:

```bash
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  up -d --build
```

That starts:

```txt
proxy   http://localhost:8080
web     Docker Nginx frontend
api     Docker ASP.NET API
postgres exposed on localhost:55440
redis    exposed on localhost:6385
```

The API uses a named `api-data-protection` volume so cookie encryption keys survive container recreation.

Smoke test the Docker services:

```bash
curl http://localhost:8080/health
curl -I http://localhost:8080/
```

Apply migrations to the local deployment database before signing in:

```bash
docker compose \
  --profile migrate \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  run --rm --build migrator
```

If your agent sandbox cannot reach Docker-published host ports, test from inside the proxy container:

```bash
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  exec -T proxy wget -S -O - --header 'Host: localhost' http://127.0.0.1/health
```

### Stop Docker Frontend, Run Frontend From Shell

Keep Docker API/PostgreSQL/Redis running, then stop only the web container:

```bash
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  stop web
```

Run the frontend from the shell:

```bash
cd src/app
VITE_API_BASE_URL=http://localhost:8080 npm run dev
```

Use the Vite URL:

```txt
http://127.0.0.1:5174
```

If you want the Docker proxy to route the app root to the shell frontend, recreate only the proxy with a shell upstream:

```bash
WEB_UPSTREAM=host.docker.internal:5174 \
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  up -d --no-deps --force-recreate proxy
```

### Stop Docker API, Run API From Shell

Keep Docker PostgreSQL/Redis running, then stop only the API container:

```bash
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  stop api
```

Run the API from the shell against the Docker PostgreSQL/Redis ports:

```bash
APP_ENV=Staging \
ASPNETCORE_ENVIRONMENT=Staging \
ASPNETCORE_URLS=http://127.0.0.1:5080 \
AUTH_COOKIE_NAME=obp.local.shell.auth \
AUTH_COOKIE_REQUIRE_SECURE=false \
BOOTSTRAP_ADMIN_EMAIL=admin.local@example.com \
BOOTSTRAP_ADMIN_PASSWORD=local-deploy-bootstrap-password \
POSTGRES_HOST=127.0.0.1 \
POSTGRES_PORT=55440 \
POSTGRES_DB=open_business_platform_local_deploy \
POSTGRES_USER=obp_local_deploy \
POSTGRES_PASSWORD=local-deploy-postgres-password \
REDIS_HOST=127.0.0.1 \
REDIS_PORT=6385 \
dotnet run --no-launch-profile --project src/api/OpenBusinessPlatform.Api.csproj
```

If the Docker proxy should route `/api` and `/health` to the shell API, recreate only the proxy with a shell upstream:

```bash
API_UPSTREAM=host.docker.internal:5080 \
WEB_UPSTREAM=host.docker.internal:5174 \
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  up -d --no-deps --force-recreate proxy
```

### Return To Full Docker

Stop the shell-run frontend/API processes, then restore the Docker services and proxy upstreams:

```bash
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  up -d --build api web

API_UPSTREAM=api:5080 \
WEB_UPSTREAM=web:80 \
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  up -d --no-deps --force-recreate proxy
```

Stop the local deployment stack when you are done:

```bash
docker compose \
  --env-file deploy/env/local.env.example \
  -f deploy/compose.yml \
  -f deploy/compose.stage.example.yml \
  -f deploy/compose.local.example.yml \
  -f deploy/compose.proxy.yml \
  down
```

The `down` command keeps named volumes by default. Add `-v` only when you intentionally want to delete the local deployment database and Redis data.

# Deployment Kit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable Docker/deployment kit that lives in the core repo and can be copied or overlaid by a private project without committing private secrets.

**Architecture:** Keep local development Compose separate from server runtime Compose. Add buildable API and web images, generic stage/prod examples, Caddy proxy templates, deploy workflow examples, and active core CI only. Private projects provide real env files, domains, server paths, and active deploy workflows.

**Tech Stack:** Docker Compose, ASP.NET Core .NET 10, React/Vite, Nginx static serving, Caddy reverse proxy, GitHub Actions examples.

---

### Task 1: Container Build Inputs

**Files:**
- Create: `src/app/Dockerfile`
- Create: `src/app/nginx.conf`
- Create: `src/app/.dockerignore`
- Create: `src/api/.dockerignore`
- Modify: `src/api/Dockerfile`
- Modify: `src/api/Program.cs`

- [ ] Add the production web image for the Vite app. It should build with Node, copy the `dist` output into Nginx, and serve SPA routes with `try_files`.
- [ ] Add Docker ignore files for the frontend and API contexts.
- [ ] Add a lightweight API container healthcheck dependency by installing `curl` in the final API image.
- [ ] Enable forwarded headers and production secure cookies in the API host so it behaves correctly behind Caddy/Nginx TLS termination.

### Task 2: Reusable Deployment Templates

**Files:**
- Create: `deploy/compose.yml`
- Create: `deploy/compose.stage.example.yml`
- Create: `deploy/compose.prod.example.yml`
- Create: `deploy/compose.proxy.yml`
- Create: `deploy/proxy/Caddyfile.example`
- Create: `deploy/env/common.env.example`
- Create: `deploy/env/stage.env.example`
- Create: `deploy/env/prod.env.example`
- Create: `deploy/scripts/deploy.example.sh`

- [ ] Add a generic server Compose file with `web`, `api`, `postgres`, and `redis`.
- [ ] Add stage/prod override examples that private projects can copy to active override files.
- [ ] Add a proxy Compose file and Caddyfile example that route `/api` and `/health` to the API and all other routes to the web container.
- [ ] Add env examples with explicit private-project comments for secrets, domains, and per-environment values.
- [ ] Add an example deployment script that builds from source on the server without requiring a registry.

### Task 3: CI And Private-Project Examples

**Files:**
- Create: `.github/workflows/ci.yml`
- Create: `deploy/github-actions/deploy-stage.yml.example`
- Create: `deploy/github-actions/deploy-prod.yml.example`

- [ ] Add active core CI for tests, builds, Compose config rendering, and Docker image build checks without publishing images.
- [ ] Add inactive deploy workflow examples for private projects to copy and adapt.

### Task 4: Documentation

**Files:**
- Create: `deploy/README.md`
- Modify: `README.md`
- Modify: `docs/TECH_STACK.md`
- Modify: `docs/ARCHITECTURE.md`

- [ ] Document the split between local dev, reusable core deployment templates, and private deployment values.
- [ ] Document how private projects should copy/adapt examples without committing secrets.
- [ ] Mention that publishing Docker images is intentionally deferred.

### Task 5: Verification

**Commands:**
- `docker compose -f deploy/compose.yml -f deploy/compose.proxy.yml --env-file deploy/env/stage.env.example config`
- `npm test` from `src/app`
- `npm run build` from `src/app`
- `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`
- `dotnet build src/api/OpenBusinessPlatform.Api.csproj`

- [ ] Run Compose config rendering to catch YAML/template errors.
- [ ] Run frontend tests and build.
- [ ] Run backend test harness and build.

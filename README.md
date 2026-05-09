# Open Business Platform

Open Business Platform is an open-source modular platform for building internal company systems.

The goal is to help companies build dashboards, user management, forms, workflows, reports, and other business apps step by step.

## Vision

Many companies need many internal systems: HR, forms, approvals, inventory, CRM, tickets, reports, and document workflows.

This project provides a shared foundation so developers do not need to rebuild authentication, permissions, dashboards, audit logs, and module structure every time.

## First MVP

The first version includes:

- Dashboard
- Authentication
- User management
- Role management
- Permission management
- Audit logs
- Module registry
- Docker Compose setup

## Future Modules

- Dynamic Form Builder
- Workflow / Approval Engine
- Notification Center
- Reports
- CRM
- HR
- Inventory
- Document Management
- AI Assistant

## Tech Stack

- Backend: ASP.NET Core on .NET 10
- Frontend: React 19 + Vite + Tailwind CSS
- Database: PostgreSQL
- Cache/Queue: Redis
- Auth: OpenIddict or external OIDC provider
- Observability: OpenTelemetry
- AI: Provider-based integration for OpenAI, Azure OpenAI, Ollama, or custom providers

## Architecture

This project uses a modular monolith approach with practical Clean Architecture.

Each module owns its own domain, application logic, infrastructure, and API endpoints.

## Current Skeleton

The first project skeleton includes:

- ASP.NET Core API host targeting .NET 10 in `src/api`
- React 19 + Vite + Tailwind CSS dashboard in `src/app`
- PostgreSQL and Redis through Docker Compose
- API health endpoint at `http://localhost:5080/health`
- Dashboard page at `http://localhost:5174`

## Prerequisites

- Docker and Docker Compose
- .NET 10 SDK
- Node.js 20.19+ or 22.12+
- npm

## Run Locally

Start PostgreSQL and Redis:

```bash
docker compose up -d
```

Run the backend API:

```bash
cd src/api
dotnet run
```

The API listens on `http://localhost:5080`.

Check the health endpoint:

```bash
curl http://localhost:5080/health
```

Run the frontend dashboard in another terminal:

```bash
cd src/app
npm install
npm run dev
```

Open `http://localhost:5174`.

## Local Services

Docker Compose exposes:

- PostgreSQL on `localhost:5432`
- Redis on `localhost:6379`

The development PostgreSQL connection string is:

```text
Host=localhost;Port=5432;Database=open_business_platform;Username=obp;Password=obp_dev_password
```

## Project Structure

```text
src/
  api/
    Modules/
      Dashboard/
    Program.cs
  app/
    src/
      App.tsx
      main.tsx
```

The platform starts as a modular monolith. It does not use microservices, Native Federation, or dynamic DLL plugin loading in this first skeleton.

## Frontend Theme

The web app uses Tailwind CSS through the official Vite plugin. Project theme tokens are defined in `src/app/src/styles.css` with Tailwind's `@theme` directive.

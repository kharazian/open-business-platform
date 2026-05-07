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

- Backend: ASP.NET Core
- Frontend: React + Vite
- Database: PostgreSQL
- Cache/Queue: Redis
- Auth: OpenIddict or external OIDC provider
- Observability: OpenTelemetry
- AI: Provider-based integration for OpenAI, Azure OpenAI, Ollama, or custom providers

## Architecture

This project uses a modular monolith approach with practical Clean Architecture.

Each module owns its own domain, application logic, infrastructure, and API endpoints.
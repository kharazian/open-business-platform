# Backend Entity CRUD Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish a framework-lite backend foundation with Guid entity IDs, inheritance-based audit fields, and reusable CRUD primitives.

**Architecture:** Domain entities will inherit from small base classes inspired by ABP, Rails, Django, Laravel, Spring/JPA, and similar frameworks, without importing a framework dependency. EF Core will keep explicit table/column mappings and the initial migration will be updated while it is still newly staged.

**Tech Stack:** ASP.NET Core minimal APIs, .NET 10, EF Core 10, Npgsql, PostgreSQL.

---

### Task 1: Lock Entity Conventions With Tests

**Files:**
- Modify: `src/api.Tests/Program.cs`

- [ ] Add assertions that core entities use `Guid` IDs.
- [ ] Add assertions that core entities inherit audited base classes.
- [ ] Add assertions that the EF model maps Guid IDs to PostgreSQL `uuid`.
- [ ] Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj` and confirm the new assertions fail before implementation.

### Task 2: Add Domain Foundation Types

**Files:**
- Create: `src/api/Domain/Common/Entity.cs`
- Create: `src/api/Domain/Common/Auditing.cs`
- Create: `src/api/Domain/Common/EntityCapabilities.cs`

- [ ] Add `IEntity<TKey>`, `Entity<TKey>`, `AggregateRoot<TKey>`.
- [ ] Add creation, modification, deletion, and full-audit interfaces and base classes.
- [ ] Add capability interfaces for concurrency stamps, extra JSON properties, active status, and sort order.

### Task 3: Rename Current Entities and Apply Inheritance

**Files:**
- Rename/update: `src/api/Domain/Entities/ApplicationUser.cs` to `User.cs`
- Rename/update: `src/api/Domain/Entities/ApplicationRole.cs` to `Role.cs`
- Rename/update: `src/api/Domain/Entities/ApplicationDepartment.cs` to `Department.cs`
- Rename/update: `src/api/Domain/Entities/ApplicationUserRole.cs` to `UserRole.cs`
- Rename/update: `src/api/Domain/Entities/ApplicationUserDepartment.cs` to `UserDepartment.cs`
- Modify: `src/api/Domain/Entities/FormDefinition.cs`
- Modify: `src/api/Domain/Entities/FormVersion.cs`
- Modify: `src/api/Domain/Entities/FormRecord.cs`
- Modify: `src/api/Domain/Entities/AuditLogEntry.cs`

- [ ] Use `Guid` primary keys for real entities.
- [ ] Keep composite keys for join entities.
- [ ] Use audited aggregate roots for mutable business entities.
- [ ] Keep audit log append-only and avoid generic CRUD semantics for it.

### Task 4: Update EF Core Persistence

**Files:**
- Modify: `src/api/Infrastructure/Persistence/OpenBusinessPlatformDbContext.cs`
- Modify: `src/api/Infrastructure/Persistence/Migrations/20260519141036_InitialDatabaseFoundation.cs`
- Modify: `src/api/Infrastructure/Persistence/Migrations/20260519141036_InitialDatabaseFoundation.Designer.cs`
- Modify: `src/api/Infrastructure/Persistence/Migrations/OpenBusinessPlatformDbContextModelSnapshot.cs`

- [ ] Update `DbSet` names and entity mappings.
- [ ] Map Guid IDs to `uuid` columns.
- [ ] Map audit columns inherited from base classes.
- [ ] Add `concurrency_stamp` and `extra_properties_json` where supported by aggregate roots.
- [ ] Preserve table names such as `users`, `roles`, `forms`, and `records`.

### Task 5: Add CRUD Building Blocks Without Exposing Generic Endpoints

**Files:**
- Create: `src/api/Application/Common/Paging.cs`
- Create: `src/api/Application/Common/EntityDtos.cs`
- Create: `src/api/Application/Common/Repositories.cs`
- Create: `src/api/Application/Common/ApplicationServiceBase.cs`
- Create: `src/api/Application/Common/CrudServiceBase.cs`
- Create: `src/api/Infrastructure/Persistence/EfRepository.cs`
- Modify: `src/api/Program.cs`

- [ ] Add DTO and paging primitives.
- [ ] Add repository interfaces and EF implementation.
- [ ] Add read-only and mutable CRUD service base classes with permission/check hooks.
- [ ] Register repository services in DI.
- [ ] Do not add generated CRUD HTTP endpoints yet.

### Task 6: Update Docs and Verify

**Files:**
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/DATA_MODEL.md`
- Modify: `docs/TECH_STACK.md`
- Modify: `docs/TESTING_STRATEGY.md`
- Modify: `README.md`

- [ ] Document Guid IDs, base entities, and CRUD foundation.
- [ ] Run `dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj`.
- [ ] Run `dotnet build src/api/OpenBusinessPlatform.Api.csproj`.
- [ ] Run `git diff --cached --check`.
- [ ] Try `dotnet ef migrations has-pending-model-changes --project src/api/OpenBusinessPlatform.Api.csproj --startup-project src/api/OpenBusinessPlatform.Api.csproj`; if `dotnet-ef` is unavailable, report that honestly.

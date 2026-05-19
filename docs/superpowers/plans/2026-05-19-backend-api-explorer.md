# Backend API Explorer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add development-only OpenAPI, Swagger UI, and Scalar UI to the ASP.NET Core backend.

**Architecture:** The minimal API host registers built-in OpenAPI generation and maps documentation endpoints only in the `Development` environment. Swagger UI and Scalar UI both read the same generated `/openapi/v1.json` document.

**Tech Stack:** ASP.NET Core minimal APIs targeting .NET 10, `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore.SwaggerUI`, `Scalar.AspNetCore`.

---

### Task 1: Verify Missing Explorer Endpoint

**Files:**
- Read: `src/api/Program.cs`
- Read: `src/api/OpenBusinessPlatform.Api.csproj`

- [ ] **Step 1: Start the API before implementation**

Run:

```bash
dotnet run --project src/api/OpenBusinessPlatform.Api.csproj
```

Expected: API listens on `http://localhost:5080`.

- [ ] **Step 2: Verify OpenAPI endpoint is missing**

Run:

```bash
curl -i http://localhost:5080/openapi/v1.json
```

Expected: `HTTP/1.1 404 Not Found`.

### Task 2: Add OpenAPI and UI Packages

**Files:**
- Modify: `src/api/OpenBusinessPlatform.Api.csproj`

- [ ] **Step 1: Add package references**

Add these references to the API project:

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.4" />
<PackageReference Include="Scalar.AspNetCore" Version="2.14.11" />
<PackageReference Include="Swashbuckle.AspNetCore.SwaggerUI" Version="10.1.7" />
```

- [ ] **Step 2: Restore packages**

Run:

```bash
dotnet restore src/api/OpenBusinessPlatform.Api.csproj
```

Expected: restore completes with exit code `0`.

### Task 3: Map Development API Explorer

**Files:**
- Modify: `src/api/Program.cs`

- [ ] **Step 1: Import Scalar extensions**

Add:

```csharp
using Scalar.AspNetCore;
```

- [ ] **Step 2: Register OpenAPI services**

Add after configuration service registration:

```csharp
builder.Services.AddOpenApi();
```

- [ ] **Step 3: Map development-only explorer endpoints**

Inside the existing development block, add:

```csharp
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Open Business Platform API v1");
});
app.MapScalarApiReference();
```

### Task 4: Document Local URLs

**Files:**
- Modify: `README.md`
- Modify: `docs/API_SPEC.md`

- [ ] **Step 1: Update README backend instructions**

Document these development URLs:

```text
http://localhost:5080/openapi/v1.json
http://localhost:5080/swagger
http://localhost:5080/scalar
```

- [ ] **Step 2: Update API spec**

Add a short local API explorer section noting that the explorer is development-only.

### Task 5: Verify Implementation

**Files:**
- Read: `src/api/Program.cs`
- Read: `src/api/OpenBusinessPlatform.Api.csproj`
- Read: `README.md`
- Read: `docs/API_SPEC.md`

- [ ] **Step 1: Build backend**

Run:

```bash
dotnet build src/api/OpenBusinessPlatform.Api.csproj
```

Expected: build completes with exit code `0`.

- [ ] **Step 2: Run backend test harness**

Run:

```bash
dotnet run --project src/api.Tests/OpenBusinessPlatform.Api.Tests.csproj
```

Expected: command completes with exit code `0`.

- [ ] **Step 3: Start API**

Run:

```bash
dotnet run --project src/api/OpenBusinessPlatform.Api.csproj
```

Expected: API listens on `http://localhost:5080`.

- [ ] **Step 4: Check OpenAPI JSON**

Run:

```bash
curl -i http://localhost:5080/openapi/v1.json
```

Expected: `HTTP/1.1 200 OK` with an OpenAPI JSON document.

- [ ] **Step 5: Check Swagger UI**

Run:

```bash
curl -i http://localhost:5080/swagger
```

Expected: a redirect or `200 OK` response for Swagger UI.

- [ ] **Step 6: Check Scalar UI**

Run:

```bash
curl -i http://localhost:5080/scalar
```

Expected: a redirect or `200 OK` response for Scalar UI.


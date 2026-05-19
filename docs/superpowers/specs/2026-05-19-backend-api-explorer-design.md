# Backend API Explorer Design

Date: 2026-05-19

## Goal

Expose local development API documentation for the ASP.NET Core backend so developers can browse and test endpoints in the browser.

## Scope

Add development-only OpenAPI output and two browser UIs:

- Raw OpenAPI JSON at `/openapi/v1.json`
- Swagger UI at `/swagger`
- Scalar UI at `/scalar`

The change does not add product endpoints, database tables, authorization behavior, or frontend UI.

## Architecture

The API host remains a minimal API application. `Program.cs` registers ASP.NET Core's built-in OpenAPI document generator and maps the OpenAPI/Swagger/Scalar endpoints only when `app.Environment.IsDevelopment()` is true.

Swagger UI and Scalar both read the same generated `/openapi/v1.json` document. Existing platform modules continue to map their own endpoint groups through `MapPlatformApiModules()`.

## Packages

- `Microsoft.AspNetCore.OpenApi` for first-party OpenAPI document generation.
- `Swashbuckle.AspNetCore.SwaggerUI` for the classic Swagger browser UI.
- `Scalar.AspNetCore` for the modern API reference UI.

## Security

The API explorer endpoints are development-only to avoid exposing internal API details in production. Authenticated endpoints remain protected by the existing cookie authentication and authorization middleware.

## Documentation

Update the API documentation and README with the local URLs and note that the explorer is only available in development.

## Verification

Run a red check before implementation by starting the API and requesting `/openapi/v1.json`; it should return `404`.

After implementation:

- `dotnet restore src/api/OpenBusinessPlatform.Api.csproj`
- `dotnet build src/api/OpenBusinessPlatform.Api.csproj`
- `dotnet run --project src/api/OpenBusinessPlatform.Api.csproj`
- `curl http://localhost:5080/openapi/v1.json`
- `curl http://localhost:5080/swagger`
- `curl http://localhost:5080/scalar`


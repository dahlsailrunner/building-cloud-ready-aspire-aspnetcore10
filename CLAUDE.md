# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A teaching/reference demo for building cloud-ready ASP.NET Core 10 applications with .NET Aspire (the "CarvedRock" outdoor-gear store). It is *not* a production app — code favors clarity and explicitness so it can serve the narrative in `readme.md`, which walks through layering Aspire in piece by piece. When making changes, prefer the option that keeps the educational story clear over the cleverest one, and keep `readme.md` in mind as the source of truth for the intended progression.

## Commands

The solution file is `cloud-ready-with-aspire.slnx` (the newer XML `.slnx` format). NuGet versions are declared **directly in each `.csproj`** — there is no `Directory.Packages.props` / central package management.

```bash
# Build everything
dotnet build cloud-ready-with-aspire.slnx

# Run the whole app via Aspire (starts Postgres, MailPit, API, MCP, Agent, WebApp, inspectors)
aspire run                                   # requires the Aspire CLI
dotnet run --project CarvedRock.AppHost      # or run the AppHost directly
# In VS Code (with the C# Dev Kit + Aspire extension) just hit F5.

# Tests (xUnit v3 integration tests that boot the full AppHost)
dotnet test
dotnet test --filter "FullyQualifiedName~ApiTests"                       # one class
dotnet test --filter "FullyQualifiedName~ApiTests.GetAllProductsReturnsAllProducts"  # one test

# EF Core migrations — run from the CarvedRock.Data folder, with the API as startup project
dotnet ef migrations add <Name> -s ../CarvedRock.Data

# Publish a Helm chart for Kubernetes into CarvedRock.AppHost/aspire-output
aspire publish
```

Playwright-based tests need browsers installed once: build the test project, then run
`playwright.ps1 install` from its build output folder.

## Architecture

Aspire orchestrates several services; `CarvedRock.AppHost/AppHost.cs` is the composition root that wires them together (references, `WaitFor` ordering, health checks, custom dashboard commands like "Reset Data", and parameters such as the OpenAI key). Read it first to understand how the pieces connect.

**Service projects** (each calls `builder.AddServiceDefaults()` from `CarvedRock.ServiceDefaults` for OpenTelemetry, health checks, service discovery, and HTTP resilience):

- **CarvedRock.Api** — REST API (controllers). The product domain itself follows a clean/layered split:
  - `CarvedRock.Domain` — business logic (`ProductLogic`), validators, model mapping.
  - `CarvedRock.Data` — EF Core (`LocalContext`), repository, entities, migrations, `SeedData.json`.
  - `CarvedRock.Core` — shared models, constants, OpenAPI helpers, and the `AdminClaimsTransformation`.
- **CarvedRock.Mcp** — Model Context Protocol server exposing tools/prompts over the API; forwards the caller's bearer token to the API via `TokenForwarder`.
- **CarvedRock.Agent** — AI agent endpoint (`GET /agent`) using `Microsoft.Extensions.AI` + OpenAI; calls the MCP server.
- **CarvedRock.WebApp** — Razor Pages front end; OIDC login, calls the API and Agent, sends email via `MailKit.Client` (an Aspire client integration) against the MailPit container.

**Data flow / dependency direction:** WebApp → Agent → Mcp → Api → Domain → Data. Api/Domain/Data/Core never reference the web-facing projects.

### Auth model (important and a little unusual)

All services authenticate JWTs against the **Duende demo IdentityServer** (`https://demo.duendesoftware.com`) — a public demo instance, no local auth server. Admin rights are not a real claim from the token: `AdminClaimsTransformation` (in `CarvedRock.Core`) grants the `admin` role at runtime when the user's email starts with `bobsmith` (the demo `bob` login) or when the client is `m2m.short`. So:

- `GET` product routes allow anonymous.
- `POST` / `PUT` / `DELETE` require auth **and** the admin role → log in as `bob`, not `alice`.

### Persistence and seeding

PostgreSQL via EF Core 10 + Npgsql, provided as an Aspire container (no manual Docker needed when running through the AppHost). On startup in Development the API calls `LocalContext.MigrateAndCreateData()`, which applies migrations and — **only when connecting to a `localhost`/`postgres` host** — wipes and reseeds from `CarvedRock.Data/SeedData.json`. The dashboard's "Reset Data" command and `POST /internal/reset-data` re-trigger this.

### Validation & errors

FluentValidation (`NewProductValidator`) is enforced in `ProductLogic` via `ValidateAndThrowAsync`; a global `ValidationExceptionHandler` + ProblemDetails translate failures into responses, stamping `Activity.Current?.RootId` as the instance id for correlation.

### Tests

`tests/CarvedRock.Tests` uses `Aspire.Hosting.Testing`. `AppFixture` (an xUnit collection fixture) spins up the **entire** AppHost once, waits for the `webapp` resource to become healthy, and exposes `App.CreateHttpClient("<resource>")` plus helpers for authenticated/anonymous MCP clients. Admin credentials come from AppHost parameters `adminUsername` / `adminPassword` (set via user secrets / parameters in Development). Tests are real integration tests — expect them to be slower and to need Docker available.

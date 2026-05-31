# Repository Guidelines

## Project Structure & Module Organization

This repository is a .NET 10 Aspire sample for the Carved Rock application. The solution file is `cloud-ready-with-aspire.slnx`. Runtime projects live at the repository root:

- `CarvedRock.AppHost/` defines the Aspire distributed application, PostgreSQL, MailPit, MCP Inspector, and project orchestration.
- `CarvedRock.Api/`, `CarvedRock.WebApp/`, `CarvedRock.Mcp/`, and `CarvedRock.Agent/` are the externally visible services.
- `CarvedRock.Domain/`, `CarvedRock.Data/`, `CarvedRock.Core/`, `CarvedRock.ServiceDefaults/`, and `MailKit.Client/` contain shared logic, persistence, configuration, telemetry, and infrastructure helpers.
- `tests/CarvedRock.Tests/` contains Aspire integration tests and Playwright UI tests.

Static web assets are under `CarvedRock.WebApp/wwwroot/`; EF migrations and seed data are under `CarvedRock.Data/`.

## Build, Test, and Development Commands

- `dotnet restore cloud-ready-with-aspire.slnx` restores NuGet packages.
- `dotnet build cloud-ready-with-aspire.slnx` builds all projects.
- `aspire start --apphost CarvedRock.AppHost/CarvedRock.AppHost.csproj` starts the distributed app and dashboard.
- `dotnet test tests/CarvedRock.Tests/CarvedRock.Tests.csproj` runs xUnit integration and UI tests.
- `pwsh tests/CarvedRock.Tests/bin/Debug/net10.0/playwright.ps1 install` installs Playwright browsers after the test project is built.
- `aspire publish --apphost CarvedRock.AppHost/CarvedRock.AppHost.csproj` generates sample Kubernetes output in `CarvedRock.AppHost/aspire-output/`.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled. Follow existing style: four-space indentation for `.cs`, PascalCase for types and public members, camelCase for local variables and parameters, and async methods ending in `Async`. Keep Razor pages paired as `Page.cshtml` and `Page.cshtml.cs`. Prefer dependency injection and existing project boundaries over cross-project shortcuts.

## Testing Guidelines

Tests use xUnit v3, `Aspire.Hosting.Testing`, Playwright, and coverlet. Add tests in `tests/CarvedRock.Tests/` with descriptive names such as `GetAllProductsReturnsAllProducts`. UI tests should use Playwright role locators where practical. Some admin tests require `adminUsername` and `adminPassword` Aspire parameters or user secrets.

## Commit & Pull Request Guidelines

Git history uses short, imperative, lower-case summaries such as `added k8s publish as sample` and `minor tweaks`. Keep commits focused. Pull requests should describe the change, list validation commands run, call out configuration or migration impacts, and include screenshots for visible web UI changes.

## Security & Configuration Tips

Do not commit secrets. Configure `openaiKey`, `adminUsername`, and `adminPassword` through Aspire parameters, dashboard prompts, or .NET user secrets. Avoid committing generated `bin/`, `obj/`, `.vs/`, `.idea/`, or `aspire-output/` content.

## Agent-Specific Instructions

For AppHost or resource changes, prefer Aspire CLI workflows and inspect `CarvedRock.AppHost/AppHost.cs` first. Keep generated files and build artifacts out of edits unless explicitly requested.

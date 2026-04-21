# Building Cloud-Ready ASP.NET Core 10 Applications with Aspire

This repo is meant to help with the understanding of how to build
cloud-ready applications with ASP.NET Core 10 and Aspire.  Key concepts include:

* Aspire fundamentals - including the app host and service defaults
* Aspire hosting and client integrations
* Basics of logging, OpenTelemetry, health checks, and resilience
* Configuration and service discovery
* Testing with Aspire
* Agentic development with Aspire
* Contrasting with Docker Compose and other orchestration solutions

## Getting Started

You need the [Aspire prerequisites](https://aspire.dev/get-started/prerequisites/).

### VS Code Setup

You need the following extension:

* [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

Then just hit `F5` to run the app.

The [Aspire CLI](https://aspire.dev/get-started/install-cli/#install-the-aspire-cli) is highly recommended, along with the [Aspire VS Code Extension](https://aspire.dev/get-started/aspire-vscode-extension/).

## First Steps

This content is meant to start simply - and without Aspire - and then
layer in Aspire with different building blocks from it.

### Running What's Here

Beyond the prerequisites for Aspire and .NET, this API project
in this solution uses PostgreSQL.

To run it without Aspire, perform the following steps / commands:

```bash
docker pull postgres
docker run -p 5432:5432 -d -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=carvedrock -e POSTGRES_DB=carvedrock postgres
```

Then you can start the API project (but you might have to wait a minute
for Postgres to be fully available).

The `GET /product?category=all` as a first step from the Scalar API reference UI.

Don't forget to stop / remove the docker container after you're done
running this!

```bash
docker ps # to find the container id for postgres
docker stop <container id>
docker rm <container id>
```

### Adding Aspire App Host and Postgres Hosting Integration

See next release!

### Adding ServiceDefaults

See next release!

### Adding Postgres Client Integration

See next release!

## Features

* **API**
  
  * `GET` based on category (or "all") and by id allow anonymous requests
  * `POST`, `PUT`, and `DELETE` require authentication and an `admin` role (available with the `bob` login, but not `alice`)
  * Validation with [FluentValidation](https://docs.fluentvalidation.net/en/latest/index.html) - try the `POST` method with a duplicate name or very high price
  * A `GET` with a category of something other than "all", "boots", "equip", or "kayak" will throw an error
  * Data is seeded by the `SeedData.json` contents in the `Data` project

## Data and EF Core Migrations

The `dotnet ef` tool is used to manage EF Core migrations.  The following command was used to create migrations (from the `CarvedRock.Data` folder).

```bash
dotnet ef migrations add Initial -s ../CarvedRock.Api
```

The application uses PostgreSQL.

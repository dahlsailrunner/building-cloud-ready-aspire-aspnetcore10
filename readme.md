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

Use the .NET templates for an Aspire AppHost to start.

```bash
dotnet new install Aspire.ProjectTemplates # if you don't already have them
dotnet new aspire-apphost -o CarvedRock.AppHost
```

Once you've done that, you can add a project reference in the AppHost project
to the `CarvedRock.Api` project.

Then you can add the API to the AppHost (`AppHost.cs`):

```csharp
builder.AddProject<Projects.CarvedRock_Api>("api");
```

**NOTE:** This will not work without either running the Postgres Docker container,
or (better!) keep going and add the PostgreSQL hosting integration.

In the AppHost directory:

```bash
dotnet new add package Aspire.Hosting.PostgreSQL
```

Or use the Aspire VS Code extension and choose the `Aspire: Add an integration`
command from the command pallete and search for postgres, then add
`Aspire.Hosting.PostgreSQL`.

Then update `AppHost.cs` to have these lines:

```csharp
var db = builder.AddPostgres("db").AddDatabase("CarvedRockPostgres");

builder.AddProject<Projects.CarvedRock_Api>("api")
    .WithReference(db)
    .WaitFor(db);
```

Finally add a `.vscode/launch.json` file that looks like this to the
root folder:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "type": "aspire",
            "request": "launch",
            "name": "Aspire: Launch default AppHost",
            "program": ""
        }
    ]
}
```

Then run it!  A Postgres database should start up, and then
the API should start.  Then you can try the `GET /product` route and
it should work.

### Adding ServiceDefaults

From the root folder:

```bash
dotnet new aspire-servicedefaults -o CarvedRock.ServiceDefaults
```

Add a project reference in the API to the new CarvedRock.ServiceDefaults project.

Add the following lines to Program.cs of the API:

```csharp
builder.AddServiceDefaults(); // after var builder = WebApplication.CreateBuilder(args);

//...

app.MapDefaultEndpoints(); // after var app = builder.Build();

```

Run the app again, and now you get Structured Logs, Traces, and Metrics in the
Aspire Dashboard!

### Adding Postgres Client Integration

In the API directory:

```bash
dotnet add package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
```

Add the same reference to in the CarvedRock.Data project, and the following
references can be removed:

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1"/>
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.6" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.6" />
```

You may also need to update the version number of the
`Microsoft.EntityFrameworkCore.Design` package.

In `Program.cs` of the API, comment out the existing addition of the `LocalContext`
and replace it as shown below:

```csharp
// var cstr = builder.Configuration.GetConnectionString("CarvedRockPostgres");
// builder.Services.AddDbContext<LocalContext>(options =>
//      options.UseNpgsql(cstr));

builder.AddNpgsqlDbContext<LocalContext>("CarvedRockPostgres");
```

Run the project again, you should see trace information that includes
database activity in the Traces on API calls, and you should also see
Npgsql metrics in the Metrics tab of the Dashboard!

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

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

> **NOTE:** This repo has some "releases" that have different points
> of progression regarding maturity of the repo within Aspire and the
> corresponding Pluralsight course. Download the releases if you want
> to experiment at different points of its evolution.

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

## Testing

A testing project has been included that performs some simple tests.

Some of the tests use Playwright, and for those you need to build
the project and ensure the Playwright browsers are installed.

Run the `playwright.ps1 install` script from the build output folder.

## Deployment

This application isn't really deployed anywhere, but some
sample code has been added that enables publishing to kubernetes.

If you run `aspire publish`, it will create a Helm chart in the
`CarvedRock.AppHost/aspire-output` directory.  Feel free to create
that and browse the content.

## Agentic Development

Start with `AGENTS.md` and/or `CLAUDE.md` by using `/init` commands
from within Claude or Codex or other similar tool.

Then use the `aspire agent init` command from the terminal to add
Aspire-specific skills.

Verify by using the following prompt:

`Start the aspire app and show me resource status`

### Understanding the Application

Creating Aspire skills for your agent(s) can
improve the agent's ability to help you
understand an application.  Here are some
suggestions for what you might ask:

* Ask about the startup error that occurs in the API -- is it anything to worry about?
* Ask to explain a trace you've created.

Here are some other questions that you might ask:

* How is a user identified as an admin?
* Is there any chat functionality available only to admins?
* How is the MCP server authenticated?
* How does validation work?
* How is streaming achieved for the responses from the agent?
* Is the browser environment properly secured in the context of this application?
* What are the key NuGet packages used in the solution?

### Planning and Implementing Features

The plan created in `docs/cart-order-persistence-plan.md` was created
from the following prompt:

```txt
Create a plan for the following: Update the cart
functionality in the webapp to use the database via the
api (will require a new table - with a key or index on
the user id), and when an order is completed, add a
record of it to the database as well (new order and
details tables).  The email logic should also be moved
into the api method used to place an order.  A migration
should be created for the new tables, which can start
empty.  To keep things simple, let's also update the
Listing page to require an authenticated user. Tests
should include adding items to the cart and making sure
the database has those rows, and placing an order creates
entries in the appropriate tables, and that an email has
been sent (use the MailPit REST api to verify).
```

Here are some other ideas for features that you could create plans for and
implement:

* Add capability to the interactive agent to add recommended items
to the cart (one or more from the recommendations) (would require
mcp changes)
* Add admin ui for reviewing placed orders
* Add ability to see changes to products made by admins - audit log
* Add ability for admins to create promotions / sales / discounts
temporarily for certain products
* Add support for guest (anonymous) checkout

## How we got here

> **NOTE:** These steps have been completed!  Earlier releases enable
> you to follow these steps if you want, but this final version of the
> repo has all of this work already completed.

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

using CarvedRock.Core;
using CarvedRock.Mcp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
var authServer = builder.Configuration.GetValue<string>("AuthServer")!;
var mcpServerUrl = builder.Configuration.GetValue<string>("McpServerUrl")!;

builder.Services.AddCors(options => // cors is required for mcp inspector with oauth
{
    options.AddPolicy("DevAll", policy => policy
       .AllowAnyOrigin()
       .AllowAnyMethod()
       .AllowAnyHeader());
});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = authServer;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidIssuer = authServer,
        ValidAudience = "api",
        NameClaimType = "email",
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // context.Exception.Message is the validation failure
            return Task.CompletedTask;
        },
    };
})
.AddMcp(options =>
{
    options.ResourceMetadata = new ProtectedResourceMetadata
    {
        Resource = "api",
        AuthorizationServers = { authServer },
        ScopesSupported = ["api"],
    };
});

builder.Services.AddAuthorization();
builder.Services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();

builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true; // important for scaling
        //options.ConfigureSessionOptions = AuthHelper.AuthorizeToolsForUser;
    })
    .WithPromptsFromAssembly()
    .WithToolsFromAssembly() // just get everything registered, then use authz attributes to filter
                             //.WithTools<CarvedRockTools>() 
                             //.WithTools<AdminTools>()
                             // Authorization Filter info: https://modelcontextprotocol.github.io/csharp-sdk/concepts/filters.html#built-in-authorization-filters
    .AddAuthorizationFilters();  // Add support for [Authorize] and [AllowAnonymous]

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TokenForwarder>();
builder.Services.AddHttpClient("CarvedRockApi",
        client => client.BaseAddress = new("https://api"))
    .AddHttpMessageHandler<TokenForwarder>(); ;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevAll");
}

app.UseAuthentication();
app.UseAuthorization();

var mcpEndpoint = app.MapMcp();
//.RequireAuthorization();  // this would require auth for **all** connections (even "initialize")
// only add if you don't have any anonymous tools to support

// The MCP Inspector resolves OAuth AS metadata against the MCP server URL rather than
// the auth server URL, so proxy it here to the actual authorization server.
app.MapGet("/.well-known/oauth-authorization-server", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{authServer}/.well-known/oauth-authorization-server");
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json");
}).AllowAnonymous();

app.Run();

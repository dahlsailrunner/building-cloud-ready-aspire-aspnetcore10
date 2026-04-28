using CarvedRock.Agent;
using CarvedRock.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(opts => opts.CustomizeProblemDetails = CustomizeProblemDetails);

// package: Aspire.Azure.AI.OpenAI
// builder.AddAzureOpenAIClient("kyt-AzureOpenAI", configureSettings: settings =>
// {
//     settings.EnableSensitiveTelemetryData = true;
//     settings.Endpoint = new Uri(builder.Configuration.GetValue<string>("AIConnection:Endpoint")!);
//     settings.Key = builder.Configuration.GetValue<string>("AIConnection:Key")!;
// }).AddChatClient(builder.Configuration.GetValue<string>("AIConnection:Deployment")!);

// package: Aspire.OpenAI
// Then add your API key for OpenAI to user secrets for the AIConnection:OpenAIKey value
builder.AddOpenAIClient("kyt-OpenAI", configureSettings: settings =>
{
    settings.EnableSensitiveTelemetryData = true;
    settings.Key = builder.Configuration.GetValue<string>("AIConnection:OpenAIKey");
}).AddChatClient(builder.Configuration.GetValue<string>("AIConnection:OpenAIModel"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Agent>();

var authority = builder.Configuration.GetValue<string>("Auth:Authority");
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = authority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email",
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

var oauthScopes = new Dictionary<string, string>
{
    { "api", "Resource access: Carved Rock API" },
    { "openid", "OpenID information" },
    { "profile", "User profile information" },
    { "email", "User email address" }
};

builder.Services.AddOpenApiWithAuth(builder.Configuration.GetValue<string>("Auth:Authority")!,
    oauthScopes);

builder.Services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes("oauth2")
        .AddAuthorizationCodeFlow("oauth2", flow =>
        {
            flow.ClientId = "interactive.public";
            flow.Pkce = Pkce.Sha256;
            flow.SelectedScopes = [.. oauthScopes.Keys];
        }));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/agent", async (string message, Agent agent, CancellationToken cancellationToken) =>
{
    return agent.GetAgentResponse(message, cancellationToken);
});

app.Run();

static void CustomizeProblemDetails(ProblemDetailsContext context)
{
    context.ProblemDetails.Detail = "Provide the instance value when contacting us for support";
    context.ProblemDetails.Instance = Activity.Current?.RootId;
}

record AIConnection(string Endpoint, string Key, string Deployment);

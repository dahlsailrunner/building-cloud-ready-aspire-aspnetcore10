using CarvedRock.Api;
using CarvedRock.Core;
using CarvedRock.Data;
using CarvedRock.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatorsFromAssemblyContaining<NewProductValidator>();
builder.Services.AddProblemDetails(opts => opts.CustomizeProblemDetails = CustomizeProblemDetails);
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration.GetValue<string>("Auth:Authority"); ;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email",
            ValidateAudience = false
        };
    });
builder.Services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();

builder.Services.AddControllers();

var oauthScopes = new Dictionary<string, string>
{
    { "api", "Resource access: Carved Rock API" },
    { "openid", "OpenID information" },
    { "profile", "User profile information" },
    { "email", "User email address" }
};

builder.Services.AddOpenApiWithAuth(builder.Configuration.GetValue<string>("Auth:Authority")!, 
    oauthScopes); 

builder.Services.AddScoped<IProductLogic, ProductLogic>();

var cstr = builder.Configuration.GetConnectionString("CarvedRockPostgres");
builder.Services.AddDbContext<LocalContext>(options =>
     options.UseNpgsql(cstr));

builder.Services.AddScoped<ICarvedRockRepository, CarvedRockRepository>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    SetupDevelopment(app, oauthScopes);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();

static void SetupDevelopment(WebApplication app, Dictionary<string, string> oauthScopes)
{
    using var scope = app.Services.CreateScope();

    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LocalContext>();
    context.MigrateAndCreateData();

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

static void CustomizeProblemDetails(ProblemDetailsContext context)
{
    context.ProblemDetails.Detail = "Provide the instance value when contacting us for support";
    context.ProblemDetails.Instance = Activity.Current?.RootId;
}

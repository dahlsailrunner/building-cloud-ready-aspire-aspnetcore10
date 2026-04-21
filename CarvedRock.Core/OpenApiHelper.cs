using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace CarvedRock.Core;

public static class OpenApiHelper
{
    public static IServiceCollection AddOpenApiWithAuth(this IServiceCollection services,
        string authority, Dictionary<string, string> scopes)
    {
        services.AddOpenApi(options =>
        {            
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes["oauth2"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{authority}/connect/authorize"),
                            TokenUrl = new Uri($"{authority}/connect/token"),
                            Scopes = scopes
                        }
                    }
                };

                document.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("oauth2", document)] = [.. scopes.Keys]
                    }
                ];

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.Client;

namespace CarvedRock.Agent;

public static class McpClientHelper
{
    public static async Task<McpClient> GetMcpClient(
        IConfiguration config,
        IHttpContextAccessor httpCtxAccessor,
        CancellationToken cxl)
    {
        var httpCtx = httpCtxAccessor.HttpContext!;
        if (httpCtx == null || httpCtx.User.Identity == null || !httpCtx.User.Identity.IsAuthenticated)
        {
            // anonymous user
            return await GetAnonymousClient(config, cxl);
        }
        else
        {
            // authenticated user
            return await GetTokenForwardingClient(httpCtxAccessor, config, cxl);
        }
    }

    public static async Task<McpClient> GetAnonymousClient(IConfiguration config, CancellationToken cxl)
    {
        var clientTransport = new HttpClientTransportOptions
        {
            Endpoint = new Uri(GetMcpServerUrl(config)),
            TransportMode = HttpTransportMode.StreamableHttp
        };
        return await McpClient.CreateAsync(new HttpClientTransport(clientTransport),
                                           cancellationToken: cxl);
    }

    public static async Task<McpClient> GetTokenForwardingClient(IHttpContextAccessor httpCtxAccessor,
        IConfiguration config, CancellationToken cxl)
    {
        var clientTransport = new HttpClientTransportOptions
        {
            Endpoint = new Uri(GetMcpServerUrl(config)),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>()
        };
        clientTransport.AdditionalHeaders.Add("Authorization",
                            await GetAccessTokenFromHttpContext(httpCtxAccessor));

        return await McpClient.CreateAsync(new HttpClientTransport(clientTransport),
                                           cancellationToken: cxl);
    }

    private static string GetMcpServerUrl(IConfiguration config)
    {
        return config.GetValue<string>("Services:mcp:https:0") // service discovery setup from Aspire
            ?? config.GetValue<string>("McpServer")            // production / testing deployments config
            ?? "http://localhost:5555";                        // not using 5241 to prove above works
    }

    private static async Task<string> GetAccessTokenFromHttpContext(IHttpContextAccessor httpCtxAccessor)
    {
        var httpContext = httpCtxAccessor.HttpContext;
        if (httpContext?.Request.Headers.Authorization.FirstOrDefault() is string authHeader &&
            authHeader.StartsWith($"{JwtBearerDefaults.AuthenticationScheme} ",
                    StringComparison.OrdinalIgnoreCase))
        {
            
            var accessToken = authHeader.Replace($"{JwtBearerDefaults.AuthenticationScheme} ", "",
                                    StringComparison.OrdinalIgnoreCase).Trim();
            //var newAccessToken = await GetDelegatedAccessTokenAsync(accessToken);
            return $"{JwtBearerDefaults.AuthenticationScheme} {accessToken}";
        }

        throw new Exception("Http Context does not have a bearer token.");
    }

    // TOKEN EXCHANGE example
    //https://docs.duendesoftware.com/identityserver/tokens/extension-grants/#token-exchange
    // private static async Task<string> GetDelegatedAccessTokenAsync(string accessToken)
    // {

    //     var idSrvUrl = "https://localhost:5001/"; // TODO: Get from config
    //     var client = new HttpClient() { BaseAddress = new Uri(idSrvUrl) };

    //     var response = await client.RequestTokenExchangeTokenAsync(new TokenExchangeTokenRequest
    //     {
    //         Address = "connect/token",
    //         ClientId = "ai.agent",
    //         ClientSecret = "secret",

    //         SubjectToken = accessToken,
    //         SubjectTokenType = OidcConstants.TokenTypeIdentifiers.AccessToken,
    //         Scope = "api email",

    //         Parameters =
    //         {
    //             { "exchange_style", "delegation" }
    //         }
    //     });

    //     return response.AccessToken!;
    // }
}

using Duende.IdentityModel.Client;

namespace CarvedRock.Tests.Utils;

public static class AuthHelper
{
    public static async Task<string> GetClientCredsAccessTokenAsync(
        string clientId, string secret,
        string scope = "api", CancellationToken cancellationToken = default)
    {
        var idSrvRoot = new Uri("https://demo.duendesoftware.com");
        var client = new HttpClient { BaseAddress = idSrvRoot };

        var response = await client.RequestClientCredentialsTokenAsync(
            new ClientCredentialsTokenRequest
            {
                Address = "connect/token",

                ClientId = clientId,
                ClientSecret = secret,
                Scope = scope,
            }, cancellationToken);

        if (response.IsError)
        {
            throw new Exception($"Error retrieving access " +
                        "token for clientId {clientId}: {response.Error}");
        }

        return response.AccessToken!;
    }

    public static async Task<string> GetUserAccessTokenAsync(AppFixture fixture,
        string username, string password,
        string scope = "openid profile email api",
        CancellationToken cancellationToken = default)
    {
        var idSrvRoot = fixture.App.GetEndpoint("idsrv", "https");
        var client = new HttpClient { BaseAddress = idSrvRoot };

        var response = await client.RequestPasswordTokenAsync(
            new PasswordTokenRequest
            {
                Address = "connect/token",

                ClientId = "testing.confidential",
                ClientSecret = "secret",
                Scope = scope,

                UserName = username,
                Password = password
            }, cancellationToken);

        if (response.IsError)
        {
            throw new Exception($"Error retrieving access " +
                    "token for user {username}: {response.Error}");
        }

        return response.AccessToken!;
    }
}
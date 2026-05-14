using System.Text.Json;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace CarvedRock.Tests.Utils;

public class AppFixture : IDisposable
{
    internal string? AdminUsername { get; set; }
    internal string? AdminPassword { get; set; }

    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(60);
    public readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public DistributedApplication App { get; private set; } = null!;

    public AppFixture()
    {
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        // takes longer than the xunit default timeout to spin up resources
        var cancellationToken = new CancellationTokenSource(_defaultTimeout).Token;
        var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.CarvedRock_AppHost>(cancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        AdminUsername = appHost.Configuration.GetValue<string>("Parameters:adminUsername")!;
        AdminPassword = appHost.Configuration.GetValue<string>("Parameters:adminPassword")!;

        App = await appHost
                .BuildAsync(cancellationToken)
                .WaitAsync(_defaultTimeout, cancellationToken);

        await App.StartAsync(cancellationToken)
                 .WaitAsync(_defaultTimeout, cancellationToken);

        // waiting for the webapp means that everything else should be running
        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("webapp", TestContext.Current.CancellationToken)
            .WaitAsync(_defaultTimeout, TestContext.Current.CancellationToken);
    }

    public async Task<McpClient> GetMcpClient(string? user = null,
                string? pwd = null, CancellationToken cancelToken = default)
    {
        if (user == null) return await GetAnonymousMcpClient(cancelToken);

        // must want an authenticated client        
        var accessToken = await AuthHelper.GetClientCredsAccessTokenAsync(
            user, pwd!, cancellationToken: cancelToken);

        var clientTransport = new HttpClientTransportOptions
        {
            Endpoint = App.GetEndpoint("mcp", "https"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>()
        };
        clientTransport.AdditionalHeaders
                .Add("Authorization", $"Bearer {accessToken}");

        return await McpClient.CreateAsync(
                new HttpClientTransport(clientTransport),
                cancellationToken: cancelToken);
    }

    private async Task<McpClient> GetAnonymousMcpClient(
        CancellationToken cancelToken = default)
    {
        var clientTransport = new HttpClientTransportOptions
        {
            Endpoint = App.GetEndpoint("mcp", "https"),
            TransportMode = HttpTransportMode.StreamableHttp
        };
        return await McpClient.CreateAsync(
            new HttpClientTransport(clientTransport),
            cancellationToken: cancelToken);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition("Integration test collection")]
public class IntegrationCollection : ICollectionFixture<AppFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
using System.Net.Http.Json;
using System.Text.Json;

namespace CarvedRock.Tests.Utils;

/// <summary>
/// Thin wrapper over the MailPit REST API (exposed on the "smtp" resource's
/// "http" endpoint) for verifying that emails were sent.
/// </summary>
public static class MailPitHelper
{
    // MailPit returns a lowercase "messages" array, but the message fields are
    // PascalCase ("Subject", "To", "Address"); case-insensitive handles both.
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static HttpClient CreateClient(AppFixture fixture)
    {
        var baseUrl = fixture.App.GetEndpoint("smtp", "http");
        return new HttpClient { BaseAddress = baseUrl };
    }

    public static async Task ClearInboxAsync(AppFixture fixture, CancellationToken ct = default)
    {
        using var client = CreateClient(fixture);
        var response = await client.DeleteAsync("/api/v1/messages", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Polls the inbox until a message with the given recipient and subject appears,
    /// or returns null after a short timeout.
    /// </summary>
    public static async Task<MailPitMessage?> WaitForMessageAsync(AppFixture fixture,
        string toAddress, string subject, CancellationToken ct = default)
    {
        using var client = CreateClient(fixture);

        for (var attempt = 0; attempt < 40; attempt++)
        {
            var list = await client.GetFromJsonAsync<MailPitMessagesResponse>(
                "/api/v1/messages", _json, ct);

            var match = list?.Messages?.FirstOrDefault(m =>
                string.Equals(m.Subject, subject, StringComparison.OrdinalIgnoreCase) &&
                m.To != null &&
                m.To.Any(t => string.Equals(t.Address, toAddress, StringComparison.OrdinalIgnoreCase)));

            if (match != null) return match;

            await Task.Delay(250, ct);
        }

        return null;
    }
}

public record MailPitMessagesResponse
{
    public List<MailPitMessage>? Messages { get; set; }
}

public record MailPitMessage
{
    public string Id { get; set; } = "";
    public string Subject { get; set; } = "";
    public List<MailPitAddress>? To { get; set; }
}

public record MailPitAddress
{
    public string Address { get; set; } = "";
    public string Name { get; set; } = "";
}

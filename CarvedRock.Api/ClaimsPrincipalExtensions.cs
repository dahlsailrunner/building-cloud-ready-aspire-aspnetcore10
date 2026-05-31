using System.Security.Claims;

namespace CarvedRock.Api;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Stable per-caller identifier used to key carts and orders. For interactive
    /// users this is the token "sub"; for machine-to-machine callers it falls back
    /// to "client_id". (Inbound claim mapping is disabled, so names are the raw JWT names.)
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value
            ?? user.FindFirst("client_id")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("No user identifier claim found on the token.");
    }

    /// <summary>
    /// Email claim if present on the token (it may not be, since access tokens
    /// don't always carry the email scope). Returns null when absent.
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst("email")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;
    }
}

using Microsoft.Playwright;

namespace CarvedRock.Tests.Utils;

public static class PlaywrightHelpers
{
    // Public Duende demo credentials. "alice" is a regular (non-admin) user;
    // "bob" is treated as admin by AdminClaimsTransformation.
    public const string AliceUsername = "alice";
    public const string AlicePassword = "alice";

    /// <summary>
    /// Drives the web app's OIDC login UI against the Duende demo IdentityServer.
    /// Navigates to the web app root first, then signs in with the given credentials.
    /// </summary>
    public static async Task LoginAsync(IPage page, Uri webAppUrl, string username, string password)
    {
        await page.GotoAsync(webAppUrl.AbsoluteUri);

        await page.GetByRole(AriaRole.Link, new() { Name = "Sign in" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username);
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).PressAsync("Tab");
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
    }
}

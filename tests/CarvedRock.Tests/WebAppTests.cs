using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace CarvedRock.Tests;

[Collection("Integration test collection")]
public class WebAppTests(AppFixture fixture) : PageTest
{
    // NOTE: Playwright is better for tests against UI projects
    [Fact]
    public async Task GetWebAppRootReturnsOk()
    {
        // Act        
        using var httpClient = fixture.App.CreateHttpClient("webapp");

        using var response = await httpClient.GetAsync("/",
                    TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddToCartWorks()
    {
        var url = fixture.App.GetEndpoint("webapp", "https");

        await Page.GotoAsync(url.AbsoluteUri);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Footwear" })
                    .ClickAsync();

        await Page.GetByRole(AriaRole.Row, new() { Name = "Desert Walker Desert Walker" })
                    .GetByRole(AriaRole.Button)
                    .ClickAsync();

        await Page.ScreenshotAsync(new() { Path = "cart-1-item.png" });
        await Expect(Page.Locator("#carvedrockcart")).ToContainTextAsync("Cart (1)");
    }

    [Fact]
    public async Task CanLoginAsAdminAndGoToAdminPage()
    {
        if (string.IsNullOrEmpty(fixture.AdminUsername) || string.IsNullOrEmpty(fixture.AdminPassword))
        {
            throw new Exception("Missing AdminUsername and/or Password - set parameters in User " +
                                "secrets via the Dashboard.");
        }

        var url = fixture.App.GetEndpoint("webapp", "https");
        await Page.GotoAsync(url.AbsoluteUri);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(fixture.AdminUsername);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(fixture.AdminPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Admin" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync("Create New");
        await Expect(Page.Locator("tbody")).ToContainTextAsync("Mountain Summit");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace CarvedRock.Tests;

[Collection("Integration test collection")]
public class CartOrderTests(AppFixture fixture) : PageTest
{
    [Fact]
    public async Task AddingToCartPersistsCartItemRowInDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        await DbTestHelper.ClearCartsAndOrdersAsync(fixture, ct);

        var url = fixture.App.GetEndpoint("webapp", "https");
        await PlaywrightHelpers.LoginAsync(Page, url,
            PlaywrightHelpers.AliceUsername, PlaywrightHelpers.AlicePassword);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Footwear" }).ClickAsync();
        await Page.GetByRole(AriaRole.Row, new() { Name = "Desert Walker Desert Walker" })
                    .GetByRole(AriaRole.Button)
                    .ClickAsync();

        await Expect(Page.Locator("#carvedrockcart")).ToContainTextAsync("Cart (1)");

        // The cart item should now exist in the database, keyed to a user id.
        await using var db = await DbTestHelper.CreateContextAsync(fixture, ct);
        var product = await db.Products.FirstAsync(p => p.Name == "Desert Walker", ct);

        var cartItems = await db.CartItems.ToListAsync(ct);
        var cartItem = Assert.Single(cartItems);
        Assert.Equal(product.Id, cartItem.ProductId);
        Assert.Equal(1, cartItem.Quantity);
        Assert.False(string.IsNullOrWhiteSpace(cartItem.UserId));
    }

    [Fact]
    public async Task CompletingOrderCreatesOrderRowsClearsCartAndSendsEmail()
    {
        var ct = TestContext.Current.CancellationToken;
        await DbTestHelper.ClearCartsAndOrdersAsync(fixture, ct);
        await MailPitHelper.ClearInboxAsync(fixture, ct);

        var url = fixture.App.GetEndpoint("webapp", "https");
        await PlaywrightHelpers.LoginAsync(Page, url,
            PlaywrightHelpers.AliceUsername, PlaywrightHelpers.AlicePassword);

        // Add an item to the cart.
        await Page.GetByRole(AriaRole.Link, new() { Name = "Footwear" }).ClickAsync();
        await Page.GetByRole(AriaRole.Row, new() { Name = "Desert Walker Desert Walker" })
                    .GetByRole(AriaRole.Button)
                    .ClickAsync();
        await Expect(Page.Locator("#carvedrockcart")).ToContainTextAsync("Cart (1)");

        // Cart -> Checkout -> Submit Order.
        await Page.Locator("#carvedrockcart").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Checkout" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit Order" }).ClickAsync();

        await Expect(Page.Locator("h1")).ToContainTextAsync("Thanks for your");

        // Verify the order and its details were written to the database.
        await using var db = await DbTestHelper.CreateContextAsync(fixture, ct);
        var product = await db.Products.FirstAsync(p => p.Name == "Desert Walker", ct);

        var orders = await db.Orders.Include(o => o.Details).ToListAsync(ct);
        var order = Assert.Single(orders);
        Assert.False(string.IsNullOrWhiteSpace(order.Email));
        Assert.Contains("alice", order.Email, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(product.Price, order.Total);

        var detail = Assert.Single(order.Details);
        Assert.Equal(product.Id, detail.ProductId);
        Assert.Equal("Desert Walker", detail.ProductName);
        Assert.Equal(1, detail.Quantity);

        // The cart should have been cleared as part of placing the order.
        var remainingCart = await db.CartItems.ToListAsync(ct);
        Assert.Empty(remainingCart);

        // Verify the confirmation email landed in MailPit.
        var message = await MailPitHelper.WaitForMessageAsync(fixture,
            order.Email, "Your CarvedRock Order", ct);
        Assert.NotNull(message);
    }
}

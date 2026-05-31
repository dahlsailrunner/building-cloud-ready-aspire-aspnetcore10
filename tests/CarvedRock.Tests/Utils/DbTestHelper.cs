using CarvedRock.Data;
using Microsoft.EntityFrameworkCore;

namespace CarvedRock.Tests.Utils;

/// <summary>
/// Builds a LocalContext against the running Aspire Postgres resource so tests
/// can assert side effects (cart/order rows) directly in the database.
/// </summary>
public static class DbTestHelper
{
    public static async Task<LocalContext> CreateContextAsync(AppFixture fixture,
        CancellationToken ct = default)
    {
        var connectionString = await fixture.App.GetConnectionStringAsync("CarvedRockPostgres", ct)
            ?? throw new InvalidOperationException("No connection string for CarvedRockPostgres.");

        var options = new DbContextOptionsBuilder<LocalContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LocalContext(options);
    }

    /// <summary>
    /// Removes all cart and order rows so each test starts from a known, isolated state.
    /// (The /internal/reset-data command only reseeds Products.)
    /// </summary>
    public static async Task ClearCartsAndOrdersAsync(AppFixture fixture,
        CancellationToken ct = default)
    {
        await using var ctx = await CreateContextAsync(fixture, ct);
        await ctx.OrderDetails.ExecuteDeleteAsync(ct);
        await ctx.Orders.ExecuteDeleteAsync(ct);
        await ctx.CartItems.ExecuteDeleteAsync(ct);
    }
}

using CarvedRock.Core;
using CarvedRock.Data;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CarvedRock.Domain;

public class CartLogic(ICarvedRockRepository repo,
            IValidator<AddToCartModel> addToCartValidator,
            ILogger<CartLogic> logger) : ICartLogic
{
    public async Task<List<CartItemModel>> GetCartAsync(string userId)
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object> { ["userId"] = userId });

        var items = await repo.GetCartItemsAsync(userId);
        var products = await repo.GetProductsByIdsAsync(items.Select(i => i.ProductId));
        var productsById = products.ToDictionary(p => p.Id);

        var result = new List<CartItemModel>();
        foreach (var item in items)
        {
            if (!productsById.TryGetValue(item.ProductId, out var product)) continue;

            result.Add(new CartItemModel
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Name = product.Name,
                Category = product.Category,
                Price = product.Price,
                Total = product.Price * item.Quantity
            });
        }
        return result;
    }

    public async Task<int> GetCartItemCountAsync(string userId)
    {
        return await repo.GetCartItemCountAsync(userId);
    }

    public async Task AddToCartAsync(string userId, AddToCartModel item)
    {
        await addToCartValidator.ValidateAndThrowAsync(item);

        logger.LogInformation("Adding product {ProductId} (qty {Quantity}) to cart for {UserId}",
            item.ProductId, item.Quantity, userId);

        await repo.AddOrIncrementCartItemAsync(userId, item.ProductId, item.Quantity);
    }

    public async Task ClearCartAsync(string userId)
    {
        await repo.ClearCartAsync(userId);
    }
}

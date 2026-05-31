using CarvedRock.Core;

namespace CarvedRock.Domain;

public interface ICartLogic
{
    Task<List<CartItemModel>> GetCartAsync(string userId);
    Task AddToCartAsync(string userId, AddToCartModel item);
    Task ClearCartAsync(string userId);
}

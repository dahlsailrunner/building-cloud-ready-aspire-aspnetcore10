using CarvedRock.Data.Entities;

namespace CarvedRock.Data;

public interface ICarvedRockRepository
{
    Task<List<Product>> GetProductsAsync(string category);
    Task<Product?> GetProductByIdAsync(int id);        
    Task<bool> IsProductNameUniqueAsync(string name);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(Product product);
    Task DeleteProductAsync(int id);

    Task<List<CartItem>> GetCartItemsAsync(string userId);
    Task AddOrIncrementCartItemAsync(string userId, int productId, int quantity);
    Task ClearCartAsync(string userId);

    Task<Order> CreateOrderAsync(Order order);
    Task<List<Order>> GetOrdersForUserAsync(string userId);
}

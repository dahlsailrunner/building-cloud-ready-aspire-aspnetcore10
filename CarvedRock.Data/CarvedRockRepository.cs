using CarvedRock.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarvedRock.Data;

public class CarvedRockRepository(LocalContext ctx, ILogger<CarvedRockRepository> logger)
        : ICarvedRockRepository
{
    public async Task<List<Product>> GetProductsAsync(string category)
    {
        List<string> validCategories = ["kayak", "equip", "boots", "all"];

        if (!validCategories.Contains(category))
        {
            var ex = new Exception("Simulated exception for category!");
            ex.Data["Category"] = category;
            throw ex;
            //throw new Exception($"Simulated exception for category {category}");
        }

        logger.LogInformation("Querying database.");
        return await ctx.Products.Where(p => p.Category == category || category == "all")
            .OrderBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await ctx.Products.FindAsync(id);
    }

    public async Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await ctx.Products
            .Where(p => idList.Contains(p.Id))
            .ToListAsync();
    }

    public Task<bool> IsProductNameUniqueAsync(string name)
    {
        return ctx.Products.AllAsync(p => p.Name != name);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        product.Name = product.Name!.Trim();
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        var existingProduct = await ctx.Products.FindAsync(product.Id)
            ?? throw new KeyNotFoundException($"Product with ID {product.Id} not found");

        existingProduct.Name = product.Name!.Trim();
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.Category = product.Category;
        existingProduct.ImgUrl = product.ImgUrl;

        await ctx.SaveChangesAsync();
        return existingProduct;
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await ctx.Products.FindAsync(id)
            ?? throw new KeyNotFoundException($"Product with ID {id} not found");

        ctx.Products.Remove(product);
        await ctx.SaveChangesAsync();
    }

    public async Task<List<CartItem>> GetCartItemsAsync(string userId)
    {
        return await ctx.CartItems.Where(c => c.UserId == userId)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<int> GetCartItemCountAsync(string userId)
    {
        return await ctx.CartItems
            .Where(c => c.UserId == userId)
            .SumAsync(c => c.Quantity);
    }

    public async Task AddOrIncrementCartItemAsync(string userId, int productId, int quantity)
    {
        var existing = await ctx.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

        if (existing == null)
        {
            ctx.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductId = productId,
                Quantity = quantity
            });
        }
        else
        {
            existing.Quantity += quantity;
        }

        await ctx.SaveChangesAsync();
    }

    public async Task ClearCartAsync(string userId)
    {
        await ctx.CartItems
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync();
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();
        return order;
    }

    public async Task<List<Order>> GetOrdersForUserAsync(string userId)
    {
        return await ctx.Orders.Where(o => o.UserId == userId)
            .Include(o => o.Details)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}

using CarvedRock.Core;
using CarvedRock.Data.Entities;
using FluentValidation.Results;

namespace CarvedRock.Domain;

public interface IProductLogic
{
    Task<IEnumerable<Product>> GetProductsForCategoryAsync(string category);
    Task<Product?> GetProductByIdAsync(int id);
    Task<ProductModel> CreateProductAsync(NewProductModel newProduct);
    Task<ProductModel> UpdateProductAsync(int id, NewProductModel updatedProduct);
    Task DeleteProductAsync(int id);
}
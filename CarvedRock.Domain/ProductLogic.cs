using CarvedRock.Core;
using CarvedRock.Data;
using CarvedRock.Data.Entities;
using CarvedRock.Domain.Mapping;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CarvedRock.Domain;

public class ProductLogic(ICarvedRockRepository repo,
            IValidator<NewProductModel> newProductValidator,
            ILogger<ProductLogic> logger) : IProductLogic
{
    public async Task<IEnumerable<Product>> GetProductsForCategoryAsync(string category)
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object> { ["category"] = category });

        logger.LogInformation("Calling repository.");
        return await repo.GetProductsAsync(category);
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await repo.GetProductByIdAsync(id);
    }

    public async Task<ProductModel> CreateProductAsync(NewProductModel newProduct)
    {
        await newProductValidator.ValidateAndThrowAsync(newProduct);

        var productMapper = new ProductMapper();

        var productToCreate = productMapper.NewProductModelToProduct(newProduct);
        var createdProduct = await repo.CreateProductAsync(productToCreate);
        return productMapper.ProductToProductModel(createdProduct);
    }

    public async Task<ProductModel> UpdateProductAsync(int id, NewProductModel updatedProduct)
    {
        var productMapper = new ProductMapper();
        var productToUpdate = productMapper.NewProductModelToProduct(updatedProduct);
        productToUpdate.Id = id;

        var updatedProductEntity = await repo.UpdateProductAsync(productToUpdate);
        return productMapper.ProductToProductModel(updatedProductEntity);
    }

    public async Task DeleteProductAsync(int id)
    {
        await repo.DeleteProductAsync(id);
    }
}
using CarvedRock.Core;
using CarvedRock.Data.Entities;
using CarvedRock.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRock.Api.Controllers;

[ApiController]
[Route("[controller]")]
public partial class ProductController(IProductLogic productLogic,
                            ILogger<ProductController> logger) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IEnumerable<Product>> Get(string category = "all")
    {
        logger.LogInformation("Calling product logic.");
        return await productLogic.GetProductsForCategoryAsync(category);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var product = await productLogic.GetProductByIdAsync(id);
        if (product != null)
        {
            return Ok(product);
        }
        return NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType<ProductModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] NewProductModel newProduct)
    {
        var createdProduct = await productLogic.CreateProductAsync(newProduct);

        var uri = Request.Path.Value + $"/{createdProduct!.Id}";
        return Created(uri, createdProduct);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType<ProductModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] NewProductModel updatedProduct)
    {
        try
        {
            var result = await productLogic.UpdateProductAsync(id, updatedProduct);
            return Ok(result);
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            await productLogic.DeleteProductAsync(id);
            return NoContent();
        }
        catch
        {
            return NotFound();
        }
    }
}
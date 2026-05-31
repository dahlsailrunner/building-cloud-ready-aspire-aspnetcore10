using CarvedRock.Core;
using CarvedRock.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRock.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CartController(ICartLogic cartLogic,
                    ILogger<CartController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<CartItemModel>> Get()
    {
        return await cartLogic.GetCartAsync(User.GetUserId());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add([FromBody] AddToCartModel item)
    {
        logger.LogInformation("Adding product {ProductId} to cart.", item.ProductId);
        await cartLogic.AddToCartAsync(User.GetUserId(), item);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await cartLogic.ClearCartAsync(User.GetUserId());
        return NoContent();
    }
}

using CarvedRock.Core;
using CarvedRock.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRock.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class OrderController(IOrderLogic orderLogic,
                    ILogger<OrderController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrderModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder([FromBody] NewOrderModel newOrder)
    {
        var userId = User.GetUserId();
        // The access token may not carry the email claim, so the caller (the web app,
        // which has the user's email from OIDC) passes it in the request body.
        var email = !string.IsNullOrWhiteSpace(newOrder?.Email)
            ? newOrder.Email
            : User.GetEmail() ?? "unknown@carvedrock.com";

        try
        {
            var order = await orderLogic.PlaceOrderAsync(userId, email);
            logger.LogInformation("Placed order {OrderId} for {UserId}.", order.Id, userId);
            return Created($"/order/{order.Id}", order);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

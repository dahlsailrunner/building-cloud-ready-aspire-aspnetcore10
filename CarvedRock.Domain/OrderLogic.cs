using CarvedRock.Core;
using CarvedRock.Data;
using CarvedRock.Data.Entities;
using Microsoft.Extensions.Logging;

namespace CarvedRock.Domain;

public class OrderLogic(ICarvedRockRepository repo,
            IOrderEmailSender emailSender,
            ILogger<OrderLogic> logger) : IOrderLogic
{
    public async Task<OrderModel> PlaceOrderAsync(string userId, string email)
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object> { ["userId"] = userId });

        var cartItems = await repo.GetCartItemsAsync(userId);
        if (cartItems.Count == 0)
        {
            throw new InvalidOperationException("Cannot place an order with an empty cart.");
        }

        var order = new Order
        {
            UserId = userId,
            Email = email,
            OrderDate = DateTime.UtcNow,
            Details = []
        };

        foreach (var item in cartItems)
        {
            var product = await repo.GetProductByIdAsync(item.ProductId);
            if (product == null) continue;

            order.Details.Add(new OrderDetail
            {
                ProductId = item.ProductId,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                LineTotal = product.Price * item.Quantity
            });
        }
        order.Total = order.Details.Sum(d => d.LineTotal);

        var created = await repo.CreateOrderAsync(order);
        logger.LogInformation("Created order {OrderId} with {LineCount} line(s).",
            created.Id, created.Details.Count);

        await repo.ClearCartAsync(userId);

        await emailSender.SendOrderConfirmationAsync(created);

        return MapToModel(created);
    }

    private static OrderModel MapToModel(Order order) => new()
    {
        Id = order.Id,
        Email = order.Email,
        OrderDate = order.OrderDate,
        Total = order.Total,
        Details = order.Details.Select(d => new OrderDetailModel
        {
            ProductId = d.ProductId,
            ProductName = d.ProductName,
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice,
            LineTotal = d.LineTotal
        }).ToList()
    };
}

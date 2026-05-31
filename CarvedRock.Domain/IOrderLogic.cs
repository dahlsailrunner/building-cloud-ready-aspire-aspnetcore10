using CarvedRock.Core;

namespace CarvedRock.Domain;

public interface IOrderLogic
{
    Task<OrderModel> PlaceOrderAsync(string userId, string email);
}

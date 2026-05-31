using CarvedRock.Core;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;

namespace CarvedRock.WebApp;

public interface ICartService
{
    Task<List<CartItemModel>> GetCartAsync();
    Task<int> GetCartItemCountAsync();
    Task AddToCartAsync(int productId, int quantity = 1);
    Task ClearCartAsync();
    Task<OrderModel?> PlaceOrderAsync(string email);
}

public class CartService : ICartService
{
    private readonly IHttpContextAccessor _httpCtxAccessor;
    private readonly ILogger<CartService> _logger;

    private HttpClient Client { get; }

    public CartService(HttpClient client, IConfiguration config,
        IHttpContextAccessor httpCtxAccessor, ILogger<CartService> logger)
    {
        client.BaseAddress = new Uri(
            config.GetValue<string>("CarvedRock:ApiBaseUrl") ?? "https://api"
        );

        Client = client;
        _httpCtxAccessor = httpCtxAccessor;
        _logger = logger;
    }

    private async Task SetAuthorizationHeader()
    {
        var httpCtx = _httpCtxAccessor.HttpContext;
        if (httpCtx != null)
        {
            var accessToken = await httpCtx.GetTokenAsync("access_token");
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    public async Task<List<CartItemModel>> GetCartAsync()
    {
        await SetAuthorizationHeader();
        return await Client.GetFromJsonAsync<List<CartItemModel>>("Cart") ?? [];
    }

    public async Task<int> GetCartItemCountAsync()
    {
        var items = await GetCartAsync();
        return items.Sum(i => i.Quantity);
    }

    public async Task AddToCartAsync(int productId, int quantity = 1)
    {
        await SetAuthorizationHeader();
        var response = await Client.PostAsJsonAsync("Cart",
            new AddToCartModel { ProductId = productId, Quantity = quantity });
        response.EnsureSuccessStatusCode();
    }

    public async Task ClearCartAsync()
    {
        await SetAuthorizationHeader();
        var response = await Client.DeleteAsync("Cart");
        response.EnsureSuccessStatusCode();
    }

    public async Task<OrderModel?> PlaceOrderAsync(string email)
    {
        await SetAuthorizationHeader();
        var response = await Client.PostAsJsonAsync("Order", new NewOrderModel { Email = email });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderModel>();
    }
}

using CarvedRock.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CarvedRock.WebApp;

public interface IProductService
{
    Task<List<ProductModel>> GetProductsAsync(string category = "all");
    Task<ProductModel?> GetProductByIdAsync(int id);
    Task<IDictionary<string, string>> AddProductAsync(NewProductModel newProduct);
    Task<IDictionary<string, string>> UpdateProductAsync(int id, NewProductModel product);
    Task<bool> DeleteProductAsync(int id);
}

public class ProductService : IProductService
{
    private readonly IHttpContextAccessor _httpCtxAccessor;
    private readonly ILogger<ProductService> logger;

    private HttpClient Client { get; }

    public ProductService(HttpClient client, IConfiguration config,
        IHttpContextAccessor httpCtxAccessor, ILogger<ProductService> logger)
    {
        //client.BaseAddress = new Uri(config.GetValue<string>("CarvedRock:ApiBaseUrl")!);
        client.BaseAddress = new Uri("https+http://api");
        Client = client;
        _httpCtxAccessor = httpCtxAccessor;
        this.logger = logger;
    }

    private async Task SetAuthorizationHeader()
    {
        var _httpCtx = _httpCtxAccessor.HttpContext;
        if (_httpCtx != null)
        {
            var accessToken = await _httpCtx.GetTokenAsync("access_token");
            Client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
            // for a better way to include and manage access tokens for API calls:
            // https://identitymodel.readthedocs.io/en/latest/aspnetcore/web.html
        }
    }

    public async Task<List<ProductModel>> GetProductsAsync(string category = "all")
    {
        await SetAuthorizationHeader();

        var response = await Client.GetAsync($"Product?category={category}");
        if (!response.IsSuccessStatusCode)
        {
            var fullPath = $"{Client.BaseAddress}Product?category={category}";

            var details = await response.Content.ReadFromJsonAsync<ProblemDetails>()
                                    ?? new ProblemDetails();
            //var content = await response.Content.ReadAsStringAsync();
            //var details = JsonSerializer.Deserialize<ProblemDetails>(content) ?? new ProblemDetails();
            var traceId = details.Extensions["traceId"]?.ToString();

            // logger.LogWarning("API failure: {fullPath} Response: {apiResponse}, Trace: {trace}" + 
            //   "Instance: {instance}",
            //     fullPath, (int)response.StatusCode, traceId, details.Instance);

            var ex = new Exception("API call failed!");
            ex.Data["URL"] = fullPath;
            ex.Data["Response"] = (int)response.StatusCode;
            ex.Data["Trace"] = traceId;
            ex.Data["Instance"] = details.Instance;

            throw ex;
        }

        return await response.Content.ReadFromJsonAsync<List<ProductModel>>() ?? [];
    }

    public async Task<ProductModel?> GetProductByIdAsync(int id)
    {
        await SetAuthorizationHeader();

        return await Client.GetFromJsonAsync<ProductModel?>($"Product/{id}");
    }

    public async Task<IDictionary<string, string>> AddProductAsync(NewProductModel newProduct)
    {
        await SetAuthorizationHeader();

        await Client.PostAsJsonAsync("Product", newProduct);
        return new Dictionary<string, string>();
    }

    public async Task<IDictionary<string, string>> UpdateProductAsync(int id, NewProductModel product)
    {
        await SetAuthorizationHeader();

        await Client.PutAsJsonAsync($"Product/{id}", product);
        return new Dictionary<string, string>();
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        await SetAuthorizationHeader();

        var response = await Client.DeleteAsync($"Product/{id}");
        return response.IsSuccessStatusCode;
    }
}

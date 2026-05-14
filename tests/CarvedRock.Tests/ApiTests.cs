using System.Net.Http.Json;

namespace CarvedRock.Tests;

[Collection("Integration test collection")]
public class ApiTests(AppFixture fixture)
{
    [Fact]
    public async Task GetAllProductsReturnsAllProducts()
    {
        // Act
        using var httpClient = fixture.App.CreateHttpClient("api");

        var response = await httpClient
            .GetFromJsonAsync<List<ProductModel>>("/product",
                    TestContext.Current.CancellationToken);

        // Assert
        var productNames = response!.Select(p => p.Name).ToList();
        Assert.Contains("Alpine Trekker", productNames); // first one in SeedData.json
        Assert.Contains("Trail Running Hybrid Boot", productNames); // last one

        // might not be correct based on timing of delete or add operations
        // Assert.Equal(50, response?.Count); 
    }
}
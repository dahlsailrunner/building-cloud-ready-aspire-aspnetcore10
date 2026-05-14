using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace CarvedRock.Tests;

[Collection("Integration test collection")]
public class McpServerTests(AppFixture fixture)
{
    [Theory]
    [InlineData("m2m", "secret")] // non-admin
    [InlineData("m2m.short", "secret")]
    public async Task GetToolsIncludesGetProducts(string user, string pwd)
    {
        var mcpClient = await fixture.GetMcpClient(user, pwd,
                            TestContext.Current.CancellationToken);

        var tools = await mcpClient.ListToolsAsync(
                            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var getProductsTool = tools.FirstOrDefault(t => t.Name == "get_products");
        Assert.NotNull(getProductsTool);
    }

    [Fact]
    public async Task CallGetProductsToolReturnsProducts()
    {
        var mcpClient = await fixture.GetMcpClient("m2m", "secret",
                            TestContext.Current.CancellationToken);

        //Act
        var getProductsResponse = await mcpClient.CallToolAsync(
            "get_products", cancellationToken: TestContext.Current.CancellationToken);

        //Assert
        Assert.NotNull(getProductsResponse);
        Assert.NotEqual(true, getProductsResponse.IsError);

        var productJson = getProductsResponse.Content
                            .First(c => c.Type == "text") as TextContentBlock;
        var products = JsonSerializer.Deserialize<List<ProductModel>>(
            productJson?.Text ?? "[]",
            fixture.JsonSerializerOptions);

        Assert.NotNull(products);
        Assert.Contains(products!, p => p.Name == "Alpine Trekker");
    }

    [Fact]
    public async Task ListToolsDoesNotHaveAdminToolsForNormalUser()
    {
        var mcpClient = await fixture.GetMcpClient("m2m", "secret",
                        TestContext.Current.CancellationToken);

        var tools = await mcpClient.ListToolsAsync(
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tools);
        Assert.Equal(2, tools.Count);

        var adminTool = tools.FirstOrDefault(t => t.Name == "delete_product");
        Assert.Null(adminTool);

        adminTool = tools.FirstOrDefault(t => t.Name == "set_product_price");
        Assert.Null(adminTool);
    }

    [Fact]
    public async Task ListToolsHasAdminToolsForAdmin()
    {
        var mcpClient = await fixture.GetMcpClient("m2m.short", "secret",
                            TestContext.Current.CancellationToken);

        var tools = await mcpClient.ListToolsAsync(
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tools);
        Assert.Equal(4, tools.Count);

        var adminTool = tools.FirstOrDefault(t => t.Name == "delete_product");
        Assert.NotNull(adminTool);

        adminTool = tools.FirstOrDefault(t => t.Name == "set_product_price");
        Assert.NotNull(adminTool);
    }

    [Fact]
    public async Task DeleteProductWorksForAdmin()
    {
        var mcpClient = await fixture.GetMcpClient("m2m.short", "secret",
                            TestContext.Current.CancellationToken);

        var response = await mcpClient.CallToolAsync("delete_product",
            new Dictionary<string, object?>
            {
                {"id", 22}
            },
            cancellationToken: TestContext.Current.CancellationToken);

        var responseJson = response.Content
                    .First(c => c.Type == "text") as TextContentBlock;

        var opResult = JsonSerializer
            .Deserialize<OperationResult>(responseJson?.Text ?? "{}",
                    fixture.JsonSerializerOptions);

        Assert.NotNull(response);
        Assert.Equal("ok", opResult?.Status);
    }

    [Fact]
    public async Task DeleteProductDoesNotWorkForNonAdmin()
    {
        var mcpClient = await fixture.GetMcpClient("m2m", "secret",
                TestContext.Current.CancellationToken);

        try
        {
            var response = await mcpClient.CallToolAsync("delete_product",
            new Dictionary<string, object?>
            {
                {"id", 1}
            },
            cancellationToken: TestContext.Current.CancellationToken);
        }
        catch (McpException ex)
        {
            Assert.Contains("requires authorization", ex.Message);
            return;
        }
        Assert.Fail("Expected an McpException to be thrown.");
    }
}
namespace CarvedRock.IntegrationTests;

[Collection("Integration test collection")]
public class WebAppTests(AppFixture fixture)
{
    // NOTE: Playwright is better for tests against UI projects
    [Fact]
    public async Task GetWebAppRootReturnsOkStatusCode()
    {
        // Act
        using var httpClient = fixture.App.CreateHttpClient("webapp");

        using var response = await httpClient.GetAsync("/",
                    TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

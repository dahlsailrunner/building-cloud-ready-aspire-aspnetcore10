using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Chat;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;

namespace CarvedRock.Agent;

public class Agent(IChatClient chatClient,
            IConfiguration config,
            ILogger<Agent> logger,
            IHttpContextAccessor httpCtxAccessor)
{
    public async IAsyncEnumerable<string> GetAgentResponse(string message,
        [EnumeratorCancellation] CancellationToken cxl)
    {
        logger.LogInformation("Got into the Agent method.");
        var mcpClient = await McpClientHelper.GetMcpClient(config, httpCtxAccessor, cxl);

        var tools = await mcpClient.ListToolsAsync(cancellationToken: cxl);

        var prompt = await GetPromptAsync(message, mcpClient,
            httpCtxAccessor.HttpContext?.User, cxl);

        var agent = chatClient.AsAIAgent(
            instructions: prompt,
            name: "CarvedRock Assistant",
            tools: [.. tools]);

        // give some product recommendations for a mountain hike?
        await foreach (var update in
                    agent.RunStreamingAsync(message, cancellationToken: cxl))
        {
            yield return update.ToString();
        }
    }

    private static async Task<string> GetPromptAsync(string message, McpClient mcpClient,
                                    ClaimsPrincipal? user, CancellationToken cxl)
    {
        if (message.StartsWith("/admin", StringComparison.InvariantCultureIgnoreCase) &&
            (user?.IsInRole("admin") ?? false))
        {
            var prompt = await mcpClient.GetPromptAsync("admin_prompt", cancellationToken: cxl);
            var adminPrompt = new StringBuilder();
            foreach (var msg in prompt.Messages)
            {
                adminPrompt.AppendLine((msg.Content as TextContentBlock)!.Text);
            }
            return adminPrompt.ToString();
        }

        return
            """
        You are an assistant that can make recommendations about CarvedRock products.  
        Limit product recommendations to 3 for any request. 
        If you can't help with a request, please say so politely.
        """;
    }
}
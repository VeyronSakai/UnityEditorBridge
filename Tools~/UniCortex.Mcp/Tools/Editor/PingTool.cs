using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;
using UniCortex.Mcp.Domains.Interfaces;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class PingTool(IHttpClientFactory httpClientFactory, IUnityServerUrlProvider urlProvider)
{
    [McpServerTool(Name = "editor_ping", ReadOnly = true), Description("Check connectivity with the Unity Editor."),
     UsedImplicitly]
    public async Task<CallToolResult> Ping(CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("UniCortex");
            var baseUrl = urlProvider.GetUrl();
            var jsonOptions = new JsonSerializerOptions { IncludeFields = true };

            var response = await httpClient.GetAsync(baseUrl + ApiRoutes.Ping, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var ping = JsonSerializer.Deserialize<PingResponse>(json, jsonOptions)!;
            return new CallToolResult { Content = [new TextContentBlock { Text = ping.message }] };
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }
}

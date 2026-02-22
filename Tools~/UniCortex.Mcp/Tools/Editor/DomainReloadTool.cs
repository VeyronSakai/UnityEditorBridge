using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;
using UniCortex.Mcp.Domains.Interfaces;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class DomainReloadTool(IHttpClientFactory httpClientFactory, IUnityServerUrlProvider urlProvider)
{
    [McpServerTool(Name = "editor_domain_reload", ReadOnly = false),
     Description("Request a domain reload (script recompilation) in the Unity Editor."), UsedImplicitly]
    public async Task<CallToolResult> DomainReload(CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("UniCortex");
            var baseUrl = urlProvider.GetUrl();

            var response = await httpClient.PostAsync(baseUrl + ApiRoutes.DomainReload, null, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Poll /ping to wait for the server to come back after domain reload.
            // DomainReloadRetryHandler handles retries during the reload.
            var pingResponse = await httpClient.GetAsync(baseUrl + ApiRoutes.Ping, cancellationToken);
            pingResponse.EnsureSuccessStatusCode();

            return new CallToolResult { Content = [new TextContentBlock { Text = "Domain reload completed successfully." }] };
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }
}

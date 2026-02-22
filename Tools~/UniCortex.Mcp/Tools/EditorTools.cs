using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;
using UniCortex.Mcp.Domains.Interfaces;
using UniCortex.Mcp.UseCases;

namespace UniCortex.Mcp.Tools;

[McpServerToolType, UsedImplicitly]
public class EditorTools(IHttpClientFactory httpClientFactory, IUnityServerUrlProvider urlProvider)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UniCortex");
    private readonly JsonSerializerOptions _jsonOptions = new() { IncludeFields = true };

    [McpServerTool(ReadOnly = true), Description("Check connectivity with the Unity Editor."), UsedImplicitly]
    public async Task<CallToolResult> PingEditor(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = urlProvider.GetUrl();
            var response = await _httpClient.GetAsync(baseUrl + ApiRoutes.Ping, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var ping = JsonSerializer.Deserialize<PingResponse>(json, _jsonOptions)!;
            return new CallToolResult { Content = [new TextContentBlock { Text = ping.message }] };
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }

    [McpServerTool(ReadOnly = false), Description("Start Play Mode in the Unity Editor."), UsedImplicitly]
    public async Task<CallToolResult> EnterPlayMode(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = urlProvider.GetUrl();
            await DomainReloadUseCase.ReloadAsync(_httpClient, baseUrl, cancellationToken);

            var response = await _httpClient.PostAsync(baseUrl + ApiRoutes.Play, null, cancellationToken);
            response.EnsureSuccessStatusCode();

            while (true)
            {
                var statusResponse = await _httpClient.GetAsync(baseUrl + ApiRoutes.Status, cancellationToken);
                statusResponse.EnsureSuccessStatusCode();
                var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, _jsonOptions)!;
                if (status.isPlaying)
                {
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Text = "Play mode started successfully." }]
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }

    [McpServerTool(ReadOnly = false), Description("Stop Play Mode in the Unity Editor."), UsedImplicitly]
    public async Task<CallToolResult> ExitPlayMode(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = urlProvider.GetUrl();
            var response = await _httpClient.PostAsync(baseUrl + ApiRoutes.Stop, null, cancellationToken);
            response.EnsureSuccessStatusCode();

            while (true)
            {
                var statusResponse = await _httpClient.GetAsync(baseUrl + ApiRoutes.Status, cancellationToken);
                statusResponse.EnsureSuccessStatusCode();
                var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, _jsonOptions)!;
                if (!status.isPlaying)
                {
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Text = "Play mode stopped successfully." }]
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }

    [McpServerTool(ReadOnly = false), Description("Pause the Unity Editor."), UsedImplicitly]
    public async Task<CallToolResult> PauseEditor(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = urlProvider.GetUrl();
            var response = await _httpClient.PostAsync(baseUrl + ApiRoutes.Pause, null, cancellationToken);
            response.EnsureSuccessStatusCode();

            while (true)
            {
                var statusResponse = await _httpClient.GetAsync(baseUrl + ApiRoutes.Status, cancellationToken);
                statusResponse.EnsureSuccessStatusCode();
                var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, _jsonOptions)!;
                if (status.isPaused)
                {
                    return new CallToolResult { Content = [new TextContentBlock { Text = "Paused successfully." }] };
                }
            }
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }

    [McpServerTool(ReadOnly = false), Description("Resume the Unity Editor."), UsedImplicitly]
    public async Task<CallToolResult> ResumeEditor(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = urlProvider.GetUrl();
            var response = await _httpClient.PostAsync(baseUrl + ApiRoutes.Resume, null, cancellationToken);
            response.EnsureSuccessStatusCode();

            while (true)
            {
                var statusResponse = await _httpClient.GetAsync(baseUrl + ApiRoutes.Status, cancellationToken);
                statusResponse.EnsureSuccessStatusCode();
                var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, _jsonOptions)!;
                if (!status.isPaused)
                {
                    return new CallToolResult { Content = [new TextContentBlock { Text = "Resumed successfully." }] };
                }
            }
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }

    [McpServerTool(ReadOnly = false),
     Description("Request a domain reload (script recompilation) in the Unity Editor."), UsedImplicitly]
    public async Task<CallToolResult> ReloadDomain(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = urlProvider.GetUrl();
            var response = await _httpClient.PostAsync(baseUrl + ApiRoutes.DomainReload, null, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Poll /ping to wait for the server to come back after domain reload.
            // DomainReloadRetryHandler handles retries during the reload.
            var pingResponse = await _httpClient.GetAsync(baseUrl + ApiRoutes.Ping, cancellationToken);
            pingResponse.EnsureSuccessStatusCode();

            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = "Domain reload completed successfully." }]
            };
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }
}

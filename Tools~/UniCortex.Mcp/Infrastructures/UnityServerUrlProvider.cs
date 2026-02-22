using System.Text.Json;
using UniCortex.Editor.Domains.Models;
using UniCortex.Mcp.Domains.Interfaces;

namespace UniCortex.Mcp.Infrastructures;

internal sealed class UnityServerUrlProvider : IUnityServerUrlProvider
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { IncludeFields = true };

    public string GetUrl()
    {
        // 優先度1: UNICORTEX_URL 環境変数
        var url = Environment.GetEnvironmentVariable("UNICORTEX_URL");
        if (url is not null)
        {
            return url;
        }

        // 優先度2: UNICORTEX_PROJECT_PATH 環境変数
        var projectPath = Environment.GetEnvironmentVariable("UNICORTEX_PROJECT_PATH");
        if (projectPath is null)
        {
            throw new InvalidOperationException(
                "Neither UNICORTEX_URL nor UNICORTEX_PROJECT_PATH environment variable is set. " +
                "Set UNICORTEX_PROJECT_PATH to your Unity project path, " +
                "or set UNICORTEX_URL to the server URL directly.");
        }

        var path = Path.Combine(projectPath, "Library", "UniCortex", "config.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"UniCortex config file not found: {path}. " +
                "Make sure the Unity project is open and UniCortex is running, " +
                "or set UNICORTEX_URL to the server URL directly.");
        }

        var config = JsonSerializer.Deserialize<UnityServerConfig>(File.ReadAllText(path), s_jsonOptions);
        if (string.IsNullOrEmpty(config?.server_url))
        {
            throw new InvalidOperationException(
                $"server_url is missing or empty in {path}. " +
                "Make sure the Unity project is open and UniCortex is running, " +
                "or set UNICORTEX_URL to the server URL directly.");
        }

        return config.server_url;
    }
}

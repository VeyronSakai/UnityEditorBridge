using System.Text.Json;

namespace UnityEditorBridge.CLI;

public static class BridgeClient
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri(
            Environment.GetEnvironmentVariable("UEB_URL") ?? "http://localhost:56780")
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public static async Task GetAsync(string path)
    {
        try
        {
            var response = await HttpClient.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await Console.Error.WriteLineAsync(
                    $"Error: {(int)response.StatusCode} {response.StatusCode}\n{body}");
                Environment.Exit(1);
                return;
            }

            var json = JsonSerializer.Deserialize<JsonElement>(body);
            Console.WriteLine(JsonSerializer.Serialize(json, JsonOptions));
        }
        catch (HttpRequestException ex)
        {
            await Console.Error.WriteLineAsync($"Error: Could not connect to server. {ex.Message}");
            Environment.Exit(1);
        }
    }
}

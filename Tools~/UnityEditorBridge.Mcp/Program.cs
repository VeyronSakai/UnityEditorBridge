using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddHttpClient("EditorBridge", client =>
{
    var baseUrl = Environment.GetEnvironmentVariable("UEB_URL") ?? "http://localhost:56780";
    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
    {
        Console.Error.WriteLine(
            $"Invalid UEB_URL environment variable value: '{baseUrl}'. Please set it to a valid absolute URL (for example, 'http://localhost:56780').");
        Environment.Exit(1);
        return;
    }

    client.BaseAddress = baseUri;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

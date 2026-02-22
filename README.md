# UniCortex

> **Warning**
> This project is in a very early stage of development. Only a small subset of features has been implemented, and the API and command structure are subject to significant changes without notice.

A toolkit for controlling Unity Editor externally via REST API and MCP (Model Context Protocol).

Primarily designed for AI agents (Claude Code, Codex CLI, etc.) to operate Unity Editor through MCP.

## Features

- Pure C# — no external runtimes like Python or Node.js required
- Embeds an HTTP server inside Unity Editor, controlled via REST API
- .NET 8 MCP server (run via `dotnet run`) for AI agent integration via stdio
- Distributed as a UPM package

## Requirements

- Unity 2022.3 or later
- .NET 8 SDK (for MCP server)

## Installation

Add via Unity Package Manager using a Git URL:

1. Open Package Manager
2. Click the `+` button
3. Select "Add package from git URL"
4. Enter the following URL:

```
https://github.com/VeyronSakai/UniCortex.git
```

### MCP Server Setup

Add the following to `.mcp.json` in the Unity project root:

```json
{
  "mcpServers": {
    "Unity": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "Library/PackageCache/com.veyron-sakai.uni-cortex@0.1.0/Tools~/UniCortex.Mcp/"]
    }
  }
}
```

No pre-build or tool installation is required. The MCP server is built and started automatically via `dotnet run`.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/editor/ping` | Health check |
| POST | `/editor/play` | Start Play mode |
| POST | `/editor/stop` | Stop Play mode |
| POST | `/gameobject/create` | Create a GameObject |

You can also call the API directly with curl:

```bash
curl http://localhost:56780/editor/ping
curl -X POST http://localhost:56780/editor/play
```

## Settings

Configurable from Project Settings > UniCortex.

| Setting | Default | Description |
|---------|---------|-------------|
| Port | 56780 | Listen port |
| AutoStart | true | Start automatically on Editor launch |

## Contributing

When developing this package locally:

```bash
dotnet run --project Tools~/UniCortex.Mcp/
```

## License

MIT License - [LICENSE.txt](LICENSE.txt)

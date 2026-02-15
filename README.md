# UnityEditorBridge

> **Warning**
> This project is in a very early stage of development. Only a small subset of features has been implemented, and the API and command structure are subject to significant changes without notice.

A toolkit for controlling Unity Editor externally via REST API and CLI.

Primarily designed for AI agents (Claude Code, Codex CLI, etc.) to operate Unity Editor through shell commands.

## Features

- Pure C# — no external runtimes like Python or Node.js required
- Embeds an HTTP server inside Unity Editor, controlled via REST API
- .NET 8 CLI tool (`dotnet ueb`) for command-line operation
- Distributed as a UPM package

## Requirements

- Unity 2021.3 or later
- .NET 8 SDK (for CLI usage)

## Installation

Add via Unity Package Manager using a Git URL:

1. Open Package Manager
2. Click the `+` button
3. Select "Add package from git URL"
4. Enter the following URL:

```
https://github.com/VeyronSakai/UnityEditorBridge.git
```

The CLI is automatically installed as a dotnet local tool when Unity Editor starts.

## Usage

```bash
# Health check (logs "pong" in Unity Console)
dotnet ueb editor ping

# Start Play mode
dotnet ueb editor play

# Stop Play mode
dotnet ueb editor stop

# Create a GameObject
dotnet ueb gameobject create --name "Player" --primitive Cube
```

You can also call the API directly with curl:

```bash
curl http://localhost:56780/ping
curl -X POST http://localhost:56780/editor/play
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/ping` | Health check |
| POST | `/editor/play` | Start Play mode |
| POST | `/editor/stop` | Stop Play mode |
| POST | `/gameobject/create` | Create a GameObject |

## Settings

Configurable from Project Settings > Unity Editor Bridge.

| Setting | Default | Description |
|---------|---------|-------------|
| Port | 56780 | Listen port |
| AutoStart | true | Start automatically on Editor launch |

## License

MIT License - [LICENSE.txt](LICENSE.txt)

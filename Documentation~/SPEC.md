# UnityEditorBridge 仕様書

## 概要

UnityEditorBridge は、Unity Editor を外部から操作するためのツールキットです。
Unity Editor 内に HTTP サーバーを埋め込み、MCP サーバーを介して AI エージェントが直接 Editor を制御します。

AI エージェント（Claude Code, Codex CLI 等）が MCP プロトコルを通じて Unity Editor を操作することを主な目的としています。

## 設計原則

- **C# のみで完結**: Python, Node.js 等の外部ランタイムに依存しない
- **MCP プロトコル対応**: AI エージェントが MCP を通じて直接操作可能
- **REST API も維持**: curl 等での直接アクセスも引き続き可能
- **UPM パッケージとして配布**

## 名前

- GitHub リポジトリ: `UnityEditorBridge`
- UPM パッケージ名: `com.veyron-sakai.editor-bridge`
- MCP サーバー起動: `dotnet run --project <path>/Tools~/UnityEditorBridge.Mcp/`

---

## ディレクトリ構成

```
UnityEditorBridge/
├── package.json
├── README.md
├── LICENSE
├── Editor/
│   ├── UnityEditorBridge.Editor.asmdef
│   ├── AssemblyInfo.cs
│   ├── EntryPoint.cs
│   ├── Domains/
│   │   ├── Interfaces/
│   │   │   ├── IHttpServer.cs
│   │   │   ├── IMainThreadDispatcher.cs
│   │   │   ├── IRequestContext.cs
│   │   │   └── IRequestRouter.cs
│   │   └── Models/
│   │       ├── ApiRoutes.cs
│   │       ├── ErrorResponse.cs         ← エラーレスポンス DTO
│   │       ├── HttpMethodType.cs
│   │       └── PingResponse.cs          ← GET /ping レスポンス DTO
│   ├── Infrastructures/
│   │   ├── HttpListenerRequestContext.cs
│   │   ├── HttpListenerServer.cs        ← HttpListener HTTP サーバー
│   │   ├── MainThreadDispatcher.cs      ← メインスレッドディスパッチ
│   │   └── RequestRouter.cs             ← パスルーティング
│   ├── Presentations/
│   │   └── PingHandler.cs
│   ├── UseCases/
│   │   └── PingUseCase.cs
│   └── Settings/
│       └── EditorBridgeSettings.cs
├── Tools~/
│   └── UnityEditorBridge.Mcp/
│       ├── UnityEditorBridge.Mcp.csproj
│       ├── Program.cs
│       └── Tools/
│           └── PingTool.cs
└── docs/
    └── SPEC.md                         ← この文書
```

- `Editor/` — Unity Editor 拡張。asmdef で `includePlatforms: ["Editor"]`
- `Tools~/` — `~` サフィックスにより Unity のインポート対象外。.NET 8 MCP サーバープロジェクト

---

## コンポーネント 1: Unity Editor HTTP サーバー

### 技術要素

- `System.Net.HttpListener` で `http://localhost:56780/` をリッスン
- `[InitializeOnLoad]` で Editor 起動時に自動開始
- `EditorApplication.update` + `ConcurrentQueue<Action>` でメインスレッドディスパッチ
- `AssemblyReloadEvents.beforeAssemblyReload` で graceful shutdown、リロード後に再起動

### 設定（ScriptableSingleton）

| 項目 | デフォルト | 説明 |
|------|----------|------|
| Port | 56780 | リッスンポート |
| AutoStart | true | 自動開始 |

Project Settings UI（`Project/Unity Editor Bridge`）から変更可能。

### メインスレッドディスパッチ

Unity API はメインスレッドからのみ呼び出し可能。HttpListener のコールバックはスレッドプールで実行されるため、以下のパターンでブリッジする:

1. HTTP スレッドで `MainThreadDispatcher.RunOnMainThread<T>(Func<T> func)` を呼ぶ
2. `TaskCompletionSource<T>` を作成し `ConcurrentQueue` にエンキュー
3. メインスレッド（`EditorApplication.update`）でデキュー → `func()` 実行 → `tcs.SetResult()`
4. HTTP スレッドで await 完了 → レスポンスを返す

---

### JSON シリアライズ

リクエスト/レスポンスの JSON シリアライズには DTO クラスを使用する。

- `Editor/Domains/Models/` に配置。namespace: `EditorBridge.Editor.Domains.Models`
- `[Serializable]` 属性 + public fields（camelCase）
- Unity 依存（`using UnityEngine` 等）を含めないこと（MCP サーバーと共有するため）
- Unity 側: `JsonUtility.ToJson()` / `JsonUtility.FromJson<T>()`
- MCP サーバー側: `System.Text.Json` + `JsonSerializerOptions { IncludeFields = true }`
- MCP サーバーの .csproj で `<Compile Include="../../Editor/Domains/Models/**/*.cs" LinkBase="Models" />` としてソース共有

---

## API エンドポイント（v0.1.0 スコープ）

レスポンスは常に `application/json; charset=utf-8`。
エラー時: HTTP ステータスコード + `{"error": "メッセージ"}`

### GET `/ping`

サーバー疎通確認。**Unity Console に `pong` とログ出力**し、レスポンスを返す。

レスポンス:
```json
{"status": "ok", "message": "pong"}
```

### POST `/editor/play`

Play モードを開始する。`EditorApplication.isPlaying = true`

レスポンス:
```json
{"success": true}
```

### POST `/editor/stop`

Play モードを停止する。`EditorApplication.isPlaying = false`

レスポンス:
```json
{"success": true}
```

### POST `/gameobject/create`

GameObject を作成する。

リクエストボディ:
```json
{
  "name": "MyCube",
  "primitive": "Cube"
}
```

- `name`: 作成する GameObject の名前（必須）
- `primitive`: `PrimitiveType` の名前。省略時は空の GameObject を作成
  - 有効値: `Cube`, `Sphere`, `Capsule`, `Cylinder`, `Plane`, `Quad`

作成時は `Undo.RegisterCreatedObjectUndo` を呼び、Undo 対応する。

レスポンス:
```json
{"name": "MyCube", "instanceId": 12345}
```

---

## コンポーネント 2: MCP サーバー（dotnet run --project）

### 構成

`AI Agent ←(MCP/stdio)→ MCP Server ←(HTTP)→ Unity Editor HTTP Server`

### 技術スタック

- .NET 8（`net8.0`）
- ModelContextProtocol SDK（0.9.0-preview.1）
- Microsoft.Extensions.Hosting（8.0.0）
- トランスポート: stdio
- `dotnet run --project` で直接起動（事前ビルド不要）

### プロジェクトファイル（UnityEditorBridge.Mcp.csproj）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>UnityEditorBridge.Mcp</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.9.0-preview.1" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="../../Editor/Domains/Models/**/*.cs" LinkBase="Models" />
  </ItemGroup>
</Project>
```

### エントリポイント（Program.cs）

- `Host.CreateApplicationBuilder` で MCP サーバーを構築
- `.WithStdioServerTransport()` で stdio トランスポート
- `.WithToolsFromAssembly()` でツール自動検出
- `HttpClient` を DI に登録（ベースアドレスは環境変数 `UEB_URL` / デフォルト `http://localhost:56780`）
- ログは stderr に出力（stdout は MCP プロトコル用）

### MCP ツール

| ツール | API | 説明 |
|--------|-----|------|
| `Ping` | GET `/ping` | 疎通確認 |

各ツールは `[McpServerToolType]` クラス内に `[McpServerTool]` メソッドとして定義。
`IHttpClientFactory` をコンストラクタ DI で受け取り、Unity Editor HTTP サーバーにリクエストを送信する。

---

## MCP サーバーセットアップ

Unity プロジェクトのルートに `.mcp.json` を配置するだけで利用可能。事前のビルドやツールインストールは不要。

```json
{
  "mcpServers": {
    "unity-editor-bridge": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "Library/PackageCache/com.veyron-sakai.editor-bridge@0.1.0/Tools~/UnityEditorBridge.Mcp/"]
    }
  }
}
```

`dotnet run` が初回実行時に自動でビルドし、MCP サーバーを起動する。

---

## UPM パッケージ（package.json）

```json
{
  "name": "com.veyron-sakai.editor-bridge",
  "displayName": "Editor Bridge",
  "version": "0.1.0",
  "description": "Control Unity Editor via REST API and MCP.",
  "author": {
    "name": "veyron-sakai",
    "url": "https://github.com/veyron-sakai"
  }
}
```

### Assembly Definition（UnityEditorBridge.Editor.asmdef）

```json
{
  "name": "UnityEditorBridge.Editor",
  "rootNamespace": "UnityEditorBridge.Editor",
  "includePlatforms": ["Editor"]
}
```

---

## 使用例

```bash
# curl で直接 API を呼ぶことも可能
curl http://localhost:56780/ping
curl -X POST http://localhost:56780/editor/play
```

MCP 経由の操作は AI エージェント（Claude Code 等）の MCP 設定に追加することで利用可能。

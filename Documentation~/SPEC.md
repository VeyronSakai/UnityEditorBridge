# UniCortex 仕様書

## 概要

UniCortex は、Unity Editor を外部から操作するためのツールキットです。
Unity Editor 内に HTTP サーバーを埋め込み、MCP サーバーを介して AI エージェントが直接 Editor を制御します。

AI エージェント（Claude Code, Codex CLI 等）が MCP プロトコルを通じて Unity Editor を操作することを主な目的としています。

## 設計原則

- **C# のみで完結**: Python, Node.js 等の外部ランタイムに依存しない
- **MCP プロトコル対応**: AI エージェントが MCP を通じて直接操作可能
- **REST API も維持**: curl 等での直接アクセスも引き続き可能
- **UPM パッケージとして配布**

## 名前

- GitHub リポジトリ: `UniCortex`
- UPM パッケージ名: `com.veyron-sakai.uni-cortex`
- MCP サーバー起動: `dotnet run --project <path>/Tools~/UniCortex.Mcp/`

---

## ディレクトリ構成

```
UniCortex/
├── package.json
├── README.md
├── LICENSE
├── Editor/
│   ├── UniCortex.Editor.asmdef
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
│       ├── UniCortexSettings.cs
│       ├── UniCortexSettingsProvider.cs  ← Project Settings UI
│       └── ServerUrlFile.cs              ← Library/UniCortex/config.json 操作
├── Tools~/
│   └── UniCortex.Mcp/
│       ├── UniCortex.Mcp.csproj
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

- `System.Net.HttpListener` で `http://localhost:<port>/` をリッスン
- ポートは Editor 起動時にランダムな空きポートを自動割り当て（`TcpListener` port 0 で取得）
- `SessionState` でポート番号をドメインリロード間で維持（Editor 再起動時のみ変わる）
- `[InitializeOnLoad]` で Editor 起動時に自動開始
- `EditorApplication.update` + `ConcurrentQueue<Action>` でメインスレッドディスパッチ
- `AssemblyReloadEvents.beforeAssemblyReload` で graceful shutdown、リロード後に再起動
- サーバー起動成功時に `Library/UniCortex/config.json` へ URL を書き出す
- `EditorApplication.quitting` で `Library/UniCortex/config.json` を削除

### URL ファイル

`Library/UniCortex/config.json` にサーバーの URL（例: `http://localhost:54321`）を書き出す。

- プロジェクト固有（`Library/` 以下）なので複数 Unity インスタンスでも独立
- `Library/` は通常 `.gitignore` 対象なのでリポジトリには含まれない
- MCP サーバーが `UNICORTEX_PROJECT_PATH` 環境変数経由でこのファイルを読む

### 設定（ScriptableSingleton）

| 項目 | デフォルト | 説明 |
|------|----------|------|
| AutoStart | true | 自動開始 |

Project Settings UI（`Project/UniCortex`）から変更可能。現在のポート番号は同画面に読み取り専用で表示。

### メインスレッドディスパッチ

Unity API はメインスレッドからのみ呼び出し可能。HttpListener のコールバックはスレッドプールで実行されるため、以下のパターンでブリッジする:

1. HTTP スレッドで `MainThreadDispatcher.RunOnMainThread<T>(Func<T> func)` を呼ぶ
2. `TaskCompletionSource<T>` を作成し `ConcurrentQueue` にエンキュー
3. メインスレッド（`EditorApplication.update`）でデキュー → `func()` 実行 → `tcs.SetResult()`
4. HTTP スレッドで await 完了 → レスポンスを返す

---

### JSON シリアライズ

リクエスト/レスポンスの JSON シリアライズには DTO クラスを使用する。

- `Editor/Domains/Models/` に配置。namespace: `UniCortex.Editor.Domains.Models`
- `[Serializable]` 属性 + public fields（camelCase）
- Unity 依存（`using UnityEngine` 等）を含めないこと（MCP サーバーと共有するため）
- Unity 側: `JsonUtility.ToJson()` / `JsonUtility.FromJson<T>()`
- MCP サーバー側: `System.Text.Json` + `JsonSerializerOptions { IncludeFields = true }`
- MCP サーバーの .csproj で `<Compile Include="../../Editor/Domains/Models/**/*.cs" LinkBase="Models" />` としてソース共有

---

## API エンドポイント（v0.1.0 スコープ）

レスポンスは常に `application/json; charset=utf-8`。
エラー時: HTTP ステータスコード + `{"error": "メッセージ"}`

### GET `/editor/ping`

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

### POST `/editor/pause`

Play モードを一時停止する。`EditorApplication.isPaused = true`

レスポンス:
```json
{"success": true}
```

### POST `/editor/unpause`

Play モードの一時停止を解除する。`EditorApplication.isPaused = false`

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

### プロジェクトファイル（UniCortex.Mcp.csproj）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>UniCortex.Mcp</RootNamespace>
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
- `HttpClient` を DI に登録（ベースアドレスは以下の優先順で決定）
  1. 環境変数 `UNICORTEX_URL`（直接 URL 指定）
  2. 環境変数 `UNICORTEX_PROJECT_PATH` 配下の `Library/UniCortex/config.json`
  3. どちらもなければエラーで終了
- ログは stderr に出力（stdout は MCP プロトコル用）

### MCP ツール

| ツール | API | 説明 |
|--------|-----|------|
| `ping_editor` | GET `/editor/ping` | 疎通確認 |
| `enter_play_mode` | POST `/editor/play` | Play モード開始 |
| `exit_play_mode` | POST `/editor/stop` | Play モード停止 |
| `pause_editor` | POST `/editor/pause` | エディターを一時停止 |
| `resume_editor` | POST `/editor/unpause` | エディターの一時停止を解除 |
| `reload_domain` | POST `/editor/domain-reload` | ドメインリロード（スクリプト再コンパイル） |

各ツールは `[McpServerToolType]` クラス内に `[McpServerTool]` メソッドとして定義。
`IHttpClientFactory` をコンストラクタ DI で受け取り、Unity Editor HTTP サーバーにリクエストを送信する。

---

## MCP サーバーセットアップ

Unity プロジェクトのルートに `.mcp.json` を配置するだけで利用可能。事前のビルドやツールインストールは不要。

```json
{
  "mcpServers": {
    "Unity": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/your/unity/project/Library/PackageCache/com.veyron-sakai.uni-cortex@0.1.0/Tools~/UniCortex.Mcp/"],
      "env": {
        "UNICORTEX_PROJECT_PATH": "/path/to/your/unity/project"
      }
    }
  }
}
```

`/path/to/your/unity/project` を Unity プロジェクトの絶対パスに置き換える。`args` の `--project` と `UNICORTEX_PROJECT_PATH` の両方に同じパスを設定する必要がある（`args` 内での環境変数展開は MCP クライアントが対応していないため）。

`dotnet run` が初回実行時に自動でビルドし、MCP サーバーを起動する。

### URL 指定方法のまとめ

| 方法 | 設定 | 優先度 |
|------|------|--------|
| 直接 URL 指定 | `UNICORTEX_URL=http://localhost:XXXXX` | 高 |
| プロジェクトパス指定 | `UNICORTEX_PROJECT_PATH=/path/to/project` | 低 |

どちらも未設定の場合、MCP サーバーはエラーで終了する。

---

## UPM パッケージ（package.json）

```json
{
  "name": "com.veyron-sakai.uni-cortex",
  "displayName": "UniCortex",
  "version": "0.1.0",
  "description": "Control Unity Editor via REST API and MCP.",
  "author": {
    "name": "veyron-sakai",
    "url": "https://github.com/veyron-sakai"
  }
}
```

### Assembly Definition（UniCortex.Editor.asmdef）

```json
{
  "name": "UniCortex.Editor",
  "rootNamespace": "UniCortex.Editor",
  "includePlatforms": ["Editor"]
}
```

---

## 使用例

```bash
# ポート番号は Library/UniCortex/config.json または Project Settings > UniCortex で確認
PORT=$(python3 -c "import json; print(json.load(open('Library/UniCortex/config.json'))['server_url'].split(':')[-1])")

# curl で直接 API を呼ぶことも可能
curl http://localhost:${PORT}/editor/ping
curl -X POST http://localhost:${PORT}/editor/play
```

MCP 経由の操作は AI エージェント（Claude Code 等）の MCP 設定に追加することで利用可能。

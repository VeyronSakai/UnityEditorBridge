# UnityEditorBridge 仕様書

## 概要

UnityEditorBridge は、Unity Editor を外部から操作するためのツールキットです。
Unity Editor 内に HTTP サーバーを埋め込み、C# 製の CLI ツールからリクエストを送ることで Editor を制御します。

AI エージェント（Claude Code, Codex CLI 等）がシェルコマンド経由で Unity Editor を操作することを主な目的としています。

## 設計原則

- **C# のみで完結**: Python, Node.js 等の外部ランタイムに依存しない
- **素の REST API**: MCP/ACP に縛られない JSON over HTTP
- **UPM パッケージとして配布**

## 名前

- GitHub リポジトリ: `UnityEditorBridge`
- UPM パッケージ名: `com.veyron-sakai.editor-bridge`
- CLI コマンド: `dotnet ueb`

---

## ディレクトリ構成

```
UnityEditorBridge/
├── package.json
├── README.md
├── LICENSE
├── Editor/
│   ├── UnityEditorBridge.Editor.asmdef
│   ├── Server/
│   │   ├── EditorBridgeServer.cs       ← HttpListener HTTP サーバー
│   │   ├── RequestRouter.cs            ← パスルーティング
│   │   └── MainThreadDispatcher.cs     ← メインスレッドディスパッチ
│   ├── Models/
│   │   ├── PingResponse.cs             ← GET /ping レスポンス DTO
│   │   └── ErrorResponse.cs            ← エラーレスポンス DTO
│   ├── Handlers/
│   │   ├── PingHandler.cs
│   │   ├── EditorHandlers.cs
│   │   └── GameObjectHandlers.cs
│   ├── Settings/
│   │   ├── EditorBridgeSettings.cs
│   │   └── EditorBridgeSettingsProvider.cs
│   └── Installer/
│       └── CliInstaller.cs
├── Tools~/
│   └── UnityEditorBridge.CLI/
│       ├── UnityEditorBridge.CLI.csproj
│       ├── Program.cs
│       ├── BridgeClient.cs
│       └── Commands/
│           ├── EditorCommands.cs
│           └── GameObjectCommands.cs
└── docs/
    └── SPEC.md                         ← この文書
```

- `Editor/` — Unity Editor 拡張。asmdef で `includePlatforms: ["Editor"]`
- `Tools~/` — `~` サフィックスにより Unity のインポート対象外。.NET 8 CLI プロジェクト

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

- `Editor/Models/` に配置。namespace: `EditorBridge.Editor.Models`
- `[Serializable]` 属性 + public fields（camelCase）
- Unity 依存（`using UnityEngine` 等）を含めないこと（CLI と共有するため）
- Unity 側: `JsonUtility.ToJson()` / `JsonUtility.FromJson<T>()`
- CLI 側: `System.Text.Json` + `JsonSerializerOptions { IncludeFields = true }`
- CLI の .csproj で `<Compile Include="../../Editor/Models/**/*.cs" LinkBase="Models" />` としてソース共有

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

## コンポーネント 2: CLI ツール（dotnet ueb）

### 技術スタック

- .NET 8（`net8.0`）
- ConsoleAppFramework v5（5.7.13）
- dotnet local tool として配布

### プロジェクトファイル（UnityEditorBridge.CLI.csproj）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>UnityEditorBridge.CLI</RootNamespace>
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>ueb</ToolCommandName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ConsoleAppFramework" Version="5.7.13" />
  </ItemGroup>
</Project>
```

### エントリポイント（Program.cs）

```csharp
using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add<EditorCommands>("editor");
app.Add<GameObjectCommands>("gameobject");
app.Run(args);
```

### 共通 HTTP クライアント（BridgeClient.cs）

- ベースアドレス: 環境変数 `UEB_URL`（デフォルト `http://localhost:56780`）
- `GetAsync(path)` / `PostAsync(path, payload)` メソッドを提供
- レスポンス JSON を整形して stdout に出力
- エラー時は stderr に出力、exit code 1

### CLI コマンド

#### editor

| コマンド | API | 説明 |
|---------|-----|------|
| `dotnet ueb editor ping` | GET `/ping` | 疎通確認 |
| `dotnet ueb editor play` | POST `/editor/play` | Play 開始 |
| `dotnet ueb editor stop` | POST `/editor/stop` | Play 停止 |

#### gameobject

| コマンド | API | 説明 |
|---------|-----|------|
| `dotnet ueb gameobject create --name <名前> [--primitive Cube]` | POST `/gameobject/create` | オブジェクト作成 |

各メソッドに `/// <summary>` と `/// <param name="">` を記述し、`--help` で説明が出るようにする。

---

## CLI インストーラー（CliInstaller.cs）

Unity Editor 起動時に CLI を dotnet local tool として自動インストールする。

### 動作フロー（`[InitializeOnLoad]` で自動実行）

1. プロジェクトルート（`Application.dataPath` の親）を取得
2. nupkg が未ビルドまたはバージョン不一致の場合:
   ```
   dotnet pack "Packages/com.veyron-sakai.editor-bridge/Tools~/UnityEditorBridge.CLI/"
     -c Release -o "Library/EditorBridge/nupkg"
   ```
3. `.config/dotnet-tools.json` がなければ `dotnet new tool-manifest` を実行
4. 未インストールなら:
   ```
   dotnet tool install UnityEditorBridge.CLI --local
     --add-source "Library/EditorBridge/nupkg"
   ```
5. バージョン不一致なら `dotnet tool update` で更新
6. Console に `[EditorBridge] CLI installed: dotnet ueb` とログ出力

### エラーハンドリング

- `dotnet` が見つからない → 警告ログを出してスキップ（サーバー起動はブロックしない）
- `dotnet pack` / `dotnet tool install` 失敗 → エラーログを出してスキップ

---

## UPM パッケージ（package.json）

```json
{
  "name": "com.veyron-sakai.editor-bridge",
  "version": "0.1.0",
  "displayName": "Unity Editor Bridge",
  "description": "Control Unity Editor via REST API and CLI.",
  "unity": "2021.3",
  "author": {
    "name": "veyron-sakai",
    "url": "https://github.com/veyron-sakai"
  },
  "license": "MIT"
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
# 疎通確認（Unity Console に pong と出る）
dotnet ueb editor ping

# Play 開始
dotnet ueb editor play

# Play 停止
dotnet ueb editor stop

# Cube を作成
dotnet ueb gameobject create --name "Player" --primitive Cube

# curl でも可
curl http://localhost:56780/ping
curl -X POST http://localhost:56780/editor/play
```

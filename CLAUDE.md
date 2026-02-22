# UniCortex

Unity Editor を外部から REST API + MCP で操作するツールキット。

## 仕様書

実装の詳細は `Documentation~/SPEC.md` を参照してください。
実装前に必ず読んでから作業を開始すること。

## 技術スタック

- Unity Editor 側: C# HttpListener HTTP サーバー
- MCP Server: .NET 8 + Model Context Protocol C# SDK
- UPM パッケージ: com.veyron-sakai.uni-cortex

## 重要な規約

- Unity API 呼び出しは必ず MainThreadDispatcher 経由
- シーン変更操作はすべて Undo 対応
- JSON シリアライズは DTO クラス + JsonUtility（Unity 側）/ System.Text.Json（MCP サーバー側）
- DTO は Editor/Domains/Models/ に配置、namespace は UniCortex.Editor.Domains.Models
- DTO に Unity 依存（using UnityEngine 等）を入れないこと（MCP サーバーと共有するため）
- MCP サーバーの設定例（`.mcp.json`）:

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

# UnityEditorBridge

Unity Editor を外部から REST API + CLI で操作するツールキット。

## 仕様書

実装の詳細は `Documentation~/SPEC.md` を参照してください。
実装前に必ず読んでから作業を開始すること。

## 技術スタック

- Unity Editor 側: C# HttpListener HTTP サーバー
- CLI: .NET 8 + ConsoleAppFramework v5、dotnet local tool として配布
- UPM パッケージ: com.veyron-sakai.editor-bridge

## 重要な規約

- Unity API 呼び出しは必ず MainThreadDispatcher 経由
- シーン変更操作はすべて Undo 対応
- JSON シリアライズは DTO クラス + JsonUtility（Unity 側）/ System.Text.Json（CLI 側）
- DTO は Editor/Models/ に配置、namespace は EditorBridge.Models
- DTO に Unity 依存（using UnityEngine 等）を入れないこと（CLI と共有するため）
- CLI の実行は `dotnet ueb`

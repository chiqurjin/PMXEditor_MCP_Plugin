# アーキテクチャ / Architecture

## 全体像

```
Claude Code ──┐
              │  HTTP POST (JSON-RPC 2.0 / MCP Streamable HTTP)
              ▼
      HttpTransport            ← HttpListener, ワーカースレッド
              │
      McpDispatcher            ← initialize / ping / tools/list / tools/call
              │
      ToolRegistry ─► Tools/*  ← 18 個のツール実装
              │
        UiDispatcher           ← Control.Invoke で UI スレッドへ
              │
         Editor                ← GetCurrentState / Update / UpdateList / UpdateModel
              │
      PEPlugin コネクタ  ─────►  PMX エディタ本体
```

すべて **PMX エディタのプロセス内** で動きます。外部プロセスも、別言語のランタイムもありません。

## なぜプラグインだけで完結するのか

| 必要なもの | 実現方法 |
| --- | --- |
| 常駐 | `PEPluginOption(Bootup: true, ...)` で PMX エディタ起動と同時に `Run()` が呼ばれる |
| 通信 | プラグインは通常の .NET クラスライブラリなので `System.Net.HttpListener` がそのまま使える |
| ポート確保 | `http://127.0.0.1:<port>/` のループバック待ち受けは管理者権限も URL 予約も不要 |
| MCP 対応 | MCP は stdio 以外に Streamable HTTP を規定しており、クライアントは HTTP で直接繋げる |
| JSON | `System.Web.Script.Serialization.JavaScriptSerializer` (フレームワーク同梱) を使い、依存 DLL ゼロ |

stdio トランスポートは使えません。PMX エディタは GUI アプリであり、MCP クライアントが起動する子プロセスでは
ないためです。HTTP を選ぶことで、追加のブリッジプロセス無しで成立しています。

## スレッドモデル

PEPlugin のコネクタは UI スレッドから呼ぶ前提です。一方 HTTP リクエストはワーカースレッドに届きます。

1. 常駐開始時 (`IsBootup == true`、UI スレッド上) に非表示の `Form` を 1 つ作り、`Handle` を触って
   ウィンドウハンドルを確定させます。
2. `UiDispatcher` はこの `Form` の `InvokeRequired` / `Invoke` を使って、ツール本体を UI スレッドで実行します。
3. したがってツール実装は「UI スレッドで動いている」前提で書けます。

```csharp
// UiDispatcher
public T Run<T>(Func<T> func)
{
    if (m_anchor == null || m_anchor.IsDisposed || !m_anchor.InvokeRequired) return func();
    return (T)m_anchor.Invoke(func);
}
```

## 編集の流れ

`Editor.Edit()` が PEPlugin の作法をまとめています。

```csharp
IPXPmx pmx = Connector.Pmx.GetCurrentState();   // 複製を取得
mutate(pmx);                                     // 複製を編集
Connector.Pmx.Update(pmx, target, index);        // 本体へ反映 (Undo に積まれる)
Connector.Form.UpdateList(listTarget);           // リスト表示を更新
Connector.View.PmxView.UpdateModel();            // ビューを更新
```

`Update()` を通すので、`undo` ツールでも、エディタの Ctrl+Z でも同じように元に戻せます。

## HTTP の実装 (MCP Streamable HTTP)

| メソッド | 動作 |
| --- | --- |
| `POST` | JSON-RPC メッセージを 1 件受け取り、結果を `application/json` で返す。通知 (id なし) には `202` |
| `DELETE` | セッションを終了して `200` |
| `OPTIONS` | `204` |
| `GET` / その他 | `405` (サーバー発の SSE ストリームは提供しないため。仕様どおりの応答) |

- `initialize` 応答で `Mcp-Session-Id` を発行し、以降のリクエストで検証します。
- プロトコル版はクライアント要求を尊重し、`2025-06-18` / `2025-03-26` / `2024-11-05` に対応します。
- **Origin チェック**: `Origin` ヘッダがあり、それが localhost 系でなければ `403`。
  ローカル MCP サーバーに対する DNS リバインディング対策として仕様が要求しているものです。
- `token` を設定した場合は `Authorization: Bearer <token>` を必須にします。
- ボディ上限 8 MB。JSON-RPC のバッチは `2025-06-18` で廃止されたため受け付けません。

## 例外の扱い

PMX エディタはプラグイン内の例外を捕捉できず、場合によっては本体ごと落ちます。そのため:

- `Run()` 全体、リクエスト処理、ツール実行のそれぞれで例外を捕捉します。
- ツールの失敗は JSON-RPC エラーではなく、MCP のツールエラー (`isError: true`) として返します。
  モデルが読んで自己修正できるためです。
- ログはデフォルト無効。`logFile` を設定したときだけファイルに追記します (書き込み失敗も握りつぶします)。

## ツールを追加する

1. `src/PmxMcpPlugin/Tools/` に `XxxTools.cs` を作り、`Register(ToolRegistry, Editor)` を実装します。

```csharp
registry.Add(
    "my_tool",
    "何をするツールかの説明 (モデルが読む文章)",
    Schema.Object(Json.Obj("index", Schema.Int("対象インデックス")), "index"),
    true,   // readOnly
    delegate(Dictionary<string, object> args)
    {
        return editor.Read<object>(delegate(IPXPmx pmx)
        {
            return Json.Obj("count", pmx.Vertex.Count);
        });
    });
```

2. `McpService` のコンストラクタで `XxxTools.Register(m_registry, editor);` を呼びます。
3. `build.ps1` は `src` 以下の `.cs` を自動で拾うので、ファイルを足すだけでビルドされます。

書き込み系なら `editor.Edit<object>(...)` を使い、`change.Target` と `change.ListTarget` に
更新対象を設定してください (更新範囲を絞ると動作が軽くなります)。

## ソースの構成

| パス | 役割 |
| --- | --- |
| `Plugin.cs` | `PEPluginClass` の実装。エントリポイントと常駐設定 |
| `McpService.cs` | 常駐オブジェクト一式 (匿名フォーム / レジストリ / トランスポート) の生成と寿命管理 |
| `Editor.cs` | PEPlugin コネクタのラッパ。読み書きの定型処理 |
| `UiDispatcher.cs` | UI スレッドへのマーシャリング |
| `PluginConfig.cs` | `PmxMcpPlugin.json` の読み書き |
| `StatusForm.cs` | プラグインメニューから開くステータス画面 |
| `Mcp/HttpTransport.cs` | HTTP 層 (ルーティング・Origin/トークン検証・セッション) |
| `Mcp/McpDispatcher.cs` | JSON-RPC と MCP ライフサイクル |
| `Mcp/ToolRegistry.cs` | ツール定義と `tools/list` 用の記述生成 |
| `Mcp/Json.cs`, `Mcp/Schema.cs` | JSON と JSON Schema の補助 |
| `Tools/*.cs` | 各ツールの実装 |

## 制約と設計上の判断

- **C# 5 相当の構文**で書いています。Visual Studio が無い環境でも .NET Framework 付属の `csc.exe` で
  ビルドできるようにするためです。
- **外部 NuGet パッケージを使いません**。プラグインの依存 DLL は `_plugin` フォルダの探索パス外にあり、
  解決が面倒になるためです。JSON はフレームワーク同梱の実装で足ります。
- `PEPlugin.dll` / `SlimDX.dll` は参照のみで**再配布しません**。ビルド時に利用者の PMX エディタから参照します。

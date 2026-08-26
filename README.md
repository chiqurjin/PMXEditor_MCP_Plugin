# PMXEditor_MCP_Plugin

**PMXエディタを MCP サーバーにするプラグイン。DLL を 1 つ入れるだけで、Claude から PMX モデルを読み書きできます。**

外部ブリッジプロセスも Python も要りません。PMX エディタ本体のプロセス内で MCP (Model Context Protocol) の
Streamable HTTP サーバーが動くので、PMX エディタを起動した時点で接続できる状態になります。

```
Claude Code  ──HTTP/JSON-RPC──►  PmxMcpPlugin.dll  ──PEPlugin API──►  PMX エディタ本体
                                 (PMXエディタのプロセス内)
```

- 対応: PMX エディタ 0273 (PEPlugin β 版 API) / .NET Framework 4.8 / MCP `2025-06-18`
- ツール 18 個 (モデル情報・ボーン・材質・モーフ・選択・カメラ・スクリーンショット・開く/保存・Undo/Redo)
- 待ち受けは `127.0.0.1` のみ。管理者権限も `netsh` の URL 予約も不要

> **PMX エディタは 極北P (kkhk22) 様の著作物です。**
> 本リポジトリはその PMX エディタ向けの**非公式**プラグインであり、PMX エディタ作者様とは関係ありません。
> PMX エディタ本体および同梱ライブラリ (`PmxEditor.exe` / `PEPlugin.dll` / `SlimDX.dll` など) は含まず、再配布もしていません。
> 本プラグイン自体は MIT ライセンスのオープンソースです。

---

## インストール

> **はじめての方へ:** 図解つきの [かんたん導入マニュアル (PDF・全9ページ)](docs/INSTALL-MANUAL-ja.pdf) を用意しました。
> 手順だけを知りたい場合はそちらが早いです。

1. [最新リリース](https://github.com/chiqurjin/PMXEditor_MCP_Plugin/releases/latest) から
   **`PMXEditor_MCP_Plugin_v0.1.0.zip`**（マニュアル同梱・おすすめ）または `PmxMcpPlugin.dll` 単体を
   ダウンロードし、`PmxMcpPlugin.dll` を PMX エディタの `_plugin\User\` フォルダへコピーします。
2. **PmxEditor_x64.exe** を起動します (プラグインは AnyCPU ですが、64bit 版を推奨)。
   起動と同時に MCP サーバーが立ち上がり、`PmxMcpPlugin.json` が同じフォルダに自動生成されます。
3. Claude Code に登録します。

```bash
claude mcp add --transport http pmx-editor http://127.0.0.1:38731/mcp
```

4. 動作確認は `[編集] - [プラグイン] - [MCP Server]` から。状態・URL・登録コマンドが表示され、コピーボタンで
   コマンドをクリップボードにコピーできます。

> **PMX エディタが起動していないと接続できません。** MCP クライアント側は、PMX エディタを立ち上げてから接続してください。

### stdio しか使えないクライアントの場合

Claude Desktop など HTTP トランスポートを直接指定できない環境では、`mcp-remote` を挟みます。

```json
{
  "mcpServers": {
    "pmx-editor": {
      "command": "npx",
      "args": ["-y", "mcp-remote", "http://127.0.0.1:38731/mcp"]
    }
  }
}
```

---

## ツール一覧

| ツール | 種別 | 内容 |
| --- | --- | --- |
| `get_model_info` | 読み取り | モデル名・コメント・各要素数・ファイルパス・Undo 段数 |
| `set_model_info` | 書き込み | モデル名 / 英名 / コメントの変更 |
| `list_bones` | 読み取り | ボーン一覧 (ページング・名前フィルタ) |
| `get_bone` | 読み取り | ボーン 1 本の詳細 (フラグ・付与親・IK リンク) |
| `set_bone` | 書き込み | 名前・位置・表示・操作・回転/移動フラグ |
| `list_materials` | 読み取り | 材質一覧 (面数・色・テクスチャ・エッジ) |
| `set_material` | 書き込み | 名前・拡散/反射/環境色・エッジ・両面・テクスチャ |
| `list_morphs` | 読み取り | モーフ一覧 (種類・パネル・オフセット数) |
| `set_morph_name` | 書き込み | モーフ名の変更 |
| `get_selection` | 読み取り | PmxView と各リストの選択状態 |
| `set_selection` | 書き込み | ボーン/頂点/面/材質の選択を設定 |
| `capture_viewport` | 読み取り | PmxView のスクリーンショット (PNG 画像として返却) |
| `get_camera` | 読み取り | カメラの視点位置・注視点・上方向 |
| `set_camera` | 書き込み | カメラの一括設定 |
| `open_model` | 書き込み | `.pmx` / `.pmd` を開く |
| `save_model` | 書き込み | 保存 (別名保存時は既存ファイルの上書きを明示的に許可する必要あり) |
| `undo` / `redo` | 書き込み | 元に戻す / やり直す |

引数の詳細は [docs/TOOLS.md](docs/TOOLS.md) を参照してください。

### 使用例

```
> 今開いてるモデルの材質を一覧して、"髪" を含む材質のエッジを切って
> ボーン "左腕" の位置を教えて。ついでにビューのスクショも見せて
> センター以下のボーン構造を英語名つきで整理して
```

---

## 設定 (`PmxMcpPlugin.json`)

DLL と同じフォルダに自動生成されます。編集後は PMX エディタを再起動するか、プラグインメニューの
`[再起動]` ボタンを押してください。

```json
{
  "host": "127.0.0.1",
  "port": 38731,
  "path": "/mcp",
  "token": "",
  "allowWrite": true,
  "allowFileAccess": true,
  "logFile": ""
}
```

| キー | 既定値 | 説明 |
| --- | --- | --- |
| `host` / `port` / `path` | `127.0.0.1` / `38731` / `/mcp` | エンドポイント。ポート衝突時はここを変更 |
| `token` | `""` | 設定すると `Authorization: Bearer <token>` を要求 |
| `allowWrite` | `true` | `false` にすると読み取り専用 (書き込み系ツールは全て拒否) |
| `allowFileAccess` | `true` | `false` にすると `open_model` / `save_model` を拒否 |
| `logFile` | `""` | パスを入れるとファイルログを出力 (既定は無効) |

---

## セキュリティ

- 待ち受けは **ループバックのみ**。既定の `127.0.0.1` では外部ネットワークから到達できません。
- MCP 仕様が求める **Origin チェック** を実装済み (localhost 以外の Origin は 403)。
- 同じ PC 上の他プロセスからは接続できます。気になる場合は `token` を設定してください。
- 読み取り専用で使いたい場合は `allowWrite: false`。
- `save_model` は、パスを明示した別名保存で既存ファイルがある場合、`overwrite: true` を渡さない限り上書きしません。

---

## ビルド

`PEPlugin.dll` と `SlimDX.dll` は **同梱していません** (PMX エディタ本体の配布物のため)。
お手元の PMX エディタから参照してビルドします。

```powershell
.\build.ps1 -PmxEditorPath "C:\path\to\PmxEditor_0273" -Install
```

Visual Studio が無くても、.NET Framework 付属の `csc.exe` でビルドできます (あれば Roslyn を自動使用)。
`-Install` を付けると `_plugin\User\` へのコピーまで行います。

Visual Studio / MSBuild を使う場合は `src\PmxMcpPlugin\PmxMcpPlugin.csproj` を開き、
環境変数 `PMXEDITOR_PATH` に PMX エディタのフォルダを設定してください。

---

## 仕組み

- `PEPluginOption(Bootup: true, ...)` により、PMX エディタ起動と同時にプラグインが常駐します。
- 常駐時に非表示の `Form` を 1 つ作り、そのハンドルを UI スレッドへの入口にします。
  HTTP はワーカースレッドで受けるため、PEPlugin のコネクタ呼び出しは全て `Control.Invoke` で
  UI スレッドへマーシャリングしています。
- 編集は「`GetCurrentState()` で複製取得 → 変更 → `Update()` → リストとビューを更新」という
  PEPlugin の作法どおりに行うので、**エディタ本体の Undo にそのまま積まれます**。
- トランスポートは MCP Streamable HTTP。`POST` で JSON-RPC を受け、通知には `202` を返します。
  サーバー発の SSE ストリームは提供しないため `GET` には仕様どおり `405` を返します。

詳細は [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)。

---

## 制限事項

- PMX エディタが起動している間だけ接続できます (常駐サービスではありません)。
- 頂点・面の直接編集、剛体 / Joint / 表示枠の編集は未対応です。
- サーバー発の通知 (SSE)、リソース、プロンプトは未実装です。
- 巨大モデルの一覧取得は `offset` / `limit` で分割してください (1 回あたり最大 1000 件)。
- PMX エディタは同時に 1 モデルしか扱えないため、複数モデルの並行編集はできません。

---

## クレジット

- **PMX エディタ**: 極北P (kkhk22) 様。本リポジトリは PMX エディタのプラグインであり、本体の配布物
  (`PEPlugin.dll`, `SlimDX.dll`, `PmxEditor.exe` など) は一切含みません。
- **MCP (Model Context Protocol)**: <https://modelcontextprotocol.io/>

本プラグインは非公式であり、PMX エディタ作者様とは無関係です。**本プラグインに関する問い合わせを
PMX エディタ作者様に行わないでください。** 不具合や要望はこのリポジトリの Issue へお願いします。
モデルデータの破損に備えて、作業前のバックアップを推奨します。

詳細は [NOTICE](NOTICE) を参照してください。

## ライセンス

MIT License — [LICENSE](LICENSE) を参照。

---

<a name="english"></a>

## English

**A plugin that turns PMX Editor (the MMD model editor) into an MCP server. Drop in one DLL and Claude can read
and edit the model that is open.**

No bridge process and no Python: the MCP Streamable HTTP server runs inside the PMX Editor process itself, so it
is reachable as soon as the editor starts.

- Targets PMX Editor 0273 (PEPlugin beta API), .NET Framework 4.8, MCP `2025-06-18`
- 18 tools: model info, bones, materials, morphs, selection, camera, viewport screenshot, open/save, undo/redo
- Listens on `127.0.0.1` only; no admin rights and no `netsh` URL reservation needed

### Install

1. Copy `dist/PmxMcpPlugin.dll` into the `_plugin\User\` folder of your PMX Editor install.
2. Start `PmxEditor_x64.exe`. The server starts with it and writes a default `PmxMcpPlugin.json` next to the DLL.
3. Register it:

```bash
claude mcp add --transport http pmx-editor http://127.0.0.1:38731/mcp
```

4. `[編集] - [プラグイン] - [MCP Server]` opens a status dialog with the endpoint and a copy button.

PMX Editor must be running for a client to connect. See [docs/TOOLS.md](docs/TOOLS.md) for the tool reference and
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for how it works, including the UI-thread marshalling and the
security model. Build from source with `build.ps1 -PmxEditorPath <folder> -Install`; PMX Editor DLLs are
referenced from your own install and are never redistributed here.

MIT licensed. Unofficial, not affiliated with the author of PMX Editor. Back up your models before editing.

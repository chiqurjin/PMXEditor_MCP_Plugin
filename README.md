# PMXEditor_MCP_Plugin

**PMXエディタを MCP サーバーにするプラグイン。DLL を 1 つ入れるだけで、Claude から PMX モデルを読み書きできます。**

外部ブリッジプロセスも Python も要りません。PMX エディタ本体のプロセス内で MCP (Model Context Protocol) の
Streamable HTTP サーバーが動くので、PMX エディタを起動した時点で接続できる状態になります。

```
Claude Code  ──HTTP/JSON-RPC──►  PmxMcpPlugin.dll  ──PEPlugin API──►  PMX エディタ本体
                                 (PMXエディタのプロセス内)
```

- 対応: PMX エディタ 0273 (PEPlugin β 版 API) / .NET Framework 4.8 / MCP `2025-06-18`
- ツール 52 個 (モデル情報・ヘッダ・ボーン・頂点/面・材質・モーフ・表示枠・剛体・ジョイント・柔体・選択・カメラ・スクリーンショット・開く/保存・Undo/Redo)
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
| `set_bone` | 書き込み | 名前・位置・親・接続先・変形階層・各フラグ・付与・軸固定・ローカル軸・外部親 |
| `set_bone_ik` | 書き込み | ＩＫのターゲット・回数・単位角・リンク鎖 (角度制限つき) |
| `add_bone` / `delete_bone` | 書き込み | ボーンの追加・削除 |
| `list_vertices` | 読み取り | 頂点一覧 (位置・法線・UV・ウェイト。既定 50 件ずつ) |
| `get_vertex` | 読み取り | 頂点 1 つの全項目 (追加 UV・SDEF・エッジ倍率まで) |
| `set_vertex` | 書き込み | 位置・法線・UV・追加 UV・ウェイト・エッジ倍率・SDEF |
| `list_faces` | 読み取り | 材質ごとの面 (三角形) を頂点番号で |
| `list_materials` | 読み取り | 材質一覧 (面数・色・テクスチャ・エッジ) |
| `get_material` | 読み取り | 材質 1 つの全項目 (影の各フラグ・スフィア・トゥーン・面の開始位置) |
| `set_material` | 書き込み | 色・光沢・エッジ・両面・影・スフィア・トゥーン・頂点色・描画種別 |
| `delete_material` | 書き込み | 材質と、その材質が持つ面の削除 |
| `list_morphs` | 読み取り | モーフ一覧 (種類・パネル・オフセット数) |
| `get_morph` | 読み取り | モーフ 1 つの中身 (種類ごとのオフセット) |
| `set_morph` | 書き込み | 名前・パネル・種類 |
| `set_morph_name` | 書き込み | モーフ名の変更 |
| `set_morph_offsets` | 書き込み | 中身の入れ替え (頂点/UV/ボーン/材質/グループ/インパルス) |
| `add_morph` / `delete_morph` | 書き込み | モーフの追加・削除 |
| `list_nodes` | 読み取り | 表示枠一覧 (Root・表情枠は `root` / `expression` として別に返る) |
| `get_node` / `set_node` | 読み取り / 書き込み | 枠の名前と中身 (ボーン・モーフの並び) |
| `add_node` / `delete_node` | 書き込み | 表示枠の追加・削除 |
| `list_bodies` / `get_body` | 読み取り | 剛体一覧・剛体 1 つの全項目 (質量・減衰・非衝突グループ) |
| `set_body` | 書き込み | ボーン・モード・形状・大きさ・配置・質量・摩擦・反発 |
| `add_body` / `delete_body` | 書き込み | 剛体の追加・削除 |
| `list_joints` / `get_joint` | 読み取り | ジョイント一覧・1 つの全項目 (移動/回転制限・ばね定数) |
| `set_joint` | 書き込み | 接続する剛体・種類・配置・各制限・ばね定数 |
| `add_joint` / `delete_joint` | 書き込み | ジョイントの追加・削除 |
| `list_soft_bodies` / `get_soft_body` | 読み取り | 柔体一覧・1 つの全項目 (PMX 2.1 の全係数) |
| `set_soft_body` | 書き込み | 形状・材質・グループ・質量・各係数 |
| `get_selection` | 読み取り | PmxView と各リストの選択状態 |
| `set_selection` | 書き込み | ボーン/頂点/面/材質の選択を設定 |
| `capture_viewport` | 読み取り | PmxView のスクリーンショット (PNG 画像として返却) |
| `get_camera` | 読み取り | カメラの視点位置・注視点・上方向 |
| `set_camera` | 書き込み | カメラの一括設定 |
| `get_header` / `set_header` | 読み取り / 書き込み | PMX の版・文字コード・追加 UV 数 |
| `open_model` | 書き込み | `.pmx` / `.pmd` を開く |
| `save_model` | 書き込み | 保存 (別名保存時は既存ファイルの上書きを明示的に許可する必要あり) |
| `undo` / `redo` | 書き込み | 元に戻す / やり直す |

引数の詳細は [docs/TOOLS.md](docs/TOOLS.md) を参照してください。

覚えておくと迷わない点:

- **軸固定とローカル軸は変形に効きません。** PMX 仕様どおり、表示と操作の制限項目です。
  ローカル軸は X と Z を渡すと Y = Z×X、Z' = X×Y を計算して 3 軸まとめて書きます。
- **表示枠の Root と表情は一覧に入りません。** PMX エディタがその 2 つを別に持っているためで、
  `list_nodes` の `root` / `expression`、あるいは `which: "root"` で指します。
- **ヘッダは保存時に中身から書き直されます。** 2.1 の機能を使っていなければ 2.0 で保存され、
  どの頂点も使っていない追加 UV は落ちます。

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
- 52 tools: model info, header, bones, vertices/faces, materials, morphs, display frames, rigid bodies, joints, soft bodies, selection, camera, viewport screenshot, open/save, undo/redo
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

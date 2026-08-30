# Changelog

## 0.2.0 - 2026-08-30

PEPlugin が触れるモデル要素を、ひととおり道具にしました。ツールは 18 個から 52 個へ。

追加:

- **頂点と面**: `list_vertices` / `get_vertex` / `set_vertex` / `list_faces`
  (位置・法線・UV・追加 UV・ウェイト・エッジ倍率・SDEF/QDEF)
- **表示枠**: `list_nodes` / `get_node` / `set_node` / `add_node` / `delete_node`
- **剛体**: `list_bodies` / `get_body` / `set_body` / `add_body` / `delete_body`
- **ジョイント**: `list_joints` / `get_joint` / `set_joint` / `add_joint` / `delete_joint`
- **柔体**: `list_soft_bodies` / `get_soft_body` / `set_soft_body` (PMX 2.1 の全係数)
- **ヘッダ**: `get_header` / `set_header`
- **モーフの中身**: `get_morph` / `set_morph` / `set_morph_offsets` / `add_morph` / `delete_morph`
  (頂点・UV・ボーン・材質・グループ・インパルスの各オフセット)
- **材質**: `get_material` / `delete_material`
- **ボーン**: `set_bone_ik` / `add_bone` / `delete_bone`

拡張:

- `set_bone` が全項目に対応 — 親・接続先・変形階層・付与・**軸固定**・**ローカル軸**・
  外部親・物理後変形。ローカル軸は PEPlugin のインターフェイスに出ていないため、
  実体側のフィールドを読み書きします。X と Z を渡すと仕様どおり Y = Z×X、
  Z' = X×Y を求めて 3 軸まとめて書きます。
- `set_material` が影の各フラグ・スフィア・トゥーン・頂点色・描画種別に対応。
- `get_bone` がローカル軸 (X/Y/Z) と外部親キーを返すように。

覚え書き (実測):

- PMX エディタは **Root と表情の表示枠を `pmx.Node` に持っていません**。別の口で持っているので、
  一覧とは分けて返しています。
- 表示枠の変更は `PmxUpdateObject.Node` では反映されません。全体更新でのみ通ります。
- 要素を追加した直後に更新の対象へ新しい番号を渡すと範囲外になります (向こうの一覧はまだ増えていない)。
- ヘッダは保存時に中身から書き直されます。

## 0.1.0 - 2026-08-26

初回リリース / Initial release.

- PMX エディタ起動と同時に常駐する MCP サーバー (Streamable HTTP, `2025-06-18`)
- ツール 18 個: モデル情報 / ボーン / 材質 / モーフ / 選択 / カメラ / スクリーンショット / 開く・保存 / Undo・Redo
- ループバック待ち受け + Origin チェック + 任意の Bearer トークン
- `allowWrite` / `allowFileAccess` による読み取り専用モード
- Visual Studio 不要のビルドスクリプト (`build.ps1`) と MSBuild 用 csproj

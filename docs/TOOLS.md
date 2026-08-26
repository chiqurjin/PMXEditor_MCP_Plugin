# ツールリファレンス / Tool reference

すべてのインデックスは 0 始まりで、**PMX エディタで現在開いているモデル**を指します。
書き込み系ツールは `allowWrite: false` のとき、ファイル系ツールは `allowFileAccess: false` のときエラーを返します。

戻り値は MCP の `content` (テキスト) と `structuredContent` (同じ内容の JSON オブジェクト) の両方で返します。
`capture_viewport` のみ画像コンテンツを返します。

---

## モデル情報

### `get_model_info` (読み取り)

引数なし。

```json
{
  "filePath": "C:\\models\\miku.pmx",
  "name": "初音ミク",
  "nameEn": "Miku",
  "comment": "...",
  "commentEn": "",
  "pmxVersion": 2.0,
  "counts": { "vertex": 12480, "material": 17, "bone": 142, "morph": 58,
              "node": 9, "rigidBody": 34, "joint": 28, "softBody": 0 },
  "undoCount": 3,
  "redoCount": 0
}
```

### `set_model_info` (書き込み)

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `name` | string | 日本語モデル名 |
| `name_en` | string | 英語モデル名 |
| `comment` | string | 日本語コメント |
| `comment_en` | string | 英語コメント |

渡したフィールドだけが変更されます。1 つ以上必須。

### `undo` / `redo` (書き込み)

引数なし。エディタ本体の Undo / Redo を実行し、実行後の段数を返します。

---

## ボーン

### `list_bones` (読み取り)

| 引数 | 型 | 既定 | 説明 |
| --- | --- | --- | --- |
| `offset` | integer | 0 | 開始位置 (フィルタ後の並びに対して) |
| `limit` | integer | 200 | 取得件数 (最大 1000) |
| `name_contains` | string | - | 日本語名または英語名の部分一致 |

```json
{
  "total": 142, "matched": 3, "offset": 0, "count": 3,
  "bones": [
    { "index": 12, "name": "左腕", "nameEn": "arm_L", "parentIndex": 11,
      "parentName": "左肩", "position": [1.2, 15.4, 0.1],
      "level": 0, "visible": true, "isIK": false }
  ]
}
```

### `get_bone` (読み取り)

`index` または `name` のどちらかを渡します。フラグ一式、付与親、軸固定、IK リンクまで返します。

```json
{
  "index": 89, "name": "左足ＩＫ", "position": [1.1, 1.4, 0.3],
  "parentIndex": 0, "toBoneIndex": -1, "toOffset": [0, 0, 1.2], "level": 0,
  "flags": { "rotatable": true, "translatable": true, "visible": true,
             "controllable": true, "isIK": true, "appendRotation": false,
             "appendTranslation": false, "appendLocal": false, "fixAxis": false,
             "localFrame": false, "afterPhysics": false, "external": false },
  "ik": { "targetIndex": 85, "loopCount": 40, "angle": 0.0349,
          "links": [ { "boneIndex": 84, "boneName": "左ひざ", "isLimit": true,
                       "low": [-3.14, 0, 0], "high": [-0.008, 0, 0] } ] }
}
```

### `set_bone` (書き込み)

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` / `name` | integer / string | 対象ボーンの指定 (どちらか必須) |
| `new_name` / `new_name_en` | string | 名前の変更 |
| `position` | number[3] | ボーン位置 `[x, y, z]` |
| `visible` | boolean | 表示 |
| `controllable` | boolean | 操作 |
| `rotatable` | boolean | 回転フラグ |
| `translatable` | boolean | 移動フラグ |

---

## 材質

### `list_materials` (読み取り)

引数は `offset` / `limit` / `name_contains` (`list_bones` と同じ)。
面数・拡散色 (RGBA)・反射色・環境色・エッジ・テクスチャ・スフィア・Toon・メモを返します。

### `set_material` (書き込み)

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` / `name` | integer / string | 対象材質の指定 (どちらか必須) |
| `new_name` / `new_name_en` | string | 名前の変更 |
| `diffuse` | number[4] | 拡散色 `[r, g, b, a]` (0.0-1.0) |
| `specular` | number[3] | 反射色 `[r, g, b]` |
| `ambient` | number[3] | 環境色 `[r, g, b]` |
| `power` | number | 反射強度 |
| `edge` | boolean | エッジ描画 |
| `edge_color` | number[4] | エッジ色 `[r, g, b, a]` |
| `edge_size` | number | エッジ太さ |
| `both_draw` | boolean | 両面描画 |
| `texture` | string | テクスチャの相対パス |
| `memo` | string | メモ |

---

## モーフ

### `list_morphs` (読み取り)

`offset` / `limit` / `name_contains` に加えて `kind` (`Vertex`, `Bone`, `Material`, `UV`, `Group` など) で絞り込めます。

### `set_morph_name` (書き込み)

`index` または `name` で対象を指定し、`new_name` / `new_name_en` を渡します。

---

## 選択

### `get_selection` (読み取り)

PmxView の選択 (ボーン・頂点・面) と材質リストの選択、各リストのカーソル位置を返します。
インデックス配列は 500 件で打ち切られ、`count` に実際の件数、`truncated` に打ち切りの有無が入ります。

```json
{
  "bones":     { "count": 2, "indices": [12, 13], "truncated": false },
  "vertices":  { "count": 0, "indices": [], "truncated": false },
  "faces":     { "count": 0, "indices": [], "truncated": false },
  "materials": { "count": 1, "indices": [4], "truncated": false },
  "listCursor": { "bone": 12, "material": 4, "morph": -1,
                  "vertex": -1, "rigidBody": -1, "joint": -1 }
}
```

### `set_selection` (書き込み)

`bone_indices` / `vertex_indices` / `face_indices` / `material_indices` のうち、渡したものだけを置き換えます。

---

## ビュー

### `capture_viewport` (読み取り)

| 引数 | 型 | 既定 | 説明 |
| --- | --- | --- | --- |
| `max_width` | integer | 1024 | この幅に収まるよう縮小。`0` で原寸 |

PmxView のクライアント領域を PNG で返します (MCP の image コンテンツ)。

### `get_camera` (読み取り)

`position` / `target` / `up` / `rotateCenter` を `[x, y, z]` で返します。

### `set_camera` (書き込み)

| 引数 | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `position` | number[3] | ○ | 視点位置 |
| `target` | number[3] | ○ | 注視点 |
| `up` | number[3] | - | 上方向 (省略時は現在値) |

---

## ファイル

### `open_model` (書き込み・ファイル)

`path` (必須) に `.pmx` または `.pmd` の絶対パス。**開いているモデルは破棄されます。**

### `save_model` (書き込み・ファイル)

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `path` | string | 省略時は現在開いているファイルへ上書き保存 |
| `overwrite` | boolean | `path` 指定時、既存ファイルを置き換えてよいか (既定 `false`) |

`path` を指定して既存ファイルがあり `overwrite` が `false` の場合はエラーになります。

# ツールリファレンス / Tool reference

すべてのインデックスは 0 始まりで、**PMX エディタで現在開いているモデル**を指します。
書き込み系ツールは `allowWrite: false` のとき、ファイル系ツールは `allowFileAccess: false` のときエラーを返します。

戻り値は MCP の `content` (テキスト) と `structuredContent` (同じ内容の JSON オブジェクト) の両方で返します。
`capture_viewport` のみ画像コンテンツを返します。

ツールは全 52 個です。

---

## モデル情報

`set_header` で版や追加 UV 数を変えても、**保存時に PMX エディタが中身から書き直します**。2.1 の機能を使っていなければ 2.0 で書かれ、どの頂点も使っていない追加 UV は落ちます。先に中身を作ってください。

### `get_model_info` (読み取り)

Summary of the model open in PMX Editor: names, comments, element counts, file path and undo depth.

引数なし。

### `set_model_info` (書き込み)

Renames the model or replaces its comments. Only the fields you pass are changed.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `name` | string | Japanese model name |
| `name_en` | string | English model name |
| `comment` | string | Japanese comment |
| `comment_en` | string | English comment |

### `get_header` (読み取り)

The PMX header: format version, how strings are stored, and how many additional UV channels each vertex carries.

引数なし。

### `set_header` (書き込み)

Edits the PMX header. The change takes in the editor, but PMX Editor writes the header from what the model actually contains when it saves: a model using no 2.1 feature is written as 2.0 however you set the version, and an additional UV channel no vertex uses is dropped. Set the content first and the header follows.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `version` | number | Format version, normally 2.0 or 2.1 |
| `string_encode` | int | 0 stores strings as UTF-16, 1 as UTF-8 |
| `uva_count` | int | Additional UV channels per vertex, 0-4 |

### `undo` (書き込み)

Undoes the last edit in PMX Editor.

引数なし。

### `redo` (書き込み)

Redoes the last undone edit in PMX Editor.

引数なし。

---

## ボーン

軸固定 (`fix_axis`) とローカル軸 (`local_frame` / `local_x` / `local_z`) は、PMX 仕様上**変形には関与しない**表示・操作の制限項目です。手で動かすときの振る舞いだけが変わります。

ローカル軸は X と Z を渡すと、仕様どおり Y = Z×X、Z' = X×Y を計算して3 軸まとめて書きます。片方だけ渡した場合は、もう片方は今の値を使います。

### `list_bones` (読み取り)

Lists bones with their index, names, parent and position. Paged, and filterable by name.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `offset` | int | First bone index to return (default 0) |
| `limit` | int | How many bones to return (default 200, max 1000) |
| `name_contains` | string | Only bones whose Japanese or English name contains this text |

### `get_bone` (読み取り)

Full detail of one bone: flags, deform level, append parent, fixed axis, local frame axes and IK links. Identify it by index or name.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Bone index |
| `name` | string | Japanese bone name |

### `set_bone` (書き込み)

Edits one bone. Identify it by index or name; only the fields you pass are changed. Bone references accept either an index (-1 for none) or the matching _name field. fix_axis and local_frame only affect how the bone is handled in an editor, not how the model deforms.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Bone index |
| `name` | string | Japanese bone name used to find the bone |
| `new_name` | string | New Japanese name |
| `new_name_en` | string | New English name |
| `position` | number[3] | New bone position [x, y, z] |
| `parent` | int | Parent bone index, or -1 for none |
| `parent_name` | string | Parent bone name |
| `to_bone` | int | Tip bone index; sets the tip to be a bone, -1 for none |
| `to_bone_name` | string | Tip bone name |
| `to_offset` | number[3] | Tip as an offset [x, y, z]; clears the tip bone |
| `level` | int | Deform level (transform order) |
| `visible` | bool | Show the bone in the editor |
| `controllable` | bool | Allow manual operation |
| `rotatable` | bool | Rotation flag |
| `translatable` | bool | Translation flag |
| `after_physics` | bool | Deform after physics |
| `append_rotation` | bool | Take rotation from the append parent |
| `append_translation` | bool | Take translation from the append parent |
| `append_local` | bool | Take the append value in the parent's local frame |
| `append_parent` | int | Append parent bone index, or -1 for none |
| `append_parent_name` | string | Append parent bone name |
| `append_ratio` | number | Append ratio |
| `fix_axis` | bool | Restrict operation to a single axis |
| `fix_axis_vector` | number[3] | The fixed axis direction [x, y, z] |
| `local_frame` | bool | Give the bone its own operation frame |
| `local_x` | number[3] | Local frame X axis [x, y, z] |
| `local_z` | number[3] | Local frame Z axis [x, y, z] |
| `external` | bool | Deform from a parent outside the model |
| `external_key` | int | External parent key |
| `is_ik` | bool | Make this bone an IK bone |

### `set_bone_ik` (書き込み)

Edits the IK settings of one bone: its target, loop count, per-loop angle limit and the whole link chain. The angle is in radians, and PMX stores it four times larger than the raw value a PMD file holds. Passing links replaces the chain; each entry is {"bone": index or "bone_name": name} and may add {"low": [x,y,z], "high": [x,y,z]} in radians to limit that link.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Bone index of the IK bone |
| `name` | string | Japanese name of the IK bone |
| `target` | int | Target bone index (the tip that reaches for the IK bone) |
| `target_name` | string | Target bone name |
| `loop_count` | int | How many solver iterations |
| `angle` | number | Per-iteration angle limit, in radians |
| `links` | object[] | Replacement link chain, tip first |

### `add_bone` (書き込み)

Adds a bone and returns its index. Everything not passed keeps the editor default.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `new_name` | string | Japanese name |
| `new_name_en` | string | English name |
| `position` | number[3] | Bone position [x, y, z] |
| `parent` | int | Parent bone index, or -1 for none |
| `parent_name` | string | Parent bone name |
| `level` | int | Deform level |
| `visible` | bool | Show the bone in the editor |
| `controllable` | bool | Allow manual operation |
| `rotatable` | bool | Rotation flag |
| `translatable` | bool | Translation flag |

### `delete_bone` (書き込み)

Deletes one bone. Vertices weighted to it, and anything else that referenced it, are left pointing at nothing, so this is for bones you know are unused.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Bone index |
| `name` | string | Japanese bone name |

---

## 頂点・面

モデルの頂点は数万あるので一覧は必ず区切って返します (既定 50)。

面は材質ごとに持つのが PMX の形なので、`list_faces` も材質を指して呼びます。

### `list_vertices` (読み取り)

Lists vertices with position, normal, UV and bone weights. Paged; models have tens of thousands of vertices, so ask for a window rather than all of them.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `offset` | int | First vertex index to return (default 0) |
| `limit` | int | How many vertices to return (default 50, max 1000) |

### `get_vertex` (読み取り)

Full detail of one vertex: position, normal, UV, additional UVs, the bones that deform it with their weights, the edge scale and any SDEF data.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | **必須。**Vertex index |

### `set_vertex` (書き込み)

Edits one vertex. Only the fields you pass are changed. Weights are not normalised for you: pass the set you want.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | **必須。**Vertex index |
| `position` | number[3] | Position [x, y, z] |
| `normal` | number[3] | Normal [x, y, z] |
| `uv` | number[2] | Texture coordinate [u, v] |
| `uva1` | number[4] | Additional UV 1 [x, y, z, w] |
| `uva2` | number[4] | Additional UV 2 [x, y, z, w] |
| `uva3` | number[4] | Additional UV 3 [x, y, z, w] |
| `uva4` | number[4] | Additional UV 4 [x, y, z, w] |
| `bone1` | int | Bone index for slot 1, or -1 for none |
| `bone2` | int | Bone index for slot 2, or -1 for none |
| `bone3` | int | Bone index for slot 3, or -1 for none |
| `bone4` | int | Bone index for slot 4, or -1 for none |
| `weight1` | number | Weight for slot 1 |
| `weight2` | number | Weight for slot 2 |
| `weight3` | number | Weight for slot 3 |
| `weight4` | number | Weight for slot 4 |
| `edge_scale` | number | Per-vertex outline scale; 0 hides the outline here |
| `sdef` | bool | Use SDEF deformation |
| `qdef` | bool | Use QDEF deformation |
| `sdef_c` | number[3] | SDEF C [x, y, z] |
| `sdef_r0` | number[3] | SDEF R0 [x, y, z] |
| `sdef_r1` | number[3] | SDEF R1 [x, y, z] |

### `list_faces` (読み取り)

Lists the triangles of one material as vertex indices. Identify the material by index or name. Paged.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Material index |
| `name` | string | Japanese material name |
| `offset` | int | First triangle to return (default 0) |
| `limit` | int | How many triangles to return (default 200, max 1000) |

---

## 材質

### `list_materials` (読み取り)

Lists materials with their index, names, face count, colours and texture paths.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `offset` | int | First material index to return (default 0) |
| `limit` | int | How many materials to return (default 200, max 1000) |
| `name_contains` | string | Only materials whose Japanese or English name contains this text |

### `get_material` (読み取り)

Full detail of one material, including the shadow flags and where its faces sit in the draw order.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Material index |
| `name` | string | Japanese material name |

### `set_material` (書き込み)

Edits one material. Identify it by index or name; only the fields you pass are changed. Colour components are 0.0-1.0.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Material index |
| `name` | string | Japanese material name used to find the material |
| `new_name` | string | New Japanese name |
| `new_name_en` | string | New English name |
| `diffuse` | number[4] | Diffuse colour [r, g, b, a] |
| `specular` | number[3] | Specular colour [r, g, b] |
| `ambient` | number[3] | Ambient colour [r, g, b] |
| `power` | number | Specular power |
| `edge` | bool | Draw the outline |
| `edge_color` | number[4] | Outline colour [r, g, b, a] |
| `edge_size` | number | Outline thickness |
| `both_draw` | bool | Render both faces |
| `texture` | string | Texture path relative to the model |
| `sphere` | string | Sphere map path relative to the model |
| `sphere_mode` | string | Sphere blend: None, Mul, Add, SubTex |
| `toon` | string | Toon path, or a shared name such as toon01.bmp |
| `shadow` | bool | Cast onto the ground shadow |
| `self_shadow_map` | bool | Write to the self-shadow map |
| `self_shadow` | bool | Receive the self-shadow |
| `vertex_color` | bool | Use per-vertex colour (PMX 2.1) |
| `primitive_type` | string | Primitive kind (PMX 2.1): Tri, Point, Line |
| `memo` | string | Free-text memo |

### `delete_material` (書き込み)

Deletes one material and every face that belongs to it. The vertices stay; run the editor's cleanup if you want them gone too.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Material index |
| `name` | string | Japanese material name |

---

## モーフ

`panel` は 1:まゆ / 2:目 / 3:リップ / 4:その他。

`set_morph` で `kind` を変えると中身は空になります (種類ごとに中身の形が違うため)。中身は `set_morph_offsets` で入れ直してください。

### `list_morphs` (読み取り)

Lists morphs with their index, names, kind (Vertex/Bone/Material/UV/Group), panel and offset count.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `offset` | int | First morph index to return (default 0) |
| `limit` | int | How many morphs to return (default 200, max 1000) |
| `name_contains` | string | Only morphs whose Japanese or English name contains this text |
| `kind` | string | Only morphs of this kind, e.g. Vertex, Bone, Material, UV, Group |

### `get_morph` (読み取り)

Full detail of one morph, including its offsets. The shape of each offset depends on the kind: a vertex morph moves vertices, a bone morph moves and turns bones, a material morph scales or replaces material values, a group morph drives other morphs.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Morph index |
| `name` | string | Japanese morph name |
| `offset` | int | First offset to return (default 0) |
| `limit` | int | How many offsets to return (default 200, max 1000) |

### `set_morph` (書き込み)

Edits one morph. panel is 1 eyebrow, 2 eye, 3 mouth, 4 other. Changing kind clears the offsets, because offsets of one kind do not fit another.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Morph index |
| `name` | string | Japanese name used to find the morph |
| `new_name` | string | New Japanese name |
| `new_name_en` | string | New English name |
| `panel` | int | Operation panel: 1 eyebrow, 2 eye, 3 mouth, 4 other |
| `kind` | string | Morph kind: Group, Vertex, Bone, UV, UVA1, UVA2, UVA3, UVA4, Material, Flip, Impulse |

### `set_morph_name` (書き込み)

Renames one morph, found by index or current Japanese name.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Morph index |
| `name` | string | Current Japanese name used to find the morph |
| `new_name` | string | New Japanese name |
| `new_name_en` | string | New English name |

### `set_morph_offsets` (書き込み)

Replaces every offset of one morph. Each entry matches the morph kind. Vertex: vertex index plus offset [x,y,z]. UV: vertex index plus offset [x,y,z,w]. Bone: bone index or bone_name, plus translation [x,y,z] and rotation [x,y,z,w]. Group: morph index plus ratio. Material: material index, op 0 or 1, then any of diffuse, specular, ambient, power, edge_size, edge_color, tex, sphere, toon. Impulse: body index, local, velocity [x,y,z], torque [x,y,z].

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Morph index |
| `name` | string | Japanese name used to find the morph |
| `offsets` | object[] | The complete replacement offset list |

### `add_morph` (書き込み)

Adds an empty morph and returns its index. Fill it with set_morph_offsets.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `new_name` | string | Japanese name |
| `new_name_en` | string | English name |
| `panel` | int | Operation panel: 1 eyebrow, 2 eye, 3 mouth, 4 other |
| `kind` | string | Morph kind: Group, Vertex, Bone, UV, UVA1, UVA2, UVA3, UVA4, Material, Flip, Impulse |
| `offsets` | object[] | Optional initial offsets, as in set_morph_offsets |

### `delete_morph` (書き込み)

Deletes one morph. Display frames and group morphs that referenced it are left pointing at nothing, so check list_nodes afterwards.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Morph index |
| `name` | string | Japanese morph name |

---

## 表示枠

PMX エディタは **Root と表情の 2 枠を一覧に持ちません**。その 2 つは `list_nodes` の `root` / `expression` として別に返り、`get_node` / `set_node` では `which: "root"` `"expression"` で指します。

### `list_nodes` (読み取り)

Lists the ordinary display frames with the bones and morphs they hold. The two fixed frames a PMX must have, Root and Expression, are not part of that list and come back separately as root and expression.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `offset` | int | First frame index to return (default 0) |
| `limit` | int | How many frames to return (default 200, max 1000) |

### `get_node` (読み取り)

Full detail of one display frame: every item, in order, as a bone or morph index. Pass which=root or which=expression for the two fixed frames.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Frame index |
| `name` | string | Japanese frame name |
| `which` | string | Instead of an index: root or expression, for the two fixed frames PMX Editor keeps outside the list |

### `set_node` (書き込み)

Edits one display frame. Passing items replaces the whole contents; each item is {"bone": index} or {"morph": index}, or the same with _name.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Frame index |
| `name` | string | Japanese name used to find the frame |
| `which` | string | Instead of an index: root or expression |
| `new_name` | string | New Japanese name |
| `new_name_en` | string | New English name |
| `items` | object[] | Replacement contents, in order. Each entry is {"bone": index}, {"bone_name": name}, {"morph": index} or {"morph_name": name}. |

### `add_node` (書き込み)

Adds a display frame and returns its index.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `new_name` | string | Japanese name |
| `new_name_en` | string | English name |
| `items` | object[] | Contents, in the same shape as set_node |

### `delete_node` (書き込み)

Deletes one ordinary display frame. The two fixed frames are refused.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Frame index |
| `name` | string | Japanese frame name |

---

## 剛体

`group` は 0〜15 の所属グループ、`pass_group` は**当たらない相手**を表す16 個の真偽値です (PMX の持ち方そのまま)。`rotation` はラジアン。

### `list_bodies` (読み取り)

Lists rigid bodies with their bone, mode, shape and placement. Paged, and filterable by name.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `offset` | int | First body index to return (default 0) |
| `limit` | int | How many bodies to return (default 200, max 1000) |
| `name_contains` | string | Only bodies whose Japanese or English name contains this text |

### `get_body` (読み取り)

Full detail of one rigid body, including mass, damping and collision groups.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Rigid body index |
| `name` | string | Japanese rigid body name |

### `set_body` (書き込み)

Edits one rigid body. Only the fields you pass are changed. mode is one of Static, Dynamic, DynamicWithBone; shape is one of Sphere, Box, Capsule. Rotation is in radians.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Rigid body index |
| `name` | string | Japanese name used to find the body |
| `new_name` | string | New Japanese name |
| `new_name_en` | string | New English name |
| `bone` | int | Bone index this body follows, or -1 for none |
| `bone_name` | string | Bone name this body follows |
| `mode` | string | Physics mode: Static, Dynamic, DynamicWithBone |
| `shape` | string | Collision shape: Sphere, Box, Capsule |
| `size` | number[3] | Shape size [x, y, z] |
| `position` | number[3] | Position [x, y, z] |
| `rotation` | number[3] | Rotation in radians [x, y, z] |
| `group` | int | Collision group, 0-15 |
| `pass_group` | boolean[16] | 16 booleans: the groups this body does not collide with |
| `mass` | number | Mass |
| `position_damping` | number | Linear damping |
| `rotation_damping` | number | Angular damping |
| `restitution` | number | Bounciness |
| `friction` | number | Friction |

### `add_body` (書き込み)

Adds a rigid body and returns its index. Everything not passed keeps the editor default, so a new body can be created with just a name and a bone.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `new_name` | string | Japanese name |
| `new_name_en` | string | English name |
| `bone` | int | Bone index this body follows, or -1 for none |
| `bone_name` | string | Bone name this body follows |
| `mode` | string | Physics mode: Static, Dynamic, DynamicWithBone |
| `shape` | string | Collision shape: Sphere, Box, Capsule |
| `size` | number[3] | Shape size [x, y, z] |
| `position` | number[3] | Position [x, y, z] |
| `rotation` | number[3] | Rotation in radians [x, y, z] |
| `group` | int | Collision group, 0-15 |
| `mass` | number | Mass |

### `delete_body` (書き込み)

Deletes one rigid body. Joints that referenced it are left pointing at nothing, so check list_joints afterwards.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Rigid body index |
| `name` | string | Japanese rigid body name |

---

## ジョイント

### `list_joints` (読み取り)

Lists joints with the two bodies they connect and their placement. Paged.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `offset` | int | First joint index to return (default 0) |
| `limit` | int | How many joints to return (default 200, max 1000) |
| `name_contains` | string | Only joints whose Japanese or English name contains this text |

### `get_joint` (読み取り)

Full detail of one joint, including its movement and angle limits and spring constants.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Joint index |
| `name` | string | Japanese joint name |

### `set_joint` (書き込み)

Edits one joint. Only the fields you pass are changed. kind is one of Sp6DOF, G6DOF, P2P, ConeTwist, Slider, Hinge. Angles are in radians.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Joint index |
| `name` | string | Japanese name used to find the joint |
| `new_name` | string | New Japanese name |
| `new_name_en` | string | New English name |
| `body_a` | int | Index of the first rigid body, or -1 for none |
| `body_b` | int | Index of the second rigid body, or -1 for none |
| `kind` | string | Joint kind: Sp6DOF, G6DOF, P2P, ConeTwist, Slider, Hinge |
| `position` | number[3] | Position [x, y, z] |
| `rotation` | number[3] | Rotation in radians [x, y, z] |
| `move_low` | number[3] | Lower movement limit [x, y, z] |
| `move_high` | number[3] | Upper movement limit [x, y, z] |
| `angle_low` | number[3] | Lower angle limit in radians [x, y, z] |
| `angle_high` | number[3] | Upper angle limit in radians [x, y, z] |
| `spring_move` | number[3] | Movement spring constants [x, y, z] |
| `spring_rotate` | number[3] | Rotation spring constants [x, y, z] |

### `add_joint` (書き込み)

Adds a joint and returns its index.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `new_name` | string | Japanese name |
| `new_name_en` | string | English name |
| `body_a` | int | Index of the first rigid body |
| `body_b` | int | Index of the second rigid body |
| `kind` | string | Joint kind: Sp6DOF, G6DOF, P2P, ConeTwist, Slider, Hinge |
| `position` | number[3] | Position [x, y, z] |
| `rotation` | number[3] | Rotation in radians [x, y, z] |

### `delete_joint` (書き込み)

Deletes one joint.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Joint index |
| `name` | string | Japanese joint name |

---

## 柔体

PMX 2.1 の機能です。ほとんどのモデルは 0 個です。

### `list_soft_bodies` (読み取り)

Lists soft bodies. PMX 2.1 only; most models have none.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `offset` | int | First soft body index to return (default 0) |
| `limit` | int | How many to return (default 200, max 1000) |

### `get_soft_body` (読み取り)

Full detail of one soft body, including every simulation coefficient.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Soft body index |
| `name` | string | Japanese soft body name |

### `set_soft_body` (書き込み)

Edits one soft body. Only the fields you pass are changed. shape is one of TriMesh, Rope. The single-letter coefficients are the PMX 2.1 names.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `index` | int | Soft body index |
| `name` | string | Japanese name used to find the soft body |
| `new_name` | string | New Japanese name |
| `new_name_en` | string | New English name |
| `shape` | string | Shape: TriMesh, Rope |
| `material` | int | Material index the soft body is built from |
| `group` | int | Collision group, 0-15 |
| `total_mass` | number | Total mass |
| `margin` | number | Collision margin |
| `aero_model` | int | Aero model |
| `cluster_count` | int | Cluster count |
| `bending_link_distance` | int | Bending link distance |
| `generate_bending_links` | bool | Generate bending links |
| `generate_clusters` | bool | Generate clusters |
| `randomize_constraints` | bool | Randomise constraints |
| `coefficients` | 任意 | Any of VCF, DP, DG, LF, PR, VC, DF, MT, CHR, KHR, SHR, AHR, SRHR_CL, SKHR_CL, SSHR_CL, SR_SPLT_CL, SK_SPLT_CL, SS_SPLT_CL, LST, AST, VST as numbers, and V_IT, P_IT, D_IT, C_IT as integers |

---

## 選択

### `get_selection` (読み取り)

Reads the current selection: bones, vertices, faces and materials selected in PmxView and in the editor lists. Long index lists are truncated, the full size is reported as a count.

引数なし。

### `set_selection` (書き込み)

Replaces the selection in PmxView and the material list. Pass only the kinds you want to change.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `bone_indices` | integer[] | Bone indices to select |
| `vertex_indices` | integer[] | Vertex indices to select |
| `face_indices` | integer[] | Face indices to select |
| `material_indices` | integer[] | Material indices to select |

---

## ビュー

### `capture_viewport` (読み取り)

Captures the PmxView window as a PNG image. Use it to see the model or to check the result of an edit.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `max_width` | int | Downscale so the image is at most this wide (default 1024, 0 keeps full size) |

### `get_camera` (読み取り)

Reads the PmxView camera: eye position, target and up vector.

引数なし。

### `set_camera` (書き込み)

Moves the PmxView camera. Pass position and target as [x, y, z]; up defaults to the current up vector.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `position` | number[3] | **必須。**Eye position [x, y, z] |
| `target` | number[3] | **必須。**Look-at point [x, y, z] |
| `up` | number[3] | Up vector [x, y, z] |

---

## ファイル

### `open_model` (書き込み・ファイル)

Opens a .pmx or .pmd file in PMX Editor, replacing whatever is loaded. Unsaved changes are lost.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `path` | string | **必須。**Absolute path to a .pmx or .pmd file |

### `save_model` (書き込み・ファイル)

Saves the current model. Without a path it overwrites the file that is open; with a path it writes a new file, and refuses to clobber an existing one unless overwrite is true.

| 引数 | 型 | 説明 |
| --- | --- | --- |
| `path` | string | Absolute path of the .pmx file to write |
| `overwrite` | bool | Allow replacing an existing file at path (default false) |

---

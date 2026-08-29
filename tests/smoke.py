"""道具をひととおり動かして確かめる。

使い方:
    py tests\\smoke.py <調べたいPMX> [<書き出し先>]

PMX エディタをプラグイン入りで起動しておくこと。
**読みは開いたモデルに対して、書きは書き出し先の複製に対してだけ**行う。
指定した元のファイルは書き換えない。
"""
import json
import os
import sys

import mcp_client as pmx

sys.stdout.reconfigure(encoding='utf-8')

if len(sys.argv) < 2:
    raise SystemExit(__doc__)

SRC = sys.argv[1].replace(os.sep, '/')
OUT = (sys.argv[2] if len(sys.argv) > 2
       else os.path.splitext(SRC)[0] + '_smoke.pmx').replace(os.sep, '/')

ok = 0
ng = 0


class ToolError(Exception):
    pass


def call(name, args=None):
    text, raw = pmx.call(name, args or {})
    if raw.get('isError'):
        raise ToolError(text)
    try:
        return json.loads(text)
    except ValueError:
        raise ToolError(text)


def check(label, cond, detail=''):
    global ok, ng
    if cond:
        ok += 1
        print(f'  OK   {label}' + (f'   {detail}' if detail else ''))
    else:
        ng += 1
        print(f'  FAIL {label}   {detail}')


def attempt(label, name, args=None):
    """道具を呼び、誤りが返ったらそれを不合格として扱う。"""
    try:
        return call(name, args or {})
    except (SystemExit, ToolError) as e:
        check(label, False, str(e)[:200])
        return None


def refuses(label, name, args, wanted):
    """断るべきものを断るか。"""
    try:
        call(name, args)
        check(label, False, '通ってしまった')
    except (SystemExit, ToolError) as e:
        check(label, wanted in str(e), str(e)[:80])


try:
    pmx.start()
except pmx.NotRunning as e:
    raise SystemExit(str(e))

call('open_model', {'path': SRC})
BASE = call('get_model_info')['counts']
print('調べるモデル:', SRC)
print('書き出し先  :', OUT)
print()

print('== 頭 ==')
h = attempt('頭が読める', 'get_header')
if h:
    check('版が読める', h['version'] >= 2.0, str(h['version']))
    check('文字の入れ方が読める', h['stringEncode'] in (0, 1), h['stringEncodeName'])
    check('追加UVが読める', 0 <= h['uvaCount'] <= 4, str(h['uvaCount']))

print('== 頂点・面 ==')
v = attempt('頂点の一覧', 'list_vertices', {'limit': 3})
if v:
    check('頂点がある', v['total'] > 0, str(v['total']))
    check('区切って返る', v['count'] == min(3, v['total']), str(v['count']))
g = attempt('頂点1つ', 'get_vertex', {'index': 0})
if g:
    check('骨と重みが揃っている', len(g['bones']) == 4 and len(g['weights']) == 4)
    check('輪郭の倍率が読める', 'edgeScale' in g, str(g.get('edgeScale')))
f = attempt('面の一覧', 'list_faces', {'index': 0, 'limit': 2})
if f:
    check('面が3頂点', len(f['faces'][0]) == 3, str(f['faces'][0]))
    check('材質0に面がある', f['total'] > 0, str(f['total']))

print('== 材質 ==')
m = attempt('材質1つ', 'get_material', {'index': 0})
if m:
    check('貼りの欄が読める', 'texture' in m and 'sphere' in m and 'toon' in m)
    check('影の旗が読める', 'selfShadow' in m and 'shadow' in m)
    check('先頭材質の面は0から', m['faceStart'] == 0, str(m['faceStart']))

print('== 表情 ==')
mo = attempt('表情1つ', 'get_morph', {'index': 0, 'limit': 3})
if mo:
    check('種類が名前で出る', mo['kind'] in (
        'Group', 'Vertex', 'Bone', 'UV', 'UVA1', 'UVA2', 'UVA3', 'UVA4',
        'Material', 'Flip', 'Impulse'), mo['kind'])
    check('枠が1〜4', 1 <= mo['panel'] <= 4, str(mo['panel']))
    check('中身の数が読める', mo['offsetCount'] >= 0, str(mo['offsetCount']))

print('== 表示枠 ==')
n = attempt('表示枠の一覧', 'list_nodes')
if n:
    check('枠がある', n['total'] > 0, str(n['total']))
    check('固定の2枠が別に返る', n.get('root') is not None and n.get('expression') is not None,
          f"{(n.get('root') or {}).get('name')} / {(n.get('expression') or {}).get('name')}")

print('== 剛体・ジョイント・柔体 ==')
b = attempt('剛体の一覧', 'list_bodies', {'limit': 2})
if b:
    check('剛体の数が読める', b['total'] >= 0, str(b['total']))
gb = attempt('剛体1つ', 'get_body', {'index': 0}) if b and b['total'] else None
if gb:
    check('質量が読める', 'mass' in gb, str(gb.get('mass')))
    check('当たらない組が16', gb['passGroup'] is not None and len(gb['passGroup']) == 16)
    check('種別が名前で出る', gb['mode'] in ('Static', 'Dynamic', 'DynamicWithBone'), gb['mode'])
j = attempt('ジョイントの一覧', 'list_joints', {'limit': 2})
gj = attempt('ジョイント1つ', 'get_joint', {'index': 0}) if j and j['total'] else None
if gj:
    check('ばね定数が読める', 'springMove' in gj)
    check('繋ぐ剛体が読める', gj['bodyAIndex'] >= 0 and gj['bodyBIndex'] >= 0,
          f"{gj['bodyAName']} - {gj['bodyBName']}")
s = attempt('柔体の一覧', 'list_soft_bodies')
if s:
    check('柔体の数が読める', s['total'] >= 0, str(s['total']))

print('== 書き(作業用の複製へ) ==')
r = attempt('骨を足す', 'add_bone',
            {'new_name': 'ためし骨', 'position': [0, 20, 0], 'parent': 0})
if r:
    check('足りた', r['name'] == 'ためし骨', f"番号{r['index']}")

r = attempt('ＩＫを組む', 'set_bone_ik', {
    'name': 'ためし骨',
    'target': 1,
    'loop_count': 12,
    'angle': 0.75,
    'links': [
        {'bone': 1, 'low': [0, 0, -0.2], 'high': [0, 0, 0.2]},
        {'bone': 0},
    ],
})
if r:
    ik = r.get('ik') or {}
    check('回数12', ik.get('loopCount') == 12, str(ik.get('loopCount')))
    check('制限角0.75', abs((ik.get('angle') or 0) - 0.75) < 1e-6, str(ik.get('angle')))
    check('鎖2本', len(ik.get('links', [])) == 2)
    check('1本目に制限あり', ik['links'][0]['isLimit'] is True)
    check('2本目に制限なし', ik['links'][1]['isLimit'] is False)

r = attempt('軸固定とローカル軸を入れる', 'set_bone', {
    'name': 'ためし骨',
    'fix_axis': True, 'fix_axis_vector': [0.7071, -0.7071, 0],
    'local_frame': True, 'local_x': [0.7071, -0.7071, 0], 'local_z': [0, 0, 1],
})
if r:
    check('軸固定が入る', r['flags']['fixAxis'] is True)
    check('軸固定の向きが残る', abs(r['fixAxisVector'][0] - 0.7071) < 1e-4,
          str(r.get('fixAxisVector')))
    check('ローカル軸が入る', r['flags']['localFrame'] is True)
    check('ローカルXが残る', r.get('localX') and abs(r['localX'][0] - 0.7071) < 1e-4,
          str(r.get('localX')))
    # Y は X と Z から導かれる。Z×X なので、この組では (0,0,1)×(0.7071,-0.7071,0)
    check('ローカルYが導かれている', r.get('localY') is not None, str(r.get('localY')))

r = attempt('表情を足す', 'add_morph', {
    'new_name': 'ためし表情', 'panel': 3, 'kind': 'Vertex',
    'offsets': [{'vertex': 0, 'offset': [0.5, 0, 0]},
                {'vertex': 1, 'offset': [0, 0.5, 0]}],
})
if r:
    check('頂点表情2つ', r['offsetCount'] == 2, str(r['offsetCount']))
    check('枠はリップ', r['panel'] == 3, str(r['panel']))

r = attempt('骨表情も作れる', 'add_morph', {
    'new_name': 'ためし骨表情', 'panel': 4, 'kind': 'Bone',
    'offsets': [{'bone': 0, 'translation': [1, 0, 0],
                 'rotation': [0, 0, 0.3826834, 0.9238795]}],
})
if r:
    check('骨表情1つ', r['offsetCount'] == 1 and r['kind'] == 'Bone', r['kind'])

r = attempt('表示枠を足す', 'add_node', {
    'new_name': 'ためし枠',
    'items': [{'bone': 0}, {'morph_name': 'ためし表情'}],
})
if r:
    check('枠に2つ入る', r['itemCount'] == 2, str(r['itemCount']))
    check('骨と表情が並ぶ',
          r['items'][0]['kind'] == 'bone' and r['items'][1]['kind'] == 'morph')

r = attempt('剛体を足す', 'add_body', {
    'new_name': 'ためし剛体', 'bone': 0, 'mode': 'Dynamic',
    'shape': 'Capsule', 'size': [0.5, 2, 0], 'position': [1, 15, 0], 'mass': 2.5,
})
newbody = r['index'] if r else -1
if r:
    check('剛体が足りた', r['mode'] == 'Dynamic' and r['shape'] == 'Capsule',
          f"{r['mode']}/{r['shape']}")
    check('質量2.5', abs(r['mass'] - 2.5) < 1e-6, str(r['mass']))

r = attempt('ジョイントを足す', 'add_joint', {
    'new_name': 'ためしジョイント', 'body_a': 0, 'body_b': newbody, 'kind': 'Sp6DOF',
})
if r:
    check('ジョイントが足りた', r['bodyBIndex'] == newbody, str(r['bodyBIndex']))

r = attempt('頂点を書き換える', 'set_vertex',
            {'index': 0, 'edge_scale': 0.25, 'uv': [0.125, 0.875]})
if r:
    check('輪郭の倍率0.25', abs(r['edgeScale'] - 0.25) < 1e-6, str(r['edgeScale']))
    check('UVが入った', abs(r['uv'][0] - 0.125) < 1e-6, str(r['uv']))

r = attempt('材質を書き換える', 'set_material',
            {'index': 0, 'sphere_mode': 'Add', 'self_shadow': False, 'power': 12.5})
if r:
    check('球の混ぜ方Add', r['sphereMode'] == 'Add', r['sphereMode'])
    check('自己影を切った', r['selfShadow'] is False)
    check('光沢12.5', abs(r['power'] - 12.5) < 1e-6, str(r['power']))

print('== 断り方 ==')
refuses('固定の枠は消せない', 'delete_node', {'which': 'root'}, 'cannot be deleted')
refuses('知らない種別は断る', 'set_body', {'index': 0, 'mode': 'Nonsense'}, 'must be one of')
refuses('範囲の外は断る', 'get_vertex', {'index': 99999999}, 'out of range')
refuses('目印なしは断る', 'get_bone', {}, 'either')

print('== 保存して読み直す ==')
attempt('保存', 'save_model', {'path': OUT, 'overwrite': True})
call('open_model', {'path': OUT})
c2 = call('get_model_info')['counts']
check('骨が1本増えている', c2['bone'] == BASE['bone'] + 1, f"{BASE['bone']} → {c2['bone']}")
check('表情が2つ増えている', c2['morph'] == BASE['morph'] + 2, f"{BASE['morph']} → {c2['morph']}")
check('枠が1つ増えている', c2['node'] == BASE['node'] + 1, f"{BASE['node']} → {c2['node']}")
check('剛体が1つ増えている', c2['rigidBody'] == BASE['rigidBody'] + 1,
      f"{BASE['rigidBody']} → {c2['rigidBody']}")
check('ジョイントが1つ増えている', c2['joint'] == BASE['joint'] + 1,
      f"{BASE['joint']} → {c2['joint']}")

b2 = call('get_bone', {'name': 'ためし骨'})
check('ＩＫが残る', (b2.get('ik') or {}).get('loopCount') == 12,
      str((b2.get('ik') or {}).get('loopCount')))
check('軸固定が残る', b2['flags']['fixAxis'] is True)
check('ローカル軸が残る', b2['flags']['localFrame'] is True and b2.get('localX') is not None,
      str(b2.get('localX')))
v2 = call('get_vertex', {'index': 0})
check('輪郭の倍率が残る', abs(v2['edgeScale'] - 0.25) < 1e-6, str(v2['edgeScale']))

# ヘッダは「中身から」書き直される。使えば残ることを見ておく
call('set_header', {'uva_count': 1})
call('set_vertex', {'index': 0, 'uva1': [0.1, 0.2, 0.3, 0.4]})
call('save_model', {'path': OUT, 'overwrite': True})
call('open_model', {'path': OUT})
check('使えば追加UVは残る', call('get_header')['uvaCount'] == 1)
check('その値も残る', abs(call('get_vertex', {'index': 0})['uva1'][0] - 0.1) < 1e-6)

print()
print(f'===== 合格 {ok} / 不合格 {ng} =====')
sys.exit(1 if ng else 0)

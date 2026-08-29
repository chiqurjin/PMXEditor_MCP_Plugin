"""PMX エディタの MCP サーバーを素の JSON-RPC で叩く小さな道具。

MCP クライアントを用意しなくても、この 1 ファイルでひととおり試せる。

    py mcp_client.py                       … 道具の一覧
    py mcp_client.py get_model_info        … 引数なしの道具
    py mcp_client.py get_bone "{\\"name\\": \\"左腕\\"}"

PMX エディタをプラグイン入りで起動しておくこと。
"""
import json
import sys
import urllib.error
import urllib.request

HOST = "127.0.0.1"
PORT = 38731
PATH = "/mcp"
URL = "http://%s:%d%s" % (HOST, PORT, PATH)

_sid = None


class NotRunning(Exception):
    """サーバーに繋がらない。PMX エディタ側の問題なので、切り分けを添えて伝える。"""


def _explain():
    return (
        "PMX エディタの MCP サーバー (%s) に繋がりません。\n"
        "\n"
        "  1. PMX エディタが起動しているか。**起動と同時にサーバーが立ちます。**\n"
        "  2. PmxMcpPlugin.dll が PMX エディタの _plugin\\User\\ に入っているか。\n"
        "     入れた後は PMX エディタを起動し直すまで読み込まれません。\n"
        "  3. [編集]-[プラグイン]-[MCP Server] で状態を確認できます。\n"
        "  4. ポートを変えている場合は PmxMcpPlugin.json に合わせてください。\n"
        "  5. PMX エディタを 2 つ起動していると、後から起動したほうは\n"
        "     ポートを取れません。" % URL
    )


def rpc(method, params=None):
    """JSON-RPC を 1 往復。MCP の誤りは SystemExit、繋がらないのは NotRunning。"""
    global _sid
    body = json.dumps({"jsonrpc": "2.0", "id": 1, "method": method,
                       "params": params or {}}).encode()
    headers = {"Content-Type": "application/json",
               "Origin": "http://" + HOST,
               "Accept": "application/json, text/event-stream"}
    if _sid:
        headers["Mcp-Session-Id"] = _sid

    req = urllib.request.Request(URL, body, headers)
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            if not _sid:
                _sid = r.headers.get("Mcp-Session-Id")
            raw = r.read().decode("utf-8")
    except urllib.error.URLError as e:
        raise NotRunning(_explain() + "\n\n  (%s)" % e.reason)

    # Streamable HTTP は SSE で返すこともあるので data: 行を拾う
    if raw.startswith("event:") or raw.startswith("data:"):
        raw = "".join(l[5:] for l in raw.splitlines() if l.startswith("data:"))

    d = json.loads(raw)
    if "error" in d:
        raise SystemExit("MCP の誤り: " + json.dumps(d["error"], ensure_ascii=False))
    return d["result"]


def start():
    """繋いで初期化する。以後のやりとりに要るセッション ID もここで受け取る。"""
    return rpc("initialize", {"protocolVersion": "2025-06-18", "capabilities": {},
                              "clientInfo": {"name": "mcp_client.py", "version": "1"}})


def call(name, args=None):
    """道具を 1 つ呼ぶ。(本文, 生の返り) を返す。"""
    r = rpc("tools/call", {"name": name, "arguments": args or {}})
    out = []
    for c in r.get("content", []):
        if c.get("type") == "text":
            out.append(c["text"])
        else:
            out.append("<%s %d文字>" % (c.get("type"), len(c.get("data", ""))))
    return "\n".join(out), r


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    try:
        start()
        if len(sys.argv) < 2:
            for t in rpc("tools/list")["tools"]:
                head = (t.get("description") or "").splitlines()[0]
                print("  %-22s %s" % (t["name"], head[:80]))
            return 0
        args = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
        print(call(sys.argv[1], args)[0])
        return 0
    except NotRunning as e:
        print(e, file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())

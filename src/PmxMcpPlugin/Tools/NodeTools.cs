using System.Collections.Generic;
using PEPlugin.Pmx;

namespace PmxMcp
{
    /// <summary>
    /// Display frames, which PMX calls nodes.
    ///
    /// A PMX file always starts with two frames, Root and Expression, but PMX Editor does
    /// not keep them in the node list: they sit on their own handles and are written back
    /// at save time. So the list here holds only the ordinary frames, and the two fixed
    /// ones are reached with which="root" or which="expression".
    ///
    /// Frame edits do not take through the Node update channel on this build; they only
    /// land when the whole model is refreshed, which is why these tools ask for All.
    /// </summary>
    internal static class NodeTools
    {
        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "list_nodes",
                "Lists the ordinary display frames with the bones and morphs they hold. The "
                    + "two fixed frames a PMX must have, Root and Expression, are not part of "
                    + "that list and come back separately as root and expression.",
                Schema.Object(Json.Obj(
                    "offset", Schema.Int("First frame index to return (default 0)"),
                    "limit", Schema.Int("How many frames to return (default 200, max 1000)"))),
                true,
                delegate(Dictionary<string, object> args) { return ListNodes(editor, args); });

            registry.Add(
                "get_node",
                "Full detail of one display frame: every item, in order, as a bone or morph "
                    + "index. Pass which=root or which=expression for the two fixed frames.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Frame index"),
                    "name", Schema.Str("Japanese frame name"),
                    "which", Schema.Str("Instead of an index: root or expression, for the two "
                        + "fixed frames PMX Editor keeps outside the list"))),
                true,
                delegate(Dictionary<string, object> args) { return GetNode(editor, args); });

            registry.Add(
                "set_node",
                "Edits one display frame. Passing items replaces the whole contents; each item "
                    + "is {\"bone\": index} or {\"morph\": index}, or the same with _name.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Frame index"),
                    "name", Schema.Str("Japanese name used to find the frame"),
                    "which", Schema.Str("Instead of an index: root or expression"),
                    "new_name", Schema.Str("New Japanese name"),
                    "new_name_en", Schema.Str("New English name"),
                    "items", Schema.ObjArray(
                        "Replacement contents, in order. Each entry is {\"bone\": index}, "
                        + "{\"bone_name\": name}, {\"morph\": index} or {\"morph_name\": name}."))),
                false,
                delegate(Dictionary<string, object> args) { return SetNode(editor, args); });

            registry.Add(
                "add_node",
                "Adds a display frame and returns its index.",
                Schema.Object(Json.Obj(
                    "new_name", Schema.Str("Japanese name"),
                    "new_name_en", Schema.Str("English name"),
                    "items", Schema.ObjArray("Contents, in the same shape as set_node"))),
                false,
                delegate(Dictionary<string, object> args) { return AddNode(editor, args); });

            registry.Add(
                "delete_node",
                "Deletes one ordinary display frame. The two fixed frames are refused.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Frame index"),
                    "name", Schema.Str("Japanese frame name"))),
                false,
                delegate(Dictionary<string, object> args) { return DeleteNode(editor, args); });
        }

        /// <summary>
        /// The fixed frame named by a "which" argument, or null when the caller meant an
        /// ordinary one.  PMX Editor holds these two outside the node list.
        /// </summary>
        private static IPXNode Fixed(IPXPmx pmx, Dictionary<string, object> args)
        {
            if (!Json.Has(args, "which")) return null;
            string which = Json.Str(args, "which", "");
            if (string.Equals(which, "root", System.StringComparison.OrdinalIgnoreCase))
            {
                return pmx.RootNode;
            }
            if (string.Equals(which, "expression", System.StringComparison.OrdinalIgnoreCase))
            {
                return pmx.ExpressionNode;
            }
            throw new McpToolException("which must be root or expression");
        }

        private static object NodeRow(IPXPmx pmx, int i, IPXNode n)
        {
            List<object> items = new List<object>();
            foreach (IPXNodeItem item in n.Items)
            {
                if (item.IsBone && item.BoneItem != null)
                {
                    items.Add(Json.Obj(
                        "kind", "bone",
                        "index", PmxUtil.IndexOf(pmx.Bone, item.BoneItem.Bone),
                        "name", item.BoneItem.Bone == null ? null : item.BoneItem.Bone.Name));
                }
                else if (item.IsMorph && item.MorphItem != null)
                {
                    items.Add(Json.Obj(
                        "kind", "morph",
                        "index", PmxUtil.IndexOf(pmx.Morph, item.MorphItem.Morph),
                        "name", item.MorphItem.Morph == null ? null : item.MorphItem.Morph.Name));
                }
                else
                {
                    items.Add(Json.Obj("kind", "empty"));
                }
            }
            return Json.Obj(
                "index", i,
                "name", n.Name,
                "nameEn", n.NameE,
                "itemCount", items.Count,
                "items", items.ToArray());
        }

        private static object ListNodes(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                int offset, limit;
                PmxUtil.Page(args, pmx.Node.Count, 200, out offset, out limit);

                List<object> rows = new List<object>();
                for (int i = offset; i < offset + limit; i++)
                {
                    rows.Add(NodeRow(pmx, i, pmx.Node[i]));
                }
                return Json.Obj(
                    "total", pmx.Node.Count,
                    "offset", offset,
                    "count", rows.Count,
                    "nodes", rows.ToArray(),
                    // The two fixed frames are not in the list; hand them back alongside
                    // so a caller can see the whole picture in one call.
                    "root", pmx.RootNode == null ? null : NodeRow(pmx, -1, pmx.RootNode),
                    "expression", pmx.ExpressionNode == null ? null : NodeRow(pmx, -1, pmx.ExpressionNode));
            });
        }

        private static object GetNode(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                IPXNode fixedFrame = Fixed(pmx, args);
                if (fixedFrame != null) return NodeRow(pmx, -1, fixedFrame);

                int i = PmxRef.ResolveNode(pmx, args);
                return NodeRow(pmx, i, pmx.Node[i]);
            });
        }

        /// <summary>Rebuilds a frame's contents from the "items" argument, if it was given.</summary>
        private static void ApplyItems(IPXPmx pmx, IPXNode n, Dictionary<string, object> args)
        {
            object[] items = Json.Arr(args, "items");
            if (items == null) return;

            n.Items.Clear();
            foreach (object raw in items)
            {
                Dictionary<string, object> item = raw as Dictionary<string, object>;
                if (item == null)
                {
                    throw new McpToolException("each entry of items must be an object");
                }

                bool given;
                IPXBone bone = PmxRef.BoneArg(pmx, item, "bone", out given);
                if (given)
                {
                    if (bone == null) throw new McpToolException("a bone item needs a real bone");
                    n.Items.Add(PmxRef.Builder.BoneNodeItem(bone));
                    continue;
                }

                int mi = -1;
                if (Json.Has(item, "morph"))
                {
                    mi = Json.Int(item, "morph", -1);
                }
                else if (Json.Has(item, "morph_name"))
                {
                    string name = Json.Str(item, "morph_name", "");
                    for (int k = 0; k < pmx.Morph.Count; k++)
                    {
                        if (pmx.Morph[k].Name == name) { mi = k; break; }
                    }
                    if (mi < 0) throw new McpToolException("no morph named " + name);
                }
                else
                {
                    throw new McpToolException(
                        "each entry of items needs one of bone, bone_name, morph or morph_name");
                }

                if (mi < 0 || mi >= pmx.Morph.Count)
                {
                    throw new McpToolException("morph index " + mi + " is out of range");
                }
                n.Items.Add(PmxRef.Builder.MorphNodeItem(pmx.Morph[mi]));
            }
        }

        private static object SetNode(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.All;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Node;

                IPXNode fixedFrame = Fixed(pmx, args);
                int i = fixedFrame == null ? PmxRef.ResolveNode(pmx, args) : -1;
                IPXNode n = fixedFrame != null ? fixedFrame : pmx.Node[i];

                if (Json.Has(args, "new_name")) n.Name = Json.Str(args, "new_name", n.Name);
                if (Json.Has(args, "new_name_en")) n.NameE = Json.Str(args, "new_name_en", n.NameE);
                ApplyItems(pmx, n, args);

                return NodeRow(pmx, i, n);
            });
        }

        private static object AddNode(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.All;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Node;

                IPXNode n = PmxRef.Builder.Node();
                n.Name = Json.Str(args, "new_name", "新規枠");
                n.NameE = Json.Str(args, "new_name_en", "");
                ApplyItems(pmx, n, args);
                pmx.Node.Add(n);

                // 足したときは番号を渡さない(向こうの一覧はまだ増えていない)
                return NodeRow(pmx, pmx.Node.Count - 1, n);
            });
        }

        private static object DeleteNode(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.All;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Node;

                if (Json.Has(args, "which"))
                {
                    throw new McpToolException(
                        "the Root and Expression frames cannot be deleted; a PMX needs both to load");
                }
                int i = PmxRef.ResolveNode(pmx, args);
                string name = pmx.Node[i].Name;
                pmx.Node.RemoveAt(i);
                return Json.Obj("deleted", name, "index", i, "remaining", pmx.Node.Count);
            });
        }
    }
}

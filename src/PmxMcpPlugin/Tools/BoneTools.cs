using System.Collections.Generic;
using PEPlugin.Pmx;
using PEPlugin.SDX;

namespace PmxMcp
{
    internal static class BoneTools
    {
        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "list_bones",
                "Lists bones with their index, names, parent and position. Paged, and filterable by name.",
                Schema.Object(Json.Obj(
                    "offset", Schema.Int("First bone index to return (default 0)"),
                    "limit", Schema.Int("How many bones to return (default 200, max 1000)"),
                    "name_contains", Schema.Str("Only bones whose Japanese or English name contains this text"))),
                true,
                delegate(Dictionary<string, object> args) { return ListBones(editor, args); });

            registry.Add(
                "get_bone",
                "Full detail of one bone, including flags, append parent and IK links. Identify it by index or name.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Bone index"),
                    "name", Schema.Str("Japanese bone name"))),
                true,
                delegate(Dictionary<string, object> args) { return GetBone(editor, args); });

            registry.Add(
                "set_bone",
                "Edits one bone. Identify it by index or name; only the fields you pass are changed.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Bone index"),
                    "name", Schema.Str("Japanese bone name used to find the bone"),
                    "new_name", Schema.Str("New Japanese name"),
                    "new_name_en", Schema.Str("New English name"),
                    "position", Schema.NumArray("New bone position [x, y, z]", 3),
                    "visible", Schema.Bool("Show the bone in the editor"),
                    "controllable", Schema.Bool("Allow manual operation"),
                    "rotatable", Schema.Bool("Rotation flag"),
                    "translatable", Schema.Bool("Translation flag"))),
                false,
                delegate(Dictionary<string, object> args) { return SetBone(editor, args); });
        }

        private static object ListBones(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                string filter = Json.Str(args, "name_contains", null);
                List<object> rows = new List<object>();

                List<int> matched = new List<int>();
                for (int i = 0; i < pmx.Bone.Count; i++)
                {
                    IPXBone bone = pmx.Bone[i];
                    if (filter == null || PmxUtil.Matches(bone.Name, filter) || PmxUtil.Matches(bone.NameE, filter))
                    {
                        matched.Add(i);
                    }
                }

                int offset, limit;
                PmxUtil.Page(args, matched.Count, 200, out offset, out limit);

                for (int n = offset; n < offset + limit; n++)
                {
                    int i = matched[n];
                    IPXBone bone = pmx.Bone[i];
                    rows.Add(Json.Obj(
                        "index", i,
                        "name", bone.Name,
                        "nameEn", bone.NameE,
                        "parentIndex", PmxUtil.IndexOf(pmx.Bone, bone.Parent),
                        "parentName", bone.Parent == null ? null : bone.Parent.Name,
                        "position", PmxUtil.Vec3(bone.Position),
                        "level", bone.Level,
                        "visible", bone.Visible,
                        "isIK", bone.IsIK));
                }

                return Json.Obj(
                    "total", pmx.Bone.Count,
                    "matched", matched.Count,
                    "offset", offset,
                    "count", rows.Count,
                    "bones", rows.ToArray());
            });
        }

        private static object GetBone(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                int index = PmxUtil.ResolveBone(pmx, args);
                IPXBone bone = pmx.Bone[index];

                Dictionary<string, object> result = Json.Obj(
                    "index", index,
                    "name", bone.Name,
                    "nameEn", bone.NameE,
                    "position", PmxUtil.Vec3(bone.Position),
                    "parentIndex", PmxUtil.IndexOf(pmx.Bone, bone.Parent),
                    "parentName", bone.Parent == null ? null : bone.Parent.Name,
                    "toBoneIndex", PmxUtil.IndexOf(pmx.Bone, bone.ToBone),
                    "toOffset", PmxUtil.Vec3(bone.ToOffset),
                    "level", bone.Level,
                    "flags", Json.Obj(
                        "rotatable", bone.IsRotation,
                        "translatable", bone.IsTranslation,
                        "visible", bone.Visible,
                        "controllable", bone.Controllable,
                        "isIK", bone.IsIK,
                        "appendRotation", bone.IsAppendRotation,
                        "appendTranslation", bone.IsAppendTranslation,
                        "appendLocal", bone.IsAppendLocal,
                        "fixAxis", bone.IsFixAxis,
                        "localFrame", bone.IsLocalFrame,
                        "afterPhysics", bone.IsAfterPhysics,
                        "external", bone.IsExternal));

                if (bone.IsAppendRotation || bone.IsAppendTranslation)
                {
                    result["appendParentIndex"] = PmxUtil.IndexOf(pmx.Bone, bone.AppendParent);
                    result["appendRatio"] = bone.AppendRatio;
                }
                if (bone.IsFixAxis)
                {
                    result["fixAxisVector"] = PmxUtil.Vec3(bone.FixAxis);
                }
                if (bone.IsIK && bone.IK != null)
                {
                    List<object> links = new List<object>();
                    foreach (IPXIKLink link in bone.IK.Links)
                    {
                        links.Add(Json.Obj(
                            "boneIndex", PmxUtil.IndexOf(pmx.Bone, link.Bone),
                            "boneName", link.Bone == null ? null : link.Bone.Name,
                            "isLimit", link.IsLimit,
                            "low", PmxUtil.Vec3(link.Low),
                            "high", PmxUtil.Vec3(link.High)));
                    }
                    result["ik"] = Json.Obj(
                        "targetIndex", PmxUtil.IndexOf(pmx.Bone, bone.IK.Target),
                        "loopCount", bone.IK.LoopCount,
                        "angle", bone.IK.Angle,
                        "links", links.ToArray());
                }
                return result;
            });
        }

        private static object SetBone(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Bone;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Bone;

                int index = PmxUtil.ResolveBone(pmx, args);
                IPXBone bone = pmx.Bone[index];

                if (Json.Has(args, "new_name")) bone.Name = Json.Str(args, "new_name", bone.Name);
                if (Json.Has(args, "new_name_en")) bone.NameE = Json.Str(args, "new_name_en", bone.NameE);
                if (Json.Has(args, "visible")) bone.Visible = Json.Bool(args, "visible", bone.Visible);
                if (Json.Has(args, "controllable")) bone.Controllable = Json.Bool(args, "controllable", bone.Controllable);
                if (Json.Has(args, "rotatable")) bone.IsRotation = Json.Bool(args, "rotatable", bone.IsRotation);
                if (Json.Has(args, "translatable")) bone.IsTranslation = Json.Bool(args, "translatable", bone.IsTranslation);

                float[] position = Json.Floats(args, "position", 3);
                if (position != null)
                {
                    bone.Position = new V3(position[0], position[1], position[2]);
                }

                return Json.Obj(
                    "index", index,
                    "name", bone.Name,
                    "nameEn", bone.NameE,
                    "position", PmxUtil.Vec3(bone.Position),
                    "visible", bone.Visible,
                    "controllable", bone.Controllable);
            });
        }
    }
}

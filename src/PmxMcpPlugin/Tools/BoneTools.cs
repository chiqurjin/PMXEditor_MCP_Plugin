using System.Collections.Generic;
using PEPlugin.Pmx;
using PEPlugin.SDX;

namespace PmxMcp
{
    /// <summary>
    /// Bones, including the flags PMX calls display and operation limits.
    ///
    /// Two of those, the fixed axis and the local frame, do not change how the model
    /// deforms - the PMX specification is explicit about it. They constrain how the bone
    /// is handled in an editor. They are still worth reading and writing, because tools
    /// that offer manual posing need them.
    ///
    /// The local frame vectors are not on the IPXBone interface; see <see cref="PmxRef"/>.
    /// </summary>
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
                "Full detail of one bone: flags, deform level, append parent, fixed axis, local "
                    + "frame axes and IK links. Identify it by index or name.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Bone index"),
                    "name", Schema.Str("Japanese bone name"))),
                true,
                delegate(Dictionary<string, object> args) { return GetBone(editor, args); });

            registry.Add(
                "set_bone",
                "Edits one bone. Identify it by index or name; only the fields you pass are changed. "
                    + "Bone references accept either an index (-1 for none) or the matching _name field. "
                    + "fix_axis and local_frame only affect how the bone is handled in an editor, not "
                    + "how the model deforms.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Bone index"),
                    "name", Schema.Str("Japanese bone name used to find the bone"),
                    "new_name", Schema.Str("New Japanese name"),
                    "new_name_en", Schema.Str("New English name"),
                    "position", Schema.NumArray("New bone position [x, y, z]", 3),
                    "parent", Schema.Int("Parent bone index, or -1 for none"),
                    "parent_name", Schema.Str("Parent bone name"),
                    "to_bone", Schema.Int("Tip bone index; sets the tip to be a bone, -1 for none"),
                    "to_bone_name", Schema.Str("Tip bone name"),
                    "to_offset", Schema.NumArray("Tip as an offset [x, y, z]; clears the tip bone", 3),
                    "level", Schema.Int("Deform level (transform order)"),
                    "visible", Schema.Bool("Show the bone in the editor"),
                    "controllable", Schema.Bool("Allow manual operation"),
                    "rotatable", Schema.Bool("Rotation flag"),
                    "translatable", Schema.Bool("Translation flag"),
                    "after_physics", Schema.Bool("Deform after physics"),
                    "append_rotation", Schema.Bool("Take rotation from the append parent"),
                    "append_translation", Schema.Bool("Take translation from the append parent"),
                    "append_local", Schema.Bool("Take the append value in the parent's local frame"),
                    "append_parent", Schema.Int("Append parent bone index, or -1 for none"),
                    "append_parent_name", Schema.Str("Append parent bone name"),
                    "append_ratio", Schema.Num("Append ratio"),
                    "fix_axis", Schema.Bool("Restrict operation to a single axis"),
                    "fix_axis_vector", Schema.NumArray("The fixed axis direction [x, y, z]", 3),
                    "local_frame", Schema.Bool("Give the bone its own operation frame"),
                    "local_x", Schema.NumArray("Local frame X axis [x, y, z]", 3),
                    "local_z", Schema.NumArray("Local frame Z axis [x, y, z]", 3),
                    "external", Schema.Bool("Deform from a parent outside the model"),
                    "external_key", Schema.Int("External parent key"),
                    "is_ik", Schema.Bool("Make this bone an IK bone"))),
                false,
                delegate(Dictionary<string, object> args) { return SetBone(editor, args); });

            registry.Add(
                "set_bone_ik",
                "Edits the IK settings of one bone: its target, loop count, per-loop angle limit "
                    + "and the whole link chain. The angle is in radians, and PMX stores it four "
                    + "times larger than the raw value a PMD file holds. Passing links replaces "
                    + "the chain; each entry is {\"bone\": index or \"bone_name\": name} and may "
                    + "add {\"low\": [x,y,z], \"high\": [x,y,z]} in radians to limit that link.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Bone index of the IK bone"),
                    "name", Schema.Str("Japanese name of the IK bone"),
                    "target", Schema.Int("Target bone index (the tip that reaches for the IK bone)"),
                    "target_name", Schema.Str("Target bone name"),
                    "loop_count", Schema.Int("How many solver iterations"),
                    "angle", Schema.Num("Per-iteration angle limit, in radians"),
                    "links", Schema.ObjArray("Replacement link chain, tip first"))),
                false,
                delegate(Dictionary<string, object> args) { return SetBoneIk(editor, args); });

            registry.Add(
                "add_bone",
                "Adds a bone and returns its index. Everything not passed keeps the editor default.",
                Schema.Object(Json.Obj(
                    "new_name", Schema.Str("Japanese name"),
                    "new_name_en", Schema.Str("English name"),
                    "position", Schema.NumArray("Bone position [x, y, z]", 3),
                    "parent", Schema.Int("Parent bone index, or -1 for none"),
                    "parent_name", Schema.Str("Parent bone name"),
                    "level", Schema.Int("Deform level"),
                    "visible", Schema.Bool("Show the bone in the editor"),
                    "controllable", Schema.Bool("Allow manual operation"),
                    "rotatable", Schema.Bool("Rotation flag"),
                    "translatable", Schema.Bool("Translation flag"))),
                false,
                delegate(Dictionary<string, object> args) { return AddBone(editor, args); });

            registry.Add(
                "delete_bone",
                "Deletes one bone. Vertices weighted to it, and anything else that referenced it, "
                    + "are left pointing at nothing, so this is for bones you know are unused.",
                Schema.Object(Json.Obj(
                    "index", Schema.Int("Bone index"),
                    "name", Schema.Str("Japanese bone name"))),
                false,
                delegate(Dictionary<string, object> args) { return DeleteBone(editor, args); });
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

        private static object Detail(IPXPmx pmx, int index)
        {
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
                result["appendParentName"] = bone.AppendParent == null ? null : bone.AppendParent.Name;
                result["appendRatio"] = bone.AppendRatio;
            }
            if (bone.IsFixAxis)
            {
                result["fixAxisVector"] = PmxUtil.Vec3(bone.FixAxis);
            }
            if (bone.IsLocalFrame)
            {
                // Not on the interface, so it may be absent on some builds; report what
                // is there rather than pretending the axes do not exist.
                result["localX"] = PmxUtil.Vec3(PmxRef.LocalAxis(bone, "LocalX"));
                result["localY"] = PmxUtil.Vec3(PmxRef.LocalAxis(bone, "LocalY"));
                result["localZ"] = PmxUtil.Vec3(PmxRef.LocalAxis(bone, "LocalZ"));
            }
            if (bone.IsExternal)
            {
                result["externalKey"] = bone.ExternalKey;
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
                    "targetName", bone.IK.Target == null ? null : bone.IK.Target.Name,
                    "loopCount", bone.IK.LoopCount,
                    "angle", bone.IK.Angle,
                    "links", links.ToArray());
            }
            return result;
        }

        private static object GetBone(Editor editor, Dictionary<string, object> args)
        {
            return editor.Read<object>(delegate(IPXPmx pmx)
            {
                return Detail(pmx, PmxUtil.ResolveBone(pmx, args));
            });
        }

        /// <summary>Applies every writable bone field present in the arguments.</summary>
        private static void Apply(IPXPmx pmx, IPXBone bone, Dictionary<string, object> args)
        {
            if (Json.Has(args, "new_name")) bone.Name = Json.Str(args, "new_name", bone.Name);
            if (Json.Has(args, "new_name_en")) bone.NameE = Json.Str(args, "new_name_en", bone.NameE);
            if (Json.Has(args, "visible")) bone.Visible = Json.Bool(args, "visible", bone.Visible);
            if (Json.Has(args, "controllable")) bone.Controllable = Json.Bool(args, "controllable", bone.Controllable);
            if (Json.Has(args, "rotatable")) bone.IsRotation = Json.Bool(args, "rotatable", bone.IsRotation);
            if (Json.Has(args, "translatable")) bone.IsTranslation = Json.Bool(args, "translatable", bone.IsTranslation);
            if (Json.Has(args, "after_physics")) bone.IsAfterPhysics = Json.Bool(args, "after_physics", bone.IsAfterPhysics);
            if (Json.Has(args, "level")) bone.Level = Json.Int(args, "level", bone.Level);

            float[] a = Json.Floats(args, "position", 3);
            if (a != null) bone.Position = PmxRef.V3Of(a);

            bool given;
            IPXBone other = PmxRef.BoneArg(pmx, args, "parent", out given);
            if (given)
            {
                if (ReferenceEquals(other, bone))
                {
                    throw new McpToolException("a bone cannot be its own parent");
                }
                bone.Parent = other;
            }

            other = PmxRef.BoneArg(pmx, args, "to_bone", out given);
            if (given) bone.ToBone = other;

            a = Json.Floats(args, "to_offset", 3);
            if (a != null)
            {
                // The tip is either a bone or an offset, never both.
                bone.ToBone = null;
                bone.ToOffset = PmxRef.V3Of(a);
            }

            // Append (what MMD calls 付与): the flags first, so a parent set in the same
            // call is not dropped by a later flag change.
            if (Json.Has(args, "append_rotation"))
                bone.IsAppendRotation = Json.Bool(args, "append_rotation", bone.IsAppendRotation);
            if (Json.Has(args, "append_translation"))
                bone.IsAppendTranslation = Json.Bool(args, "append_translation", bone.IsAppendTranslation);
            if (Json.Has(args, "append_local"))
                bone.IsAppendLocal = Json.Bool(args, "append_local", bone.IsAppendLocal);
            other = PmxRef.BoneArg(pmx, args, "append_parent", out given);
            if (given) bone.AppendParent = other;
            if (Json.Has(args, "append_ratio"))
                bone.AppendRatio = Json.Flt(args, "append_ratio", bone.AppendRatio);

            if (Json.Has(args, "fix_axis")) bone.IsFixAxis = Json.Bool(args, "fix_axis", bone.IsFixAxis);
            a = Json.Floats(args, "fix_axis_vector", 3);
            if (a != null) bone.FixAxis = PmxRef.V3Of(a);

            if (Json.Has(args, "local_frame")) bone.IsLocalFrame = Json.Bool(args, "local_frame", bone.IsLocalFrame);

            // The frame is written as a whole: X and Z are what PMX stores, and Y is
            // derived from them. Passing only one keeps the other as it stands.
            float[] lx = Json.Floats(args, "local_x", 3);
            float[] lz = Json.Floats(args, "local_z", 3);
            if (lx != null || lz != null)
            {
                V3 ax = lx != null ? PmxRef.V3Of(lx) : PmxRef.LocalAxis(bone, "LocalX");
                V3 az = lz != null ? PmxRef.V3Of(lz) : PmxRef.LocalAxis(bone, "LocalZ");
                if (ax == null || az == null)
                {
                    throw new McpToolException("this PMX Editor build does not expose the local frame axes");
                }
                if (!PmxRef.SetLocalFrame(bone, ax, az))
                {
                    throw new McpToolException("the local frame could not be written on this build");
                }
            }

            if (Json.Has(args, "external")) bone.IsExternal = Json.Bool(args, "external", bone.IsExternal);
            if (Json.Has(args, "external_key")) bone.ExternalKey = Json.Int(args, "external_key", bone.ExternalKey);
            if (Json.Has(args, "is_ik")) bone.IsIK = Json.Bool(args, "is_ik", bone.IsIK);
        }

        private static object SetBone(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Bone;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Bone;

                int index = PmxUtil.ResolveBone(pmx, args);
                Apply(pmx, pmx.Bone[index], args);
                change.Index = index;
                return Detail(pmx, index);
            });
        }

        private static object SetBoneIk(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Bone;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Bone;

                int index = PmxUtil.ResolveBone(pmx, args);
                IPXBone bone = pmx.Bone[index];

                // Editing IK on a bone that is not an IK bone would silently do nothing,
                // so turn the flag on rather than leaving the caller puzzled.
                if (!bone.IsIK) bone.IsIK = true;
                if (bone.IK == null)
                {
                    throw new McpToolException("this bone has no IK block to edit");
                }

                bool given;
                IPXBone target = PmxRef.BoneArg(pmx, args, "target", out given);
                if (given) bone.IK.Target = target;

                if (Json.Has(args, "loop_count")) bone.IK.LoopCount = Json.Int(args, "loop_count", bone.IK.LoopCount);
                if (Json.Has(args, "angle")) bone.IK.Angle = Json.Flt(args, "angle", bone.IK.Angle);

                object[] links = Json.Arr(args, "links");
                if (links != null)
                {
                    bone.IK.Links.Clear();
                    foreach (object raw in links)
                    {
                        Dictionary<string, object> item = raw as Dictionary<string, object>;
                        if (item == null) throw new McpToolException("each entry of links must be an object");

                        bool has;
                        IPXBone linkBone = PmxRef.BoneArg(pmx, item, "bone", out has);
                        if (!has || linkBone == null)
                        {
                            throw new McpToolException("each entry of links needs bone or bone_name");
                        }

                        float[] low = Json.Floats(item, "low", 3);
                        float[] high = Json.Floats(item, "high", 3);
                        IPXIKLink link;
                        if (low != null && high != null)
                        {
                            link = PmxRef.Builder.IKLink(linkBone, PmxRef.V3Of(low), PmxRef.V3Of(high));
                            link.IsLimit = true;
                        }
                        else
                        {
                            link = PmxRef.Builder.IKLink(linkBone);
                            link.IsLimit = false;
                        }
                        bone.IK.Links.Add(link);
                    }
                }

                change.Index = index;
                return Detail(pmx, index);
            });
        }

        private static object AddBone(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Bone;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Bone;

                IPXBone bone = PmxRef.Builder.Bone();
                bone.Name = Json.Str(args, "new_name", "新規ボーン");
                bone.NameE = Json.Str(args, "new_name_en", "");
                Apply(pmx, bone, args);
                pmx.Bone.Add(bone);

                // 足したときは番号を渡さない。向こうの一覧はまだ増えていないので、
                // 新しい番号は範囲の外になる(既定の -1 で全体を作り直させる)
                return Detail(pmx, pmx.Bone.Count - 1);
            });
        }

        private static object DeleteBone(Editor editor, Dictionary<string, object> args)
        {
            return editor.Edit<object>(delegate(IPXPmx pmx, Editor.Change change)
            {
                change.Target = PmxUpdateObject.Bone;
                change.ListTarget = PEPlugin.Pmd.UpdateObject.Bone;

                int index = PmxUtil.ResolveBone(pmx, args);
                string name = pmx.Bone[index].Name;
                pmx.Bone.RemoveAt(index);
                return Json.Obj("deleted", name, "index", index, "remaining", pmx.Bone.Count);
            });
        }
    }
}

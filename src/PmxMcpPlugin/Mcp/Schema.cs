using System.Collections.Generic;

namespace PmxMcp
{
    /// <summary>Helpers for describing the JSON Schema of tool arguments.</summary>
    internal static class Schema
    {
        public static Dictionary<string, object> Object(Dictionary<string, object> properties, params string[] required)
        {
            Dictionary<string, object> schema = Json.Obj(
                "type", "object",
                "properties", properties == null ? new Dictionary<string, object>() : properties);
            if (required != null && required.Length > 0)
            {
                schema["required"] = required;
            }
            return schema;
        }

        public static Dictionary<string, object> None()
        {
            return Object(new Dictionary<string, object>());
        }

        public static Dictionary<string, object> Str(string description)
        {
            return Json.Obj("type", "string", "description", description);
        }

        public static Dictionary<string, object> Int(string description)
        {
            return Json.Obj("type", "integer", "description", description);
        }

        public static Dictionary<string, object> Num(string description)
        {
            return Json.Obj("type", "number", "description", description);
        }

        public static Dictionary<string, object> Bool(string description)
        {
            return Json.Obj("type", "boolean", "description", description);
        }

        public static Dictionary<string, object> NumArray(string description, int length)
        {
            Dictionary<string, object> s = Json.Obj(
                "type", "array",
                "items", Json.Obj("type", "number"),
                "description", description);
            if (length > 0)
            {
                s["minItems"] = length;
                s["maxItems"] = length;
            }
            return s;
        }

        /// <summary>A value whose shape is described in words rather than by a type.</summary>
        public static Dictionary<string, object> Any(string description)
        {
            return Json.Obj("description", description);
        }

        public static Dictionary<string, object> BoolArray(string description, int length)
        {
            Dictionary<string, object> s = Json.Obj(
                "type", "array",
                "items", Json.Obj("type", "boolean"),
                "description", description);
            if (length > 0)
            {
                s["minItems"] = length;
                s["maxItems"] = length;
            }
            return s;
        }

        public static Dictionary<string, object> StrArray(string description)
        {
            return Json.Obj(
                "type", "array",
                "items", Json.Obj("type", "string"),
                "description", description);
        }

        public static Dictionary<string, object> ObjArray(string description)
        {
            return Json.Obj(
                "type", "array",
                "items", Json.Obj("type", "object"),
                "description", description);
        }

        public static Dictionary<string, object> IntArray(string description)
        {
            return Json.Obj(
                "type", "array",
                "items", Json.Obj("type", "integer"),
                "description", description);
        }
    }
}

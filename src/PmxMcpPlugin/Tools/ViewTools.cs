using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PEPlugin.SDX;
using PEPlugin.View;

namespace PmxMcp
{
    /// <summary>Screenshots and camera control for the PmxView window.</summary>
    internal static class ViewTools
    {
        public static void Register(ToolRegistry registry, Editor editor)
        {
            registry.Add(
                "capture_viewport",
                "Captures the PmxView window as a PNG image. Use it to see the model or to check the result of an edit.",
                Schema.Object(Json.Obj(
                    "max_width", Schema.Int("Downscale so the image is at most this wide (default 1024, 0 keeps full size)"))),
                true,
                delegate(Dictionary<string, object> args) { return Capture(editor, args); });

            registry.Add(
                "get_camera",
                "Reads the PmxView camera: eye position, target and up vector.",
                Schema.None(),
                true,
                delegate(Dictionary<string, object> args) { return GetCamera(editor); });

            registry.Add(
                "set_camera",
                "Moves the PmxView camera. Pass position and target as [x, y, z]; up defaults to the current up vector.",
                Schema.Object(Json.Obj(
                    "position", Schema.NumArray("Eye position [x, y, z]", 3),
                    "target", Schema.NumArray("Look-at point [x, y, z]", 3),
                    "up", Schema.NumArray("Up vector [x, y, z]", 3)),
                    "position", "target"),
                false,
                delegate(Dictionary<string, object> args) { return SetCamera(editor, args); });
        }

        private static object Capture(Editor editor, Dictionary<string, object> args)
        {
            int maxWidth = Json.Int(args, "max_width", 1024);

            return editor.Ui<object>(delegate
            {
                Bitmap captured = editor.Connector.View.PmxView.GetClientImage();
                if (captured == null)
                {
                    throw new McpToolException("PmxView returned no image; make sure the view window is open.");
                }

                Bitmap scaled = null;
                try
                {
                    Bitmap source = captured;
                    if (maxWidth > 0 && captured.Width > maxWidth)
                    {
                        int height = (int)Math.Round(captured.Height * (double)maxWidth / captured.Width);
                        if (height < 1) height = 1;
                        scaled = new Bitmap(captured, new Size(maxWidth, height));
                        source = scaled;
                    }

                    using (MemoryStream stream = new MemoryStream())
                    {
                        source.Save(stream, ImageFormat.Png);
                        return new ImagePayload(Convert.ToBase64String(stream.ToArray()), "image/png");
                    }
                }
                finally
                {
                    if (scaled != null) scaled.Dispose();
                    captured.Dispose();
                }
            });
        }

        private static object GetCamera(Editor editor)
        {
            return editor.Ui<object>(delegate
            {
                IPXPmxViewConnector view = editor.Connector.View.PmxView;
                return Json.Obj(
                    "position", PmxUtil.Vec3(view.CameraPosition),
                    "target", PmxUtil.Vec3(view.CameraTarget),
                    "up", PmxUtil.Vec3(view.CameraUpVector),
                    "rotateCenter", PmxUtil.Vec3(view.CameraRotateCenter));
            });
        }

        private static object SetCamera(Editor editor, Dictionary<string, object> args)
        {
            editor.RequireWrite();

            float[] position = Json.Floats(args, "position", 3);
            float[] target = Json.Floats(args, "target", 3);
            float[] up = Json.Floats(args, "up", 3);

            if (position == null || target == null)
            {
                throw new McpToolException("position and target are both required");
            }

            return editor.Ui<object>(delegate
            {
                IPXPmxViewConnector view = editor.Connector.View.PmxView;

                V3 upVector = up != null
                    ? new V3(up[0], up[1], up[2])
                    : new V3(view.CameraUpVector);

                // SetCameraView takes (target, position, upVector).
                view.SetCameraView(
                    new V3(target[0], target[1], target[2]),
                    new V3(position[0], position[1], position[2]),
                    upVector);
                view.UpdateView();

                return Json.Obj(
                    "position", PmxUtil.Vec3(view.CameraPosition),
                    "target", PmxUtil.Vec3(view.CameraTarget),
                    "up", PmxUtil.Vec3(view.CameraUpVector));
            });
        }
    }
}

﻿using System.Reflection;
using UnityEngine;

namespace Hollow_Knight_Platforming_Mod.Hitbox
{
    // Line drawing routine originally courtesy of Linusmartensson:
    // http://forum.unity3d.com/threads/71979-Drawing-lines-in-the-editor
    //
    // Rewritten to improve performance by Yossarian King / August 2013.
    //
    // Circle drawing functionality added by Cherno
    // http://answers.unity.com/answers/713428/view.html
    //
    // This version produces virtually identical results to the original (tested by drawing
    // one over the other and observing errors of one pixel or less), but for large numbers
    // of lines this version is more than four times faster than the original, and comes
    // within about 70% of the raw performance of Graphics.DrawTexture.
    //
    // Peak performance on my laptop is around 200,000 lines per second. The laptop is
    // Windows 7 64-bit, Intel Core2 Duo CPU 2.53GHz, 4G RAM, NVIDIA GeForce GT 220M.
    // Line width and anti-aliasing had negligible impact on performance.
    //
    // For a graph of benchmark results in a standalone Windows build, see this image:
    // https://app.box.com/s/hyuhi565dtolqdm97e00
    //
    // For a Google spreadsheet with full benchmark results, see:
    // https://docs.google.com/spreadsheet/ccc?key=0AvJlJlbRO26VdHhzeHNRMVF2UHZHMXFCTVFZN011V1E&usp=sharing

    public static class Drawing
    {
        private static Texture2D aaLineTex = null;
        private static Texture2D lineTex = null;
        private static Material blitMaterial = null;
        private static Material blendMaterial = null;
        private static Rect lineRect = new Rect(0, 0, 1, 1);

        // Draw a line in screen space, suitable for use from OnGUI calls from either
        // MonoBehaviour or EditorWindow. Note that this should only be called during repaint
        // events, when (Event.current.type == EventType.Repaint).
        //
        // Works by computing a matrix that transforms a unit square -- Rect(0,0,1,1) -- into
        // a scaled, rotated, and offset rectangle that corresponds to the line and its width.
        // A DrawTexture call used to draw a line texture into the transformed rectangle.
        //
        // More specifically:
        //      scale x by line length, y by line width
        //      rotate around z by the angle of the line
        //      offset by the position of the upper left corner of the target rectangle
        //
        // By working out the matrices and applying some trigonometry, the matrix calculation comes
        // out pretty simple. See https://app.box.com/s/xi08ow8o8ujymazg100j for a picture of my
        // notebook with the calculations.
        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width, bool antiAlias)
        {
            // Normally the static initializer does this, but to handle texture reinitialization
            // after editor play mode stops we need this check in the Editor.
#if UNITY_EDITOR
                     if (!lineTex)
                     {
                         Initialize();
                     }
#endif

            // Note that theta = atan2(dy, dx) is the angle we want to rotate by, but instead
            // of calculating the angle we just use the sine (dy/len) and cosine (dx/len).
            float dx = pointB.x - pointA.x;
            float dy = pointB.y - pointA.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);

            // Early out on tiny lines to avoid divide by zero.
            // Plus what's the point of drawing a line 1/1000th of a pixel long??
            if (len < 0.001f)
            {
                return;
            }

            // Pick texture and material (and tweak width) based on anti-alias setting.
            Texture2D tex;
            Material mat;
            if (antiAlias)
            {
                // Multiplying by three is fine for anti-aliasing width-1 lines, but make a wide "fringe"
                // for thicker lines, which may or may not be desirable.
                width = width * 3.0f;
                tex = aaLineTex;
                mat = blendMaterial;
            }
            else
            {
                tex = lineTex;
                mat = blitMaterial;
            }

            float wdx = width * dy / len;
            float wdy = width * dx / len;

            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.m00 = dx;
            matrix.m01 = -wdx;
            matrix.m03 = pointA.x + 0.5f * wdx;
            matrix.m10 = dy;
            matrix.m11 = wdy;
            matrix.m13 = pointA.y - 0.5f * wdy;

            // Use GL matrix and Graphics.DrawTexture rather than GUI.matrix and GUI.DrawTexture,
            // for better performance. (Setting GUI.matrix is slow, and GUI.DrawTexture is just a
            // wrapper on Graphics.DrawTexture.)
            GL.PushMatrix();
            GL.MultMatrix(matrix);

            Graphics.DrawTexture(lineRect, tex, lineRect, 0, 0, 0, 0, color, mat);
            // 启用抗锯齿时颜色似乎是半透明的，所以重复绘制一次使其更清晰
            if (antiAlias)
            {
                Graphics.DrawTexture(lineRect, tex, lineRect, 0, 0, 0, 0, color, mat);
            }

            GL.PopMatrix();
        }

        public static void DrawCircle(Vector2 center, int radius, Color color, float width, int segmentsPerQuarter)
        {
            DrawCircle(center, radius, color, width, false, segmentsPerQuarter);
        }

        public static void DrawCircle(Vector2 center, int radius, Color color, float width, bool antiAlias, int segmentsPerQuarter)
        {
            float rh = (float)radius * 0.551915024494f;

            Vector2 p1 = new Vector2(center.x, center.y - radius);
            Vector2 p1_tan_a = new Vector2(center.x - rh, center.y - radius);
            Vector2 p1_tan_b = new Vector2(center.x + rh, center.y - radius);

            Vector2 p2 = new Vector2(center.x + radius, center.y);
            Vector2 p2_tan_a = new Vector2(center.x + radius, center.y - rh);
            Vector2 p2_tan_b = new Vector2(center.x + radius, center.y + rh);

            Vector2 p3 = new Vector2(center.x, center.y + radius);
            Vector2 p3_tan_a = new Vector2(center.x - rh, center.y + radius);
            Vector2 p3_tan_b = new Vector2(center.x + rh, center.y + radius);

            Vector2 p4 = new Vector2(center.x - radius, center.y);
            Vector2 p4_tan_a = new Vector2(center.x - radius, center.y - rh);
            Vector2 p4_tan_b = new Vector2(center.x - radius, center.y + rh);

            DrawBezierLine(p1, p1_tan_b, p2, p2_tan_a, color, width, antiAlias, segmentsPerQuarter);
            DrawBezierLine(p2, p2_tan_b, p3, p3_tan_b, color, width, antiAlias, segmentsPerQuarter);
            DrawBezierLine(p3, p3_tan_a, p4, p4_tan_b, color, width, antiAlias, segmentsPerQuarter);
            DrawBezierLine(p4, p4_tan_a, p1, p1_tan_a, color, width, antiAlias, segmentsPerQuarter);
        }

        // Other than method name, DrawBezierLine is unchanged from Linusmartensson's original implementation.
        public static void DrawBezierLine(Vector2 start, Vector2 startTangent, Vector2 end, Vector2 endTangent, Color color, float width,
            bool antiAlias, int segments)
        {
            Vector2 lastV = CubeBezier(start, startTangent, end, endTangent, 0);
            for (int i = 1; i < segments + 1; ++i)
            {
                Vector2 v = CubeBezier(start, startTangent, end, endTangent, i / (float)segments);
                DrawLine(lastV, v, color, width, antiAlias);
                lastV = v;
            }
        }


        private static Vector2 CubeBezier(Vector2 s, Vector2 st, Vector2 e, Vector2 et, float t)
        {
            float rt = 1 - t;
            return rt * rt * rt * s + 3 * rt * rt * t * st + 3 * rt * t * t * et + t * t * t * e;
        }

        // This static initializer works for runtime, but apparently isn't called when
        // Editor play mode stops, so DrawLine will re-initialize if needed.
        static Drawing()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (lineTex == null)
            {
                lineTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                lineTex.SetPixel(0, 1, Color.white);
                lineTex.Apply();
            }

            if (aaLineTex == null)
            {
                // TODO: better anti-aliasing of wide lines with a larger texture? or use Graphics.DrawTexture with border settings
                aaLineTex = new Texture2D(1, 3, TextureFormat.ARGB32, false);
                aaLineTex.SetPixel(0, 0, new Color(1, 1, 1, 0));
                aaLineTex.SetPixel(0, 1, Color.white);
                aaLineTex.SetPixel(0, 2, new Color(1, 1, 1, 0));
                aaLineTex.Apply();
            }

            // GUI.blitMaterial and GUI.blendMaterial are used internally by GUI.DrawTexture,
            // depending on the alphaBlend parameter. Use reflection to "borrow" these references.
            blitMaterial = (Material)typeof(GUI).GetMethod("get_blitMaterial", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            blendMaterial = (Material)typeof(GUI).GetMethod("get_blendMaterial", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        }
    }
}
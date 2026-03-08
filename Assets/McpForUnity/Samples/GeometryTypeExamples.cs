using System.Collections.Generic;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityEngine;

namespace ModelContextProtocol.Samples
{
    public static class GeometryTypeExamples
    {
        #region Bounds Examples

        [McpServerTool("geometry_check_bounds_contains", Description = "Check if a point is inside bounds")]
        public static CallToolResult CheckBoundsContains(
            [McpArgument(Description = "Bounding box to check")] Bounds bounds,
            [McpArgument(Description = "Point to test")] Vector3 point)
        {
            bool contains = bounds.Contains(point);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Point {point} is {(contains ? "inside" : "outside")} bounds (center: {bounds.center}, size: {bounds.size})" }
                }
            };
        }

        [McpServerTool("geometry_expand_bounds", Description = "Expand bounds by a factor")]
        public static CallToolResult ExpandBounds(
            [McpArgument(Description = "Bounding box to expand")] Bounds bounds,
            [McpArgument(Description = "Expansion factor")] float factor)
        {
            bounds.Expand(factor);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Expanded bounds: center={bounds.center}, size={bounds.size}" }
                }
            };
        }

        #endregion

        #region Rect Examples

        [McpServerTool("geometry_check_rect_contains", Description = "Check if a point is inside a 2D rectangle")]
        public static CallToolResult CheckRectContains(
            [McpArgument(Description = "Rectangle to check")] Rect rect,
            [McpArgument(Description = "2D point to test")] Vector2 point)
        {
            bool contains = rect.Contains(point);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Point {point} is {(contains ? "inside" : "outside")} rect (x:{rect.x}, y:{rect.y}, w:{rect.width}, h:{rect.height})" }
                }
            };
        }

        [McpServerTool("geometry_create_rect_array", Description = "Create and return info about multiple rectangles")]
        public static CallToolResult CreateRectArray(
            [McpArgument(Description = "Array of rectangles [x,y,w,h, x,y,w,h, ...]", Required = true)] Rect[] rects)
        {
            var info = new List<string>();
            for (int i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                info.Add($"Rect[{i}]: pos=({r.x}, {r.y}), size=({r.width}, {r.height})");
            }
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Created {rects.Length} rectangles:\n{string.Join("\n", info)}" }
                }
            };
        }

        #endregion

        #region Ray Examples

        [McpServerTool("geometry_get_ray_point", Description = "Get a point along a ray at distance")]
        public static CallToolResult GetRayPoint(
            [McpArgument(Description = "Ray (origin + direction)")] Ray ray,
            [McpArgument(Description = "Distance along ray")] float distance)
        {
            Vector3 point = ray.GetPoint(distance);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Point at distance {distance}: {point}\nRay origin: {ray.origin}\nRay direction: {ray.direction}" }
                }
            };
        }

        [McpServerTool("geometry_raycast_test", Description = "Test raycast against a plane")]
        public static CallToolResult RaycastTest(
            [McpArgument(Description = "Ray to cast")] Ray ray,
            [McpArgument(Description = "Plane to test against")] Plane plane)
        {
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Ray hits plane at distance {enter}, point: {hitPoint}" }
                    }
                };
            }
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = "Ray does not intersect plane" }
                }
            };
        }

        #endregion

        #region Ray2D Examples

        [McpServerTool("geometry_2d_ray_point", Description = "Get a point along a 2D ray at distance")]
        public static CallToolResult GetRay2DPoint(
            [McpArgument(Description = "2D ray (origin + direction)")] Ray2D ray,
            [McpArgument(Description = "Distance along ray")] float distance)
        {
            Vector2 point = ray.GetPoint(distance);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"2D Point at distance {distance}: {point}\nRay origin: {ray.origin}\nRay direction: {ray.direction}" }
                }
            };
        }

        #endregion

        #region Plane Examples

        [McpServerTool("geometry_plane_distance", Description = "Get distance from point to plane")]
        public static CallToolResult GetPlaneDistance(
            [McpArgument(Description = "Plane (normal + distance)")] Plane plane,
            [McpArgument(Description = "Point to measure from")] Vector3 point)
        {
            float distance = plane.GetDistanceToPoint(point);
            bool sameSide = plane.GetSide(point);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Distance from point to plane: {distance}\nPoint is on {(sameSide ? "same" : "opposite")} side as normal" }
                }
            };
        }

        #endregion

        #region Color Examples

        [McpServerTool("geometry_create_color", Description = "Create and describe a color")]
        public static CallToolResult CreateColor(
            [McpArgument(Description = "RGBA color (0-1 range)")] Color color)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Color: R={color.r:F2}, G={color.g:F2}, B={color.b:F2}, A={color.a:F2}\nHex: #{ColorUtility.ToHtmlStringRGBA(color)}" }
                }
            };
        }

        [McpServerTool("geometry_lerp_colors", Description = "Lerp between two colors")]
        public static CallToolResult LerpColors(
            [McpArgument(Description = "Start color")] Color fromColor,
            [McpArgument(Description = "End color")] Color toColor,
            [McpArgument(Description = "Lerp factor (0-1)")] float t)
        {
            Color result = Color.Lerp(fromColor, toColor, t);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Lerped color at t={t}: R={result.r:F2}, G={result.g:F2}, B={result.b:F2}, A={result.a:F2}" }
                }
            };
        }

        [McpServerTool("geometry_color32_convert", Description = "Convert between Color and Color32")]
        public static CallToolResult Color32Convert(
            [McpArgument(Description = "Color32 (0-255 range)")] Color32 color32)
        {
            Color floatColor = color32;
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Color32: R={color32.r}, G={color32.g}, B={color32.b}, A={color32.a}\nAs Color (0-1): R={floatColor.r:F3}, G={floatColor.g:F3}, B={floatColor.b:F3}, A={floatColor.a:F3}" }
                }
            };
        }

        #endregion

        #region Integer Vector Examples

        [McpServerTool("geometry_grid_distance", Description = "Calculate Manhattan distance between two grid positions")]
        public static CallToolResult GridDistance(
            [McpArgument(Description = "Start grid position")] Vector2Int start,
            [McpArgument(Description = "End grid position")] Vector2Int end)
        {
            int manhattan = Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y);
            float euclidean = Vector2Int.Distance(start, end);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Grid positions: {start} -> {end}\nManhattan distance: {manhattan}\nEuclidean distance: {euclidean:F2}" }
                }
            };
        }

        [McpServerTool("geometry_3d_grid_bounds", Description = "Create a 3D integer bounds")]
        public static CallToolResult Grid3DBounds(
            [McpArgument(Description = "Integer bounds (position + size)")] BoundsInt bounds)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"BoundsInt:\nPosition: {bounds.position}\nSize: {bounds.size}\nMin: {bounds.min}\nMax: {bounds.max}" }
                }
            };
        }

        #endregion

        #region RectInt Examples

        [McpServerTool("geometry_check_rectint_contains", Description = "Check if a point is inside an integer rectangle")]
        public static CallToolResult CheckRectIntContains(
            [McpArgument(Description = "Integer rectangle")] RectInt rect,
            [McpArgument(Description = "Integer point to test")] Vector2Int point)
        {
            bool contains = rect.Contains(point);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Point {point} is {(contains ? "inside" : "outside")} RectInt (x:{rect.x}, y:{rect.y}, w:{rect.width}, h:{rect.height})" }
                }
            };
        }

        #endregion

        #region RectOffset Examples

        [McpServerTool("geometry_apply_rect_offset", Description = "Apply padding offset to a rect")]
        public static CallToolResult ApplyRectOffset(
            [McpArgument(Description = "Original rectangle")] Rect rect,
            [McpArgument(Description = "Padding offset (left, right, top, bottom)")] RectOffset offset)
        {
            Rect padded = offset.Add(rect);
            Rect removed = offset.Remove(rect);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Original: {rect}\nWith padding added: {padded}\nWith padding removed: {removed}" }
                }
            };
        }

        #endregion

        #region Vector Array Examples

        [McpServerTool("geometry_bounds_array", Description = "Process array of bounds")]
        public static CallToolResult ProcessBoundsArray(
            [McpArgument(Description = "Array of bounds [cx,cy,cz,sx,sy,sz, ...]", Required = true)] Bounds[] boundsArray)
        {
            var info = new List<string>();
            Bounds combined = new Bounds();

            for (int i = 0; i < boundsArray.Length; i++)
            {
                var b = boundsArray[i];
                info.Add($"Bounds[{i}]: center={b.center}, size={b.size}");

                if (i == 0)
                    combined = b;
                else
                    combined.Encapsulate(b);
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Processed {boundsArray.Length} bounds:\n{string.Join("\n", info)}\n\nCombined bounds: center={combined.center}, size={combined.size}" }
                }
            };
        }

        [McpServerTool("geometry_color_palette", Description = "Create a color palette from array")]
        public static CallToolResult CreateColorPalette(
            [McpArgument(Description = "Array of colors [r,g,b,a, ...]", Required = true)] Color[] colors)
        {
            var info = new List<string>();
            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                info.Add($"Color[{i}]: #{ColorUtility.ToHtmlStringRGBA(c)}");
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Created palette with {colors.Length} colors:\n{string.Join("\n", info)}" }
                }
            };
        }

        #endregion

        #region Matrix4x4 Examples

        [McpServerTool("geometry_matrix_identity", Description = "Get identity matrix")]
        public static CallToolResult GetIdentityMatrix()
        {
            Matrix4x4 identity = Matrix4x4.identity;
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Identity Matrix:\n{FormatMatrix(identity)}" }
                }
            };
        }

        [McpServerTool("geometry_transform_point", Description = "Transform a point by a matrix")]
        public static CallToolResult TransformPoint(
            [McpArgument(Description = "Transformation matrix (16 values, column-major)")] Matrix4x4 matrix,
            [McpArgument(Description = "Point to transform")] Vector3 point)
        {
            Vector3 transformed = matrix.MultiplyPoint(point);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Original point: {point}\nTransformed point: {transformed}" }
                }
            };
        }

        private static string FormatMatrix(Matrix4x4 m)
        {
            return $"[{m.m00:F2}, {m.m01:F2}, {m.m02:F2}, {m.m03:F2}]\n" +
                   $"[{m.m10:F2}, {m.m11:F2}, {m.m12:F2}, {m.m13:F2}]\n" +
                   $"[{m.m20:F2}, {m.m21:F2}, {m.m22:F2}, {m.m23:F2}]\n" +
                   $"[{m.m30:F2}, {m.m31:F2}, {m.m32:F2}, {m.m33:F2}]";
        }

        #endregion
    }
}

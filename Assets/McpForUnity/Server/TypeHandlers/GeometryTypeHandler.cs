using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server.TypeHandlers
{
    internal static class GeometryTypeHandler
    {
        public static bool IsGeometryType(Type type)
        {
            return GeometryTypeDefinitions.IsGeometryType(type);
        }

        public static bool IsGeometryArrayType(Type type)
        {
            return GeometryTypeDefinitions.IsGeometryArrayType(type);
        }

        public static Type GetArrayElementType(Type arrayType)
        {
            return GeometryTypeDefinitions.GetArrayElementType(arrayType);
        }

        public static GeometryTypeDefinition GetDefinition(Type type)
        {
            return GeometryTypeDefinitions.GetDefinition(type);
        }

        #region Schema Generation

        public static void AddGeometryProperties(
            JObject properties,
            JArray required,
            string paramName,
            string paramDesc,
            Type type,
            bool isRequired,
            object defaultValue)
        {
            var definition = GeometryTypeDefinitions.GetDefinition(type);
            if (definition == null) return;

            var defaultValues = ExtractGeometryDefaults(defaultValue, type, definition);

            foreach (var group in definition.ComponentGroups)
            {
                foreach (var component in group.Components)
                {
                    string propName = $"{paramName}_{group.Prefix}{component.Name}";
                    string description = string.IsNullOrEmpty(paramDesc)
                        ? component.Description
                        : $"{paramDesc} ({component.Description})";

                    var propSchema = new JObject
                    {
                        ["type"] = component.JsonType,
                        ["description"] = description
                    };

                    if (defaultValues != null && defaultValues.TryGetValue($"{group.Prefix}{component.Name}", out var defaultVal))
                    {
                        propSchema["default"] = JToken.FromObject(defaultVal);
                    }

                    properties[propName] = propSchema;

                    if (isRequired)
                    {
                        required.Add(propName);
                    }
                }
            }
        }

        public static void AddGeometryArraySchema(
            JObject properties,
            JArray required,
            string paramName,
            string paramDesc,
            Type type,
            bool isRequired)
        {
            var elementType = GeometryTypeDefinitions.GetArrayElementType(type);
            var definition = GeometryTypeDefinitions.GetDefinition(elementType);
            if (definition == null) return;

            string itemType = definition.IsIntegerType ? "integer" : "number";

            var propSchema = new JObject
            {
                ["type"] = "array",
                ["items"] = new JObject { ["type"] = itemType },
                ["description"] = string.IsNullOrEmpty(paramDesc)
                    ? $"Flat array of {elementType.Name} values [{definition.ArrayFormatHint}]"
                    : paramDesc
            };

            properties[paramName] = propSchema;

            if (isRequired)
            {
                required.Add(paramName);
            }
        }

        private static Dictionary<string, object> ExtractGeometryDefaults(object defaultValue, Type type, GeometryTypeDefinition definition)
        {
            if (defaultValue == null || defaultValue == DBNull.Value)
                return null;

            try
            {
                var defaults = new Dictionary<string, object>();
                ExtractDefaultsFromValue(defaultValue, type, definition, defaults, "");
                return defaults.Count > 0 ? defaults : null;
            }
            catch
            {
                return null;
            }
        }

        private static void ExtractDefaultsFromValue(object value, Type type, GeometryTypeDefinition definition, Dictionary<string, object> defaults, string prefix)
        {
            if (type == typeof(Vector2) && value is Vector2 v2)
            {
                defaults[prefix + "x"] = v2.x;
                defaults[prefix + "y"] = v2.y;
            }
            else if (type == typeof(Vector3) && value is Vector3 v3)
            {
                defaults[prefix + "x"] = v3.x;
                defaults[prefix + "y"] = v3.y;
                defaults[prefix + "z"] = v3.z;
            }
            else if (type == typeof(Vector4) && value is Vector4 v4)
            {
                defaults[prefix + "x"] = v4.x;
                defaults[prefix + "y"] = v4.y;
                defaults[prefix + "z"] = v4.z;
                defaults[prefix + "w"] = v4.w;
            }
            else if (type == typeof(Quaternion) && value is Quaternion q)
            {
                defaults[prefix + "x"] = q.x;
                defaults[prefix + "y"] = q.y;
                defaults[prefix + "z"] = q.z;
                defaults[prefix + "w"] = q.w;
            }
            else if (type == typeof(Vector2Int) && value is Vector2Int v2i)
            {
                defaults[prefix + "x"] = v2i.x;
                defaults[prefix + "y"] = v2i.y;
            }
            else if (type == typeof(Vector3Int) && value is Vector3Int v3i)
            {
                defaults[prefix + "x"] = v3i.x;
                defaults[prefix + "y"] = v3i.y;
                defaults[prefix + "z"] = v3i.z;
            }
            else if (type == typeof(Bounds) && value is Bounds bounds)
            {
                defaults[prefix + "center_x"] = bounds.center.x;
                defaults[prefix + "center_y"] = bounds.center.y;
                defaults[prefix + "center_z"] = bounds.center.z;
                defaults[prefix + "size_x"] = bounds.size.x;
                defaults[prefix + "size_y"] = bounds.size.y;
                defaults[prefix + "size_z"] = bounds.size.z;
            }
            else if (type == typeof(BoundsInt) && value is BoundsInt boundsInt)
            {
                defaults[prefix + "position_x"] = boundsInt.position.x;
                defaults[prefix + "position_y"] = boundsInt.position.y;
                defaults[prefix + "position_z"] = boundsInt.position.z;
                defaults[prefix + "size_x"] = boundsInt.size.x;
                defaults[prefix + "size_y"] = boundsInt.size.y;
                defaults[prefix + "size_z"] = boundsInt.size.z;
            }
            else if (type == typeof(Rect) && value is Rect rect)
            {
                defaults[prefix + "x"] = rect.x;
                defaults[prefix + "y"] = rect.y;
                defaults[prefix + "width"] = rect.width;
                defaults[prefix + "height"] = rect.height;
            }
            else if (type == typeof(RectInt) && value is RectInt rectInt)
            {
                defaults[prefix + "x"] = rectInt.x;
                defaults[prefix + "y"] = rectInt.y;
                defaults[prefix + "width"] = rectInt.width;
                defaults[prefix + "height"] = rectInt.height;
            }
            else if (type == typeof(RectOffset) && value is RectOffset rectOffset)
            {
                defaults[prefix + "left"] = rectOffset.left;
                defaults[prefix + "right"] = rectOffset.right;
                defaults[prefix + "top"] = rectOffset.top;
                defaults[prefix + "bottom"] = rectOffset.bottom;
            }
            else if (type == typeof(Ray) && value is Ray ray)
            {
                defaults[prefix + "origin_x"] = ray.origin.x;
                defaults[prefix + "origin_y"] = ray.origin.y;
                defaults[prefix + "origin_z"] = ray.origin.z;
                defaults[prefix + "direction_x"] = ray.direction.x;
                defaults[prefix + "direction_y"] = ray.direction.y;
                defaults[prefix + "direction_z"] = ray.direction.z;
            }
            else if (type == typeof(Ray2D) && value is Ray2D ray2D)
            {
                defaults[prefix + "origin_x"] = ray2D.origin.x;
                defaults[prefix + "origin_y"] = ray2D.origin.y;
                defaults[prefix + "direction_x"] = ray2D.direction.x;
                defaults[prefix + "direction_y"] = ray2D.direction.y;
            }
            else if (type == typeof(Plane) && value is Plane plane)
            {
                defaults[prefix + "normal_x"] = plane.normal.x;
                defaults[prefix + "normal_y"] = plane.normal.y;
                defaults[prefix + "normal_z"] = plane.normal.z;
                defaults[prefix + "distance"] = plane.distance;
            }
            else if (type == typeof(Color) && value is Color color)
            {
                defaults[prefix + "r"] = color.r;
                defaults[prefix + "g"] = color.g;
                defaults[prefix + "b"] = color.b;
                defaults[prefix + "a"] = color.a;
            }
            else if (type == typeof(Color32) && value is Color32 color32)
            {
                defaults[prefix + "r"] = (int)color32.r;
                defaults[prefix + "g"] = (int)color32.g;
                defaults[prefix + "b"] = (int)color32.b;
                defaults[prefix + "a"] = (int)color32.a;
            }
            else if (type == typeof(Matrix4x4) && value is Matrix4x4 matrix)
            {
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        defaults[$"m{row}{col}"] = matrix[row, col];
                    }
                }
            }
        }

        #endregion

        #region Argument Parsing

        public static object ParseGeometryArgument(JObject args, string paramName, Type type, object defaultValue)
        {
            var definition = GeometryTypeDefinitions.GetDefinition(type);
            if (definition == null)
                return definition?.DefaultFactory?.Invoke();

            var defaults = defaultValue != null && defaultValue != DBNull.Value
                ? ExtractGeometryDefaults(defaultValue, type, definition)
                : null;

            return ParseGeometryValue(args, paramName, type, definition, defaults);
        }

        private static object ParseGeometryValue(JObject args, string paramName, Type type, GeometryTypeDefinition definition, Dictionary<string, object> defaults)
        {
            if (type == typeof(Vector2))
            {
                float x = GetComponentValue(args, paramName, "x", defaults, 0f);
                float y = GetComponentValue(args, paramName, "y", defaults, 0f);
                return new Vector2(x, y);
            }

            if (type == typeof(Vector3))
            {
                float x = GetComponentValue(args, paramName, "x", defaults, 0f);
                float y = GetComponentValue(args, paramName, "y", defaults, 0f);
                float z = GetComponentValue(args, paramName, "z", defaults, 0f);
                return new Vector3(x, y, z);
            }

            if (type == typeof(Vector4))
            {
                float x = GetComponentValue(args, paramName, "x", defaults, 0f);
                float y = GetComponentValue(args, paramName, "y", defaults, 0f);
                float z = GetComponentValue(args, paramName, "z", defaults, 0f);
                float w = GetComponentValue(args, paramName, "w", defaults, 0f);
                return new Vector4(x, y, z, w);
            }

            if (type == typeof(Quaternion))
            {
                float x = GetComponentValue(args, paramName, "x", defaults, 0f);
                float y = GetComponentValue(args, paramName, "y", defaults, 0f);
                float z = GetComponentValue(args, paramName, "z", defaults, 0f);
                float w = GetComponentValue(args, paramName, "w", defaults, 1f);
                return new Quaternion(x, y, z, w);
            }

            if (type == typeof(Vector2Int))
            {
                int x = GetIntComponentValue(args, paramName, "x", defaults, 0);
                int y = GetIntComponentValue(args, paramName, "y", defaults, 0);
                return new Vector2Int(x, y);
            }

            if (type == typeof(Vector3Int))
            {
                int x = GetIntComponentValue(args, paramName, "x", defaults, 0);
                int y = GetIntComponentValue(args, paramName, "y", defaults, 0);
                int z = GetIntComponentValue(args, paramName, "z", defaults, 0);
                return new Vector3Int(x, y, z);
            }

            if (type == typeof(Bounds))
            {
                float cx = GetComponentValue(args, paramName, "center_x", defaults, 0f);
                float cy = GetComponentValue(args, paramName, "center_y", defaults, 0f);
                float cz = GetComponentValue(args, paramName, "center_z", defaults, 0f);
                float sx = GetComponentValue(args, paramName, "size_x", defaults, 0f);
                float sy = GetComponentValue(args, paramName, "size_y", defaults, 0f);
                float sz = GetComponentValue(args, paramName, "size_z", defaults, 0f);
                return new Bounds(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz));
            }

            if (type == typeof(BoundsInt))
            {
                int px = GetIntComponentValue(args, paramName, "position_x", defaults, 0);
                int py = GetIntComponentValue(args, paramName, "position_y", defaults, 0);
                int pz = GetIntComponentValue(args, paramName, "position_z", defaults, 0);
                int sx = GetIntComponentValue(args, paramName, "size_x", defaults, 0);
                int sy = GetIntComponentValue(args, paramName, "size_y", defaults, 0);
                int sz = GetIntComponentValue(args, paramName, "size_z", defaults, 0);
                return new BoundsInt(new Vector3Int(px, py, pz), new Vector3Int(sx, sy, sz));
            }

            if (type == typeof(Rect))
            {
                float x = GetComponentValue(args, paramName, "x", defaults, 0f);
                float y = GetComponentValue(args, paramName, "y", defaults, 0f);
                float w = GetComponentValue(args, paramName, "width", defaults, 0f);
                float h = GetComponentValue(args, paramName, "height", defaults, 0f);
                return new Rect(x, y, w, h);
            }

            if (type == typeof(RectInt))
            {
                int x = GetIntComponentValue(args, paramName, "x", defaults, 0);
                int y = GetIntComponentValue(args, paramName, "y", defaults, 0);
                int w = GetIntComponentValue(args, paramName, "width", defaults, 0);
                int h = GetIntComponentValue(args, paramName, "height", defaults, 0);
                return new RectInt(x, y, w, h);
            }

            if (type == typeof(RectOffset))
            {
                int left = GetIntComponentValue(args, paramName, "left", defaults, 0);
                int right = GetIntComponentValue(args, paramName, "right", defaults, 0);
                int top = GetIntComponentValue(args, paramName, "top", defaults, 0);
                int bottom = GetIntComponentValue(args, paramName, "bottom", defaults, 0);
                return new RectOffset(left, right, top, bottom);
            }

            if (type == typeof(Ray))
            {
                float ox = GetComponentValue(args, paramName, "origin_x", defaults, 0f);
                float oy = GetComponentValue(args, paramName, "origin_y", defaults, 0f);
                float oz = GetComponentValue(args, paramName, "origin_z", defaults, 0f);
                float dx = GetComponentValue(args, paramName, "direction_x", defaults, 0f);
                float dy = GetComponentValue(args, paramName, "direction_y", defaults, 1f);
                float dz = GetComponentValue(args, paramName, "direction_z", defaults, 0f);
                return new Ray(new Vector3(ox, oy, oz), new Vector3(dx, dy, dz));
            }

            if (type == typeof(Ray2D))
            {
                float ox = GetComponentValue(args, paramName, "origin_x", defaults, 0f);
                float oy = GetComponentValue(args, paramName, "origin_y", defaults, 0f);
                float dx = GetComponentValue(args, paramName, "direction_x", defaults, 0f);
                float dy = GetComponentValue(args, paramName, "direction_y", defaults, 1f);
                return new Ray2D(new Vector2(ox, oy), new Vector2(dx, dy));
            }

            if (type == typeof(Plane))
            {
                float nx = GetComponentValue(args, paramName, "normal_x", defaults, 0f);
                float ny = GetComponentValue(args, paramName, "normal_y", defaults, 1f);
                float nz = GetComponentValue(args, paramName, "normal_z", defaults, 0f);
                float d = GetComponentValue(args, paramName, "distance", defaults, 0f);
                return new Plane(new Vector3(nx, ny, nz), d);
            }

            if (type == typeof(Color))
            {
                float r = GetComponentValue(args, paramName, "r", defaults, 0f);
                float g = GetComponentValue(args, paramName, "g", defaults, 0f);
                float b = GetComponentValue(args, paramName, "b", defaults, 0f);
                float a = GetComponentValue(args, paramName, "a", defaults, 1f);
                return new Color(r, g, b, a);
            }

            if (type == typeof(Color32))
            {
                byte r = (byte)GetIntComponentValue(args, paramName, "r", defaults, 0);
                byte g = (byte)GetIntComponentValue(args, paramName, "g", defaults, 0);
                byte b = (byte)GetIntComponentValue(args, paramName, "b", defaults, 0);
                byte a = (byte)GetIntComponentValue(args, paramName, "a", defaults, 255);
                return new Color32(r, g, b, a);
            }

            if (type == typeof(Matrix4x4))
            {
                var matrix = new Matrix4x4();
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        string key = $"m{row}{col}";
                        float defaultVal = (row == col) ? 1f : 0f;
                        matrix[row, col] = GetComponentValue(args, paramName, key, defaults, defaultVal);
                    }
                }
                return matrix;
            }

            return definition.DefaultFactory?.Invoke();
        }

        private static float GetComponentValue(JObject args, string paramName, string component, Dictionary<string, object> defaults, float fallbackDefault)
        {
            string key = $"{paramName}_{component}";

            if (args != null && args.TryGetValue(key, out var token))
            {
                return token.Value<float>();
            }

            if (defaults != null && defaults.TryGetValue(component, out var defaultVal))
            {
                return Convert.ToSingle(defaultVal);
            }

            return fallbackDefault;
        }

        private static int GetIntComponentValue(JObject args, string paramName, string component, Dictionary<string, object> defaults, int fallbackDefault)
        {
            string key = $"{paramName}_{component}";

            if (args != null && args.TryGetValue(key, out var token))
            {
                return token.Value<int>();
            }

            if (defaults != null && defaults.TryGetValue(component, out var defaultVal))
            {
                return Convert.ToInt32(defaultVal);
            }

            return fallbackDefault;
        }

        #endregion

        #region Array Parsing

        public static object ParseGeometryArrayArgument(JObject args, string paramName, Type targetType)
        {
            var elementType = GeometryTypeDefinitions.GetArrayElementType(targetType);
            var definition = GeometryTypeDefinitions.GetDefinition(elementType);
            if (definition == null)
                return null;

            if (args == null || !args.TryGetValue(paramName, out var token) || token == null)
            {
                return targetType.IsArray ? Array.CreateInstance(elementType, 0) : null;
            }

            JArray jArray = token as JArray;
            if (jArray == null)
            {
                return targetType.IsArray ? Array.CreateInstance(elementType, 0) : null;
            }

            if (definition.IsIntegerType)
            {
                int[] flatArray = jArray.ToObject<int[]>();
                return ParseIntegerGeometryArray(flatArray, elementType, targetType, definition);
            }
            else
            {
                float[] flatArray = jArray.ToObject<float[]>();
                return ParseFloatGeometryArray(flatArray, elementType, targetType, definition);
            }
        }

        private static object ParseFloatGeometryArray(float[] flatArray, Type elementType, Type targetType, GeometryTypeDefinition definition)
        {
            if (flatArray == null || flatArray.Length == 0)
            {
                return targetType.IsArray ? Array.CreateInstance(elementType, 0) : CreateEmptyList(elementType);
            }

            int componentCount = definition.TotalComponentCount;
            if (flatArray.Length % componentCount != 0)
            {
                throw new McpException(McpErrorCode.InvalidParams,
                    $"Geometry array has invalid length {flatArray.Length}. Must be divisible by {componentCount} for type {elementType.Name}.");
            }

            int count = flatArray.Length / componentCount;
            Array array = Array.CreateInstance(elementType, count);

            for (int i = 0; i < count; i++)
            {
                int offset = i * componentCount;
                object element = ParseFloatGeometryElement(flatArray, offset, elementType, definition);
                array.SetValue(element, i);
            }

            if (targetType == typeof(List<>).MakeGenericType(elementType))
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = Activator.CreateInstance(listType, array);
                return list;
            }

            return array;
        }

        private static object ParseFloatGeometryElement(float[] data, int offset, Type elementType, GeometryTypeDefinition definition)
        {
            if (elementType == typeof(Vector2))
                return new Vector2(data[offset], data[offset + 1]);

            if (elementType == typeof(Vector3))
                return new Vector3(data[offset], data[offset + 1], data[offset + 2]);

            if (elementType == typeof(Vector4))
                return new Vector4(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);

            if (elementType == typeof(Quaternion))
                return new Quaternion(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);

            if (elementType == typeof(Bounds))
                return new Bounds(
                    new Vector3(data[offset], data[offset + 1], data[offset + 2]),
                    new Vector3(data[offset + 3], data[offset + 4], data[offset + 5]));

            if (elementType == typeof(Rect))
                return new Rect(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);

            if (elementType == typeof(Ray))
                return new Ray(
                    new Vector3(data[offset], data[offset + 1], data[offset + 2]),
                    new Vector3(data[offset + 3], data[offset + 4], data[offset + 5]));

            if (elementType == typeof(Ray2D))
                return new Ray2D(
                    new Vector2(data[offset], data[offset + 1]),
                    new Vector2(data[offset + 2], data[offset + 3]));

            if (elementType == typeof(Plane))
                return new Plane(
                    new Vector3(data[offset], data[offset + 1], data[offset + 2]),
                    data[offset + 3]);

            if (elementType == typeof(Color))
                return new Color(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);

            if (elementType == typeof(Matrix4x4))
            {
                var matrix = new Matrix4x4();
                for (int i = 0; i < 16; i++)
                {
                    matrix[i / 4, i % 4] = data[offset + i];
                }
                return matrix;
            }

            return definition.DefaultFactory?.Invoke();
        }

        private static object ParseIntegerGeometryArray(int[] flatArray, Type elementType, Type targetType, GeometryTypeDefinition definition)
        {
            if (flatArray == null || flatArray.Length == 0)
            {
                return targetType.IsArray ? Array.CreateInstance(elementType, 0) : CreateEmptyList(elementType);
            }

            int componentCount = definition.TotalComponentCount;
            if (flatArray.Length % componentCount != 0)
            {
                throw new McpException(McpErrorCode.InvalidParams,
                    $"Geometry array has invalid length {flatArray.Length}. Must be divisible by {componentCount} for type {elementType.Name}.");
            }

            int count = flatArray.Length / componentCount;
            Array array = Array.CreateInstance(elementType, count);

            for (int i = 0; i < count; i++)
            {
                int offset = i * componentCount;
                object element = ParseIntegerGeometryElement(flatArray, offset, elementType, definition);
                array.SetValue(element, i);
            }

            if (targetType == typeof(List<>).MakeGenericType(elementType))
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = Activator.CreateInstance(listType, array);
                return list;
            }

            return array;
        }

        private static object ParseIntegerGeometryElement(int[] data, int offset, Type elementType, GeometryTypeDefinition definition)
        {
            if (elementType == typeof(Vector2Int))
                return new Vector2Int(data[offset], data[offset + 1]);

            if (elementType == typeof(Vector3Int))
                return new Vector3Int(data[offset], data[offset + 1], data[offset + 2]);

            if (elementType == typeof(BoundsInt))
                return new BoundsInt(
                    new Vector3Int(data[offset], data[offset + 1], data[offset + 2]),
                    new Vector3Int(data[offset + 3], data[offset + 4], data[offset + 5]));

            if (elementType == typeof(RectInt))
                return new RectInt(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);

            if (elementType == typeof(RectOffset))
                return new RectOffset(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);

            if (elementType == typeof(Color32))
                return new Color32((byte)data[offset], (byte)data[offset + 1], (byte)data[offset + 2], (byte)data[offset + 3]);

            return definition.DefaultFactory?.Invoke();
        }

        private static object CreateEmptyList(Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            return Activator.CreateInstance(listType);
        }

        #endregion
    }
}

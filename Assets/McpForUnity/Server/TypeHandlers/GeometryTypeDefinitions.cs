using System;
using System.Collections.Generic;

namespace ModelContextProtocol.Server.TypeHandlers
{
    internal enum GeometryTypeCategory
    {
        Vector,
        VectorInt,
        Shape,
        ShapeInt,
        Raycast,
        Plane,
        Color,
        ColorInt,
        Matrix
    }

    internal class GeometryComponent
    {
        public string Name { get; set; }
        public string JsonType { get; set; }
        public object DefaultValue { get; set; }
        public string Description { get; set; }

        public GeometryComponent(string name, string jsonType, object defaultValue, string description = null)
        {
            Name = name;
            JsonType = jsonType;
            DefaultValue = defaultValue;
            Description = description ?? name.ToUpper();
        }
    }

    internal class GeometryComponentGroup
    {
        public string Prefix { get; set; }
        public string Description { get; set; }
        public GeometryComponent[] Components { get; set; }

        public GeometryComponentGroup(string prefix, string description, GeometryComponent[] components)
        {
            Prefix = prefix;
            Description = description;
            Components = components;
        }
    }

    internal class GeometryTypeDefinition
    {
        public Type Type { get; set; }
        public GeometryTypeCategory Category { get; set; }
        public GeometryComponentGroup[] ComponentGroups { get; set; }
        public bool IsIntegerType { get; set; }
        public int TotalComponentCount { get; set; }
        public string ArrayFormatHint { get; set; }
        public Func<object> DefaultFactory { get; set; }

        public GeometryTypeDefinition(
            Type type,
            GeometryTypeCategory category,
            GeometryComponentGroup[] componentGroups,
            bool isIntegerType,
            string arrayFormatHint,
            Func<object> defaultFactory)
        {
            Type = type;
            Category = category;
            ComponentGroups = componentGroups;
            IsIntegerType = isIntegerType;
            ArrayFormatHint = arrayFormatHint;
            DefaultFactory = defaultFactory;

            TotalComponentCount = 0;
            foreach (var group in componentGroups)
            {
                TotalComponentCount += group.Components.Length;
            }
        }
    }

    internal static class GeometryTypeDefinitions
    {
        private static Dictionary<Type, GeometryTypeDefinition> _definitions;
        private static Dictionary<Type, Type> _arrayElementTypes;
        private static HashSet<Type> _geometryTypes;
        private static HashSet<Type> _geometryArrayTypes;

        public static IReadOnlyDictionary<Type, GeometryTypeDefinition> Definitions => _definitions;
        public static IReadOnlyCollection<Type> GeometryTypes => _geometryTypes;

        static GeometryTypeDefinitions()
        {
            InitializeDefinitions();
        }

        private static void InitializeDefinitions()
        {
            _definitions = new Dictionary<Type, GeometryTypeDefinition>();
            _arrayElementTypes = new Dictionary<Type, Type>();
            _geometryTypes = new HashSet<Type>();
            _geometryArrayTypes = new HashSet<Type>();

            // Vector2
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Vector2),
                GeometryTypeCategory.Vector,
                new[]
                {
                    new GeometryComponentGroup("", "2D vector", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "X"),
                        new GeometryComponent("y", "number", 0f, "Y")
                    })
                },
                false,
                "x1,y1, x2,y2, ...",
                () => UnityEngine.Vector2.zero
            ));

            // Vector3
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Vector3),
                GeometryTypeCategory.Vector,
                new[]
                {
                    new GeometryComponentGroup("", "3D vector", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "X"),
                        new GeometryComponent("y", "number", 0f, "Y"),
                        new GeometryComponent("z", "number", 0f, "Z")
                    })
                },
                false,
                "x1,y1,z1, x2,y2,z2, ...",
                () => UnityEngine.Vector3.zero
            ));

            // Vector4
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Vector4),
                GeometryTypeCategory.Vector,
                new[]
                {
                    new GeometryComponentGroup("", "4D vector", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "X"),
                        new GeometryComponent("y", "number", 0f, "Y"),
                        new GeometryComponent("z", "number", 0f, "Z"),
                        new GeometryComponent("w", "number", 0f, "W")
                    })
                },
                false,
                "x1,y1,z1,w1, x2,y2,z2,w2, ...",
                () => UnityEngine.Vector4.zero
            ));

            // Quaternion
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Quaternion),
                GeometryTypeCategory.Vector,
                new[]
                {
                    new GeometryComponentGroup("", "Quaternion rotation", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "X"),
                        new GeometryComponent("y", "number", 0f, "Y"),
                        new GeometryComponent("z", "number", 0f, "Z"),
                        new GeometryComponent("w", "number", 1f, "W")
                    })
                },
                false,
                "x1,y1,z1,w1, x2,y2,z2,w2, ...",
                () => UnityEngine.Quaternion.identity
            ));

            // Vector2Int
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Vector2Int),
                GeometryTypeCategory.VectorInt,
                new[]
                {
                    new GeometryComponentGroup("", "2D integer vector", new[]
                    {
                        new GeometryComponent("x", "integer", 0, "X"),
                        new GeometryComponent("y", "integer", 0, "Y")
                    })
                },
                true,
                "x1,y1, x2,y2, ...",
                () => UnityEngine.Vector2Int.zero
            ));

            // Vector3Int
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Vector3Int),
                GeometryTypeCategory.VectorInt,
                new[]
                {
                    new GeometryComponentGroup("", "3D integer vector", new[]
                    {
                        new GeometryComponent("x", "integer", 0, "X"),
                        new GeometryComponent("y", "integer", 0, "Y"),
                        new GeometryComponent("z", "integer", 0, "Z")
                    })
                },
                true,
                "x1,y1,z1, x2,y2,z2, ...",
                () => UnityEngine.Vector3Int.zero
            ));

            // Bounds
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Bounds),
                GeometryTypeCategory.Shape,
                new[]
                {
                    new GeometryComponentGroup("center_", "Center of bounding box", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "Center X"),
                        new GeometryComponent("y", "number", 0f, "Center Y"),
                        new GeometryComponent("z", "number", 0f, "Center Z")
                    }),
                    new GeometryComponentGroup("size_", "Size of bounding box", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "Size X"),
                        new GeometryComponent("y", "number", 0f, "Size Y"),
                        new GeometryComponent("z", "number", 0f, "Size Z")
                    })
                },
                false,
                "cx1,cy1,cz1,sx1,sy1,sz1, ...",
                () => new UnityEngine.Bounds(UnityEngine.Vector3.zero, UnityEngine.Vector3.zero)
            ));

            // BoundsInt
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.BoundsInt),
                GeometryTypeCategory.ShapeInt,
                new[]
                {
                    new GeometryComponentGroup("position_", "Position of bounding box", new[]
                    {
                        new GeometryComponent("x", "integer", 0, "Position X"),
                        new GeometryComponent("y", "integer", 0, "Position Y"),
                        new GeometryComponent("z", "integer", 0, "Position Z")
                    }),
                    new GeometryComponentGroup("size_", "Size of bounding box", new[]
                    {
                        new GeometryComponent("x", "integer", 0, "Size X"),
                        new GeometryComponent("y", "integer", 0, "Size Y"),
                        new GeometryComponent("z", "integer", 0, "Size Z")
                    })
                },
                true,
                "px1,py1,pz1,sx1,sy1,sz1, ...",
                () => new UnityEngine.BoundsInt(UnityEngine.Vector3Int.zero, UnityEngine.Vector3Int.zero)
            ));

            // Rect
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Rect),
                GeometryTypeCategory.Shape,
                new[]
                {
                    new GeometryComponentGroup("", "2D rectangle", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "X position"),
                        new GeometryComponent("y", "number", 0f, "Y position"),
                        new GeometryComponent("width", "number", 0f, "Width"),
                        new GeometryComponent("height", "number", 0f, "Height")
                    })
                },
                false,
                "x,y,w,h, x,y,w,h, ...",
                () => new UnityEngine.Rect(0, 0, 0, 0)
            ));

            // RectInt
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.RectInt),
                GeometryTypeCategory.ShapeInt,
                new[]
                {
                    new GeometryComponentGroup("", "2D integer rectangle", new[]
                    {
                        new GeometryComponent("x", "integer", 0, "X position"),
                        new GeometryComponent("y", "integer", 0, "Y position"),
                        new GeometryComponent("width", "integer", 0, "Width"),
                        new GeometryComponent("height", "integer", 0, "Height")
                    })
                },
                true,
                "x,y,w,h, x,y,w,h, ...",
                () => new UnityEngine.RectInt(0, 0, 0, 0)
            ));

            // RectOffset
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.RectOffset),
                GeometryTypeCategory.ShapeInt,
                new[]
                {
                    new GeometryComponentGroup("", "Rectangle offset/padding", new[]
                    {
                        new GeometryComponent("left", "integer", 0, "Left padding"),
                        new GeometryComponent("right", "integer", 0, "Right padding"),
                        new GeometryComponent("top", "integer", 0, "Top padding"),
                        new GeometryComponent("bottom", "integer", 0, "Bottom padding")
                    })
                },
                true,
                "l,r,t,b, l,r,t,b, ...",
                () => new UnityEngine.RectOffset()
            ));

            // Ray
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Ray),
                GeometryTypeCategory.Raycast,
                new[]
                {
                    new GeometryComponentGroup("origin_", "Ray origin point", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "Origin X"),
                        new GeometryComponent("y", "number", 0f, "Origin Y"),
                        new GeometryComponent("z", "number", 0f, "Origin Z")
                    }),
                    new GeometryComponentGroup("direction_", "Ray direction vector", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "Direction X"),
                        new GeometryComponent("y", "number", 1f, "Direction Y"),
                        new GeometryComponent("z", "number", 0f, "Direction Z")
                    })
                },
                false,
                "ox1,oy1,oz1,dx1,dy1,dz1, ...",
                () => new UnityEngine.Ray(UnityEngine.Vector3.zero, UnityEngine.Vector3.up)
            ));

            // Ray2D
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Ray2D),
                GeometryTypeCategory.Raycast,
                new[]
                {
                    new GeometryComponentGroup("origin_", "Ray origin point", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "Origin X"),
                        new GeometryComponent("y", "number", 0f, "Origin Y")
                    }),
                    new GeometryComponentGroup("direction_", "Ray direction vector", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "Direction X"),
                        new GeometryComponent("y", "number", 1f, "Direction Y")
                    })
                },
                false,
                "ox1,oy1,dx1,dy1, ...",
                () => new UnityEngine.Ray2D(UnityEngine.Vector2.zero, UnityEngine.Vector2.up)
            ));

            // Plane
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Plane),
                GeometryTypeCategory.Plane,
                new[]
                {
                    new GeometryComponentGroup("normal_", "Plane normal vector", new[]
                    {
                        new GeometryComponent("x", "number", 0f, "Normal X"),
                        new GeometryComponent("y", "number", 1f, "Normal Y"),
                        new GeometryComponent("z", "number", 0f, "Normal Z")
                    }),
                    new GeometryComponentGroup("", "Plane distance from origin", new[]
                    {
                        new GeometryComponent("distance", "number", 0f, "Distance")
                    })
                },
                false,
                "nx,ny,nz,d, ...",
                () => new UnityEngine.Plane(UnityEngine.Vector3.up, 0)
            ));

            // Color
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Color),
                GeometryTypeCategory.Color,
                new[]
                {
                    new GeometryComponentGroup("", "RGBA color", new[]
                    {
                        new GeometryComponent("r", "number", 0f, "Red (0-1)"),
                        new GeometryComponent("g", "number", 0f, "Green (0-1)"),
                        new GeometryComponent("b", "number", 0f, "Blue (0-1)"),
                        new GeometryComponent("a", "number", 1f, "Alpha (0-1)")
                    })
                },
                false,
                "r,g,b,a, r,g,b,a, ...",
                () => new UnityEngine.Color(0, 0, 0, 1)
            ));

            // Color32
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Color32),
                GeometryTypeCategory.ColorInt,
                new[]
                {
                    new GeometryComponentGroup("", "RGBA color (byte)", new[]
                    {
                        new GeometryComponent("r", "integer", 0, "Red (0-255)"),
                        new GeometryComponent("g", "integer", 0, "Green (0-255)"),
                        new GeometryComponent("b", "integer", 0, "Blue (0-255)"),
                        new GeometryComponent("a", "integer", 255, "Alpha (0-255)")
                    })
                },
                true,
                "r,g,b,a, r,g,b,a, ...",
                () => new UnityEngine.Color32(0, 0, 0, 255)
            ));

            // Matrix4x4
            RegisterDefinition(new GeometryTypeDefinition(
                typeof(UnityEngine.Matrix4x4),
                GeometryTypeCategory.Matrix,
                new[]
                {
                    new GeometryComponentGroup("", "4x4 transformation matrix (column-major)", new[]
                    {
                        new GeometryComponent("m00", "number", 1f, "M00"),
                        new GeometryComponent("m01", "number", 0f, "M01"),
                        new GeometryComponent("m02", "number", 0f, "M02"),
                        new GeometryComponent("m03", "number", 0f, "M03"),
                        new GeometryComponent("m10", "number", 0f, "M10"),
                        new GeometryComponent("m11", "number", 1f, "M11"),
                        new GeometryComponent("m12", "number", 0f, "M12"),
                        new GeometryComponent("m13", "number", 0f, "M13"),
                        new GeometryComponent("m20", "number", 0f, "M20"),
                        new GeometryComponent("m21", "number", 0f, "M21"),
                        new GeometryComponent("m22", "number", 1f, "M22"),
                        new GeometryComponent("m23", "number", 0f, "M23"),
                        new GeometryComponent("m30", "number", 0f, "M30"),
                        new GeometryComponent("m31", "number", 0f, "M31"),
                        new GeometryComponent("m32", "number", 0f, "M32"),
                        new GeometryComponent("m33", "number", 1f, "M33")
                    })
                },
                false,
                "m00,m01,m02,m03,m10,m11,m12,m13,m20,m21,m22,m23,m30,m31,m32,m33, ...",
                () => UnityEngine.Matrix4x4.identity
            ));
        }

        private static void RegisterDefinition(GeometryTypeDefinition definition)
        {
            _definitions[definition.Type] = definition;
            _geometryTypes.Add(definition.Type);

            // Register array types
            var arrayType = definition.Type.MakeArrayType();
            _arrayElementTypes[arrayType] = definition.Type;
            _geometryArrayTypes.Add(arrayType);

            // Register List<T>
            var listType = typeof(List<>).MakeGenericType(definition.Type);
            _arrayElementTypes[listType] = definition.Type;
            _geometryArrayTypes.Add(listType);
        }

        public static bool IsGeometryType(Type type)
        {
            return _geometryTypes.Contains(type);
        }

        public static bool IsGeometryArrayType(Type type)
        {
            return _geometryArrayTypes.Contains(type);
        }

        public static Type GetArrayElementType(Type arrayType)
        {
            if (_arrayElementTypes.TryGetValue(arrayType, out var elementType))
            {
                return elementType;
            }
            return null;
        }

        public static GeometryTypeDefinition GetDefinition(Type type)
        {
            _definitions.TryGetValue(type, out var definition);
            return definition;
        }
    }
}

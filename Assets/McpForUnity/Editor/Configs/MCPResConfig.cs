#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace ModelContextProtocol.Editor
{
    [CreateAssetMenu(fileName = "ResConfig", menuName = "MCP/Resource Config")]
    public class MCPResConfig : ScriptableObject
    {
        public static readonly string DefaultDirectory = "Assets/Resources";

        public static readonly string[] DefaultImageFormats = { "jpg", "jpeg", "png", "tga" };
        public static readonly string[] DefaultModelFormats = { "fbx", "obj" };
        public static readonly string[] DefaultBinaryFormats = { "bytes", "ttf" };
        public static readonly string[] DefaultTextFormats = { "json", "asset" };

        public static readonly Dictionary<string, string> DefaultMimeTypeMap = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "jpg", "image/jpeg" },
            { "jpeg", "image/jpeg" },
            { "png", "image/png" },
            { "tga", "image/targa" },
            { "fbx", "model/fbx" },
            { "obj", "model/obj" },
            { "bytes", "application/octet-stream" },
            { "json", "application/json" },
            { "asset", "text/plain" },
            { "ttf", "font/ttf" }
        };

        [Header("Additional Directories")]
        [Tooltip("Additional directories to scan (default: Assets/Resources is always included)")]
        [SerializeField]
        private string[] _customAssetDirectories = new string[0];

        [Header("Additional Formats")]
        [Tooltip("Additional file extensions (default formats are always included)")]
        [SerializeField]
        private string[] _customAssetFormats = new string[0];

        public string[] CustomAssetDirectories => _customAssetDirectories;
        public string[] CustomAssetFormats => _customAssetFormats;

        public HashSet<string> GetAllSupportedFormats()
        {
            var formats = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var format in DefaultImageFormats) formats.Add(format);
            foreach (var format in DefaultModelFormats) formats.Add(format);
            foreach (var format in DefaultBinaryFormats) formats.Add(format);
            foreach (var format in DefaultTextFormats) formats.Add(format);

            foreach (var format in _customAssetFormats)
            {
                string trimmed = format?.Trim().TrimStart('.');
                if (!string.IsNullOrEmpty(trimmed))
                    formats.Add(trimmed);
            }

            return formats;
        }

        public string GetMimeType(string extension)
        {
            string ext = extension?.TrimStart('.').ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext) && DefaultMimeTypeMap.TryGetValue(ext, out var mimeType))
                return mimeType;
            return "application/octet-stream";
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Editor
{
    public class EditorResourcesService
    {
        private const string ConfigAssetPath = "Assets/McpForUnity/Editor/Configs/ResConfig.asset";
        
        private MCPResConfig _config;
        private List<Resource> _resources = new List<Resource>();
        private Dictionary<string, string> _resourceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private McpServer _server;
        private bool _isWatching = false;
        private Debouncer _scanDebouncer;

        public EditorResourcesService()
        {
            LoadConfig();
            ScanResources();
            _scanDebouncer = new Debouncer(500);
        }

        private void LoadConfig()
        {
            _config = AssetDatabase.LoadAssetAtPath<MCPResConfig>(ConfigAssetPath);
            if (_config == null)
            {
                Debug.LogWarning($"[MCP Resources] Config not found at {ConfigAssetPath}. Using default settings.");
            }
        }

        private void ScanResources()
        {
            _resources.Clear();
            _resourceFilePaths.Clear();

            var supportedFormats = GetSupportedFormats();

            ScanDirectory(MCPResConfig.DefaultDirectory, supportedFormats);

            if (_config != null)
            {
                var customDirs = NormalizeDirectories(_config.CustomAssetDirectories);
                foreach (var directory in customDirs)
                {
                    if (!string.Equals(directory, MCPResConfig.DefaultDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        ScanDirectory(directory, supportedFormats);
                    }
                }
            }

            Debug.Log($"[MCP Resources] Scanned {_resources.Count} resources.");
            
            WriteResourceListLog();
        }

        private void WriteResourceListLog()
        {
            string logDirectory = "Logs/ResourcesList";
            
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string logFileName = $"{timestamp}_ResList.log";
            string logFilePath = Path.Combine(logDirectory, logFileName);

            try
            {
                using (var writer = new StreamWriter(logFilePath, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine($"[MCP Resources List] Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Total resources: {_resources.Count}");
                    writer.WriteLine(new string('-', 80));
                    
                    foreach (var resource in _resources)
                    {
                        writer.WriteLine($"URI: {resource.Uri}");
                        writer.WriteLine($"  Name: {resource.Name}");
                        writer.WriteLine($"  MIME: {resource.MimeType}");
                        writer.WriteLine($"  Size: {resource.Size} bytes");
                        writer.WriteLine();
                    }
                }

                Debug.Log($"[MCP Resources] Resource list log written to: {logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Resources] Failed to write log: {ex.Message}");
            }
        }

        private HashSet<string> GetSupportedFormats()
        {
            if (_config != null)
                return _config.GetAllSupportedFormats();

            var formats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in MCPResConfig.DefaultImageFormats) formats.Add(f);
            foreach (var f in MCPResConfig.DefaultModelFormats) formats.Add(f);
            foreach (var f in MCPResConfig.DefaultBinaryFormats) formats.Add(f);
            foreach (var f in MCPResConfig.DefaultTextFormats) formats.Add(f);
            return formats;
        }

        private string[] NormalizeDirectories(string[] directories)
        {
            if (directories == null || directories.Length == 0)
                return new string[0];

            return directories
                .Select(d => d?.Replace("\\", "/").Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private void ScanDirectory(string directory, HashSet<string> supportedFormats)
        {
            if (!Directory.Exists(directory))
            {
                Debug.LogWarning($"[MCP Resources] Directory not found: {directory}");
                return;
            }

            try
            {
                var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                
                foreach (var filePath in files)
                {
                    string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
                    
                    // 显式排除 .meta 文件
                    if (extension == "meta")
                        continue;
                    
                    if (!supportedFormats.Contains(extension))
                        continue;

                    string normalizedPath = filePath.Replace("\\", "/");
                    string uri = normalizedPath;
                    string fileName = Path.GetFileName(filePath);
                    long fileSize = new FileInfo(filePath).Length;
                    string mimeType = GetMimeTypeForExtension(extension);

                    var resource = new Resource
                    {
                        Uri = uri,
                        Name = fileName,
                        MimeType = mimeType,
                        Size = fileSize
                    };

                    _resources.Add(resource);
                    _resourceFilePaths[uri] = normalizedPath;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Resources] Error scanning directory {directory}: {ex.Message}");
            }
        }

        public void RegisterResources(McpServer server, bool enableFileWatching = true)
        {
            _server = server;
            
            foreach (var resource in _resources)
            {
                server.AddResource(resource, HandleReadResourceAsync);
            }
            
            if (enableFileWatching)
            {
                StartWatching();
            }
        }

        private Task<ReadResourceResult> HandleReadResourceAsync(ReadResourceRequestParams requestParams, CancellationToken cancellationToken)
        {
            var result = new ReadResourceResult
            {
                Contents = new List<ResourceContents>()
            };

            if (requestParams == null || string.IsNullOrEmpty(requestParams.Uri))
            {
                result.Contents.Add(new TextResourceContents
                {
                    Uri = requestParams?.Uri ?? "",
                    Text = "Resource URI is required"
                });
                return Task.FromResult(result);
            }

            string uri = requestParams.Uri;

            if (!_resourceFilePaths.TryGetValue(uri, out string filePath))
            {
                result.Contents.Add(new TextResourceContents
                {
                    Uri = uri,
                    Text = $"Resource not found: {uri}"
                });
                return Task.FromResult(result);
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Contents.Add(new TextResourceContents
                    {
                        Uri = uri,
                        Text = $"File not found: {filePath}"
                    });
                    return Task.FromResult(result);
                }

                byte[] fileBytes = File.ReadAllBytes(filePath);
                string base64Content = Convert.ToBase64String(fileBytes);
                string extension = Path.GetExtension(filePath).TrimStart('.');
                string mimeType = GetMimeTypeForExtension(extension);

                result.Contents.Add(new BlobResourceContents
                {
                    Uri = uri,
                    MimeType = mimeType,
                    Blob = base64Content
                });
            }
            catch (Exception ex)
            {
                result.Contents.Add(new TextResourceContents
                {
                    Uri = uri,
                    Text = $"Error reading file: {ex.Message}"
                });
            }

            return Task.FromResult(result);
        }

        private string GetMimeTypeForExtension(string extension)
        {
            if (_config != null)
                return _config.GetMimeType(extension);

            if (MCPResConfig.DefaultMimeTypeMap.TryGetValue(extension, out var mimeType))
                return mimeType;
            return "application/octet-stream";
        }

        public void Refresh()
        {
            ScanResources();
        }

        public IReadOnlyList<Resource> GetResources()
        {
            return _resources;
        }

        public void StartWatching()
        {
            if (_isWatching || _server == null) return;

            var directories = GetDirectoriesToWatch();
            
            foreach (var dir in directories)
            {
                SetupWatcherForDirectory(dir);
            }

            _isWatching = true;
            Debug.Log($"[MCP Resources] File watching started for {directories.Length} directories.");
        }

        public void StopWatching()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnFileChanged;
                watcher.Created -= OnFileChanged;
                watcher.Deleted -= OnFileChanged;
                watcher.Renamed -= OnFileRenamed;
                watcher.Dispose();
            }

            _watchers.Clear();
            _isWatching = false;
            _server = null;
            
            Debug.Log("[MCP Resources] File watching stopped.");
        }

        private string[] GetDirectoriesToWatch()
        {
            var dirs = new List<string> { MCPResConfig.DefaultDirectory };

            if (_config != null)
            {
                var customDirs = NormalizeDirectories(_config.CustomAssetDirectories);
                foreach (var dir in customDirs)
                {
                    if (!dirs.Contains(dir, StringComparer.OrdinalIgnoreCase))
                    {
                        dirs.Add(dir);
                    }
                }
            }

            return dirs.ToArray();
        }

        private void SetupWatcherForDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Debug.LogWarning($"[MCP Resources] Cannot watch non-existent directory: {directory}");
                return;
            }

            try
            {
                var watcher = new FileSystemWatcher(directory)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
                    Filter = "*.*"
                };

                watcher.Changed += OnFileChanged;
                watcher.Created += OnFileChanged;
                watcher.Deleted += OnFileChanged;
                watcher.Renamed += OnFileRenamed;

                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);

                Debug.Log($"[MCP Resources] Watching directory: {directory}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Resources] Failed to setup watcher for {directory}: {ex.Message}");
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string uri = e.FullPath.Replace("\\", "/");

            string extension = Path.GetExtension(uri).TrimStart('.').ToLowerInvariant();
            var supportedFormats = GetSupportedFormats();
            if (!supportedFormats.Contains(extension)) return;

            if (_resourceFilePaths.ContainsKey(uri))
            {
                _scanDebouncer.Debounce(() =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        _server?.NotifyResourceUpdatedAsync(uri);
                        Debug.Log($"[MCP Resources] Resource updated: {uri}");
                    };
                });
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            string oldUri = e.OldFullPath.Replace("\\", "/");
            string newUri = e.FullPath.Replace("\\", "/");

            string newExtension = Path.GetExtension(newUri).TrimStart('.').ToLowerInvariant();
            var supportedFormats = GetSupportedFormats();

            if (supportedFormats.Contains(newExtension) || _resourceFilePaths.ContainsKey(oldUri))
            {
                _scanDebouncer.Debounce(() =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        Refresh();
                        _server?.NotifyResourceListChangedAsync();
                        Debug.Log($"[MCP Resources] Resource renamed: {oldUri} -> {newUri}");
                    };
                });
            }
        }
    }

    internal class Debouncer
    {
        private readonly int _delayMilliseconds;
        private System.Threading.Timer _timer;

        public Debouncer(int delayMilliseconds)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        public void Debounce(Action action)
        {
            _timer?.Dispose();
            _timer = new System.Threading.Timer(_ =>
            {
                action?.Invoke();
            }, null, _delayMilliseconds, System.Threading.Timeout.Infinite);
        }
    }
}
#endif
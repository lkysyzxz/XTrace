using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Editor
{
    public class McpServerEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _currentPage = 0;
        private int _itemsPerPage = 10;
        private bool _resourcesEnabled = true;
        private bool _fileWatchingEnabled = false;

        private GUIStyle _headerStyle;
        private GUIStyle _toolNameStyle;
        private GUIStyle _disabledToolNameStyle;
        private GUIStyle _toolDescStyle;
        private GUIStyle _statusStyle;
        private bool _stylesInitialized;

        [MenuItem("Tools/MCP For Unity/Server Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<McpServerEditorWindow>("MCP Server");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            _resourcesEnabled = GlobalEditorMcpServer.ResourcesEnabled;
            _fileWatchingEnabled = GlobalEditorMcpServer.FileWatchingEnabled;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (GlobalEditorMcpServer.IsRunning)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (!_stylesInitialized)
            {
                InitStyles();
                _stylesInitialized = true;
            }

            DrawHeader();
            DrawServerControls();
            EditorGUILayout.Space(10);
            DrawToolList();
            DrawPagination();
        }

        private void InitStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 10)
            };

            _toolNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
            };

            _disabledToolNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = Color.gray }
            };

            _toolDescStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.gray }
            };

            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("MCP Server Editor", _headerStyle);
            EditorGUILayout.Space(5);
        }

        private void DrawServerControls()
        {
            bool isRunning = GlobalEditorMcpServer.IsRunning;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(isRunning);
            int port = GlobalEditorMcpServer.Port;
            port = EditorGUILayout.IntField("Port", port);
            GlobalEditorMcpServer.Port = Mathf.Clamp(port, 1, 65535);
            
            EditorGUILayout.Space(5);
            
            _resourcesEnabled = EditorGUILayout.Toggle("Enable Resources", _resourcesEnabled);
            GlobalEditorMcpServer.ResourcesEnabled = _resourcesEnabled;
            
            if (_resourcesEnabled)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(30);
                EditorGUILayout.BeginVertical();
                
                _fileWatchingEnabled = EditorGUILayout.Toggle("Enable File Watching", _fileWatchingEnabled);
                GlobalEditorMcpServer.FileWatchingEnabled = _fileWatchingEnabled;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !isRunning;
            if (GUILayout.Button("▶ Start", GUILayout.Height(30)))
            {
                GlobalEditorMcpServer.StartServer();
            }
            GUI.enabled = isRunning;
            if (GUILayout.Button("■ Stop", GUILayout.Height(30)))
            {
                GlobalEditorMcpServer.StopServer();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            var statusColor = isRunning ? Color.green : Color.gray;
            var statusText = isRunning ? $"Running (http://localhost:{GlobalEditorMcpServer.Port}/mcp)" : "Stopped";

            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"● {statusText}", _statusStyle);
            GUI.color = originalColor;

            if (isRunning)
            {
                int connectedClients = GlobalEditorMcpServer.Server?.ConnectedClients ?? 0;
                var clientColor = connectedClients > 0 ? Color.cyan : Color.gray;
                var clientText = connectedClients > 0 ? $"{connectedClients} client(s) connected" : "No clients connected";
                GUI.color = clientColor;
                EditorGUILayout.LabelField($"  ○ {clientText}", _statusStyle);
                GUI.color = originalColor;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToolList()
        {
            var tools = GlobalEditorMcpServer.Server?.AllTools;
            int toolCount = tools?.Count ?? 0;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Registered Tools ({toolCount})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));

            if (tools != null && toolCount > 0)
            {
                int totalPages = Mathf.CeilToInt((float)toolCount / _itemsPerPage);
                _currentPage = Mathf.Clamp(_currentPage, 0, Mathf.Max(0, totalPages - 1));

                int startIndex = _currentPage * _itemsPerPage;
                int endIndex = Mathf.Min(startIndex + _itemsPerPage, toolCount);

                for (int i = startIndex; i < endIndex; i++)
                {
                    var tool = tools[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    if (!tool.IsValid)
                    {
                        var errorStyle = new GUIStyle(EditorStyles.label)
                        {
                            normal = { textColor = Color.red },
                            fontStyle = FontStyle.Bold
                        };
                        EditorGUILayout.LabelField($"✗ {tool.Name} [Invalid]", errorStyle);
                        EditorGUILayout.LabelField(tool.Description ?? "No description", _toolDescStyle);
                        if (!string.IsNullOrEmpty(tool.ValidationError))
                        {
                            EditorGUILayout.HelpBox(tool.ValidationError, MessageType.Error);
                        }
                    }
                    else if (tool.IsDisabled)
                    {
                        EditorGUILayout.LabelField($"○ {tool.Name} [Disabled]", _disabledToolNameStyle);
                        EditorGUILayout.LabelField(tool.Description ?? "No description", _toolDescStyle);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"✓ {tool.Name}", _toolNameStyle);
                        EditorGUILayout.LabelField(tool.Description ?? "No description", _toolDescStyle);
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No tools registered.\n\nStart the server to see registered tools.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPagination()
        {
            var tools = GlobalEditorMcpServer.Server?.AllTools;
            int toolCount = tools?.Count ?? 0;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)toolCount / _itemsPerPage));

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUI.enabled = _currentPage > 0;
            if (GUILayout.Button("◀ Prev", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
            {
                _currentPage--;
            }
            GUI.enabled = true;

            EditorGUILayout.LabelField($"Page {_currentPage + 1}/{totalPages}", GUILayout.Width(80));

            GUI.enabled = _currentPage < totalPages - 1;
            if (GUILayout.Button("Next ▶", EditorStyles.miniButtonRight, GUILayout.Width(60)))
            {
                _currentPage++;
            }
            GUI.enabled = true;

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Per Page:", GUILayout.Width(60));
            _itemsPerPage = EditorGUILayout.IntPopup(_itemsPerPage,
                new[] { "5", "10", "20", "50" },
                new[] { 5, 10, 20, 50 },
                GUILayout.Width(60));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
    }
}

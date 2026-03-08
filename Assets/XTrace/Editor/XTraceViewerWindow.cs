#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace XTrace
{
    public class XTraceViewerWindow : EditorWindow
    {
        private const string WindowTitle = "XTrace Viewer";
        
        private XTraceData _data;
        private int _selectedIndex = -1;
        private Vector2 _listScrollPos;
        private Vector2 _detailScrollPos;
        private string _filePath;
        private int _callStackExpandedIndex = -1;

        private float _cachedLeftWidth = 400f;
        private bool _widthCacheValid = false;

        [MenuItem("Tools/XTrace/XTrace Viewer", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<XTraceViewerWindow>(WindowTitle);
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        void OnEnable()
        {
            wantsLessLayoutEvents = true;
        }

        void OnDisable()
        {
        }

        void OnGUI()
        {
            
            EditorGUILayout.Space(8);
            DrawToolbar();
            EditorGUILayout.Space(8);

            if (_data == null)
            {
                DrawEmptyState();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(_cachedLeftWidth), GUILayout.ExpandWidth(false));
                {
                    DrawSessionInfo();
                    DrawTracePointList();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                {
                    DrawTracePointDetail();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("Load...", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    LoadFile();
                }

                if (_data != null && GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ClearData();
                }

                if (_data != null && GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    ExportToJson();
                }

                GUILayout.FlexibleSpace();

                if (!string.IsNullOrEmpty(_filePath))
                {
                    GUILayout.Label(_filePath, EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.Label("No XTrace file loaded", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Load .xtrace file", GUILayout.Width(150), GUILayout.Height(30)))
            {
                LoadFile();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
        }

        void DrawSessionInfo()
        {
            EditorGUILayout.LabelField("Session Info", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(true);
            {
                EditorGUILayout.TextField("Session ID", _data.SessionId?.Substring(0, 8) ?? "");
                EditorGUILayout.TextField("Application", _data.ApplicationName);
                EditorGUILayout.TextField("Start Time", _data.StartTime);
                if (!string.IsNullOrEmpty(_data.EndTime))
                    EditorGUILayout.TextField("End Time", _data.EndTime);
                EditorGUILayout.IntField("Total Points", _data.TotalPoints);
                if (!string.IsNullOrEmpty(_data.Description))
                    EditorGUILayout.TextField("Description", _data.Description);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(8);
        }

        void DrawTracePointList()
        {
            EditorGUILayout.LabelField("Trace Points", EditorStyles.boldLabel);
            
            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos, false, false, GUILayout.Height(300));
            {
                if (_data.TracePoints == null || _data.TracePoints.Count == 0)
                {
                    GUILayout.Label("No trace points recorded", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (int i = 0; i < _data.TracePoints.Count; i++)
                    {
                        var point = _data.TracePoints[i];
                        var isSelected = i == _selectedIndex;
                        
                        var style = isSelected ? new GUIStyle(EditorStyles.helpBox) { normal = { background = Texture2D.grayTexture } }
                                              : EditorStyles.helpBox;
                        
                        EditorGUILayout.BeginHorizontal(style);
                        {
                            GUILayout.Label($"#{point.Id}", GUILayout.Width(30));
                            GUILayout.Label(point.ValueType, GUILayout.Width(60));
                            GUILayout.Label(Truncate(point.Value, 15), GUILayout.Width(80));
                            GUILayout.Label(Truncate(point.Prompt, 30));
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        var rect = GUILayoutUtility.GetLastRect();
                        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                        {
                            _selectedIndex = i;
                            _callStackExpandedIndex = -1;
                            Repaint();
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawTracePointDetail()
        {
            EditorGUILayout.LabelField("Detail View", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (_selectedIndex < 0 || _selectedIndex >= _data.TracePoints.Count)
            {
                GUILayout.Label("Select a trace point to view details", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var point = _data.TracePoints[_selectedIndex];

            _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);
            {
                EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.IntField("ID", point.Id);
                    EditorGUILayout.LongField("Timestamp (ticks)", point.Timestamp);
                    EditorGUILayout.TextField("Value Type", point.ValueType);
                    EditorGUILayout.TextField("Value", point.Value);
                    EditorGUILayout.TextField("Prompt", point.Prompt);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Call Stack", EditorStyles.boldLabel);

                if (point.CallStack == null || point.CallStack.Count == 0)
                {
                    GUILayout.Label("No call stack recorded", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (int i = 0; i < point.CallStack.Count; i++)
                    {
                        var frame = point.CallStack[i];
                        DrawStackFrame(frame, i);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawStackFrame(StackFrame frame, int index)
        {
            var isExpanded = _callStackExpandedIndex == index;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                if (GUILayout.Toggle(isExpanded, "", EditorStyles.foldout, GUILayout.Width(16)))
                {
                    _callStackExpandedIndex = isExpanded ? -1 : index;
                }
                
                GUILayout.Label($"[{index}]", GUILayout.Width(25));
                GUILayout.Label($"{frame.DeclaringType}.{frame.MethodName}()");
            }
            EditorGUILayout.EndHorizontal();

            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.TextField("Declaring Type", frame.DeclaringType);
                    EditorGUILayout.TextField("Method", frame.MethodName);
                    EditorGUILayout.TextField("File", frame.FileName);
                    EditorGUILayout.IntField("Line", frame.LineNumber);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
        }

        void LoadFile()
        {
            var path = EditorUtility.OpenFilePanel("Load XTrace File", "", "xtrace");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    _data = XTraceSampler.Import(path);
                    _filePath = path;
                    _selectedIndex = -1;
                    _callStackExpandedIndex = -1;
                    _cachedLeftWidth = CalculateOptimalLeftWidth();
                    _widthCacheValid = true;
                    Debug.Log($"Loaded XTrace file: {path} ({_data.TotalPoints} points)");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load XTrace file: {e.Message}");
                    EditorUtility.DisplayDialog("Error", $"Failed to load file:\n{e.Message}", "OK");
                }
            }
        }

        void ClearData()
        {
            _data = null;
            _filePath = null;
            _selectedIndex = -1;
            _callStackExpandedIndex = -1;
            _cachedLeftWidth = 400f;
            _widthCacheValid = false;
        }

        void ExportToJson()
        {
            if (_data == null)
            {
                EditorUtility.DisplayDialog("Error", "No data loaded to export.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_filePath))
            {
                EditorUtility.DisplayDialog("Error", "No source file path available. Please reload the .xtrace file.", "OK");
                return;
            }

            var defaultName = System.IO.Path.GetFileNameWithoutExtension(_filePath) + ".json";
            
            var savePath = EditorUtility.SaveFilePanel(
                "Export XTrace as JSON",
                "",
                defaultName,
                "json"
            );

            if (!string.IsNullOrEmpty(savePath))
            {
                try
                {
                    XTraceSampler.ConvertToJson(_filePath, savePath);
                    Debug.Log($"Exported XTrace to JSON: {savePath}");
                    EditorUtility.DisplayDialog("Export Successful", $"XTrace data exported to:\n{savePath}", "OK");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to export XTrace to JSON: {e.Message}");
                    EditorUtility.DisplayDialog("Export Error", $"Failed to export to JSON:\n{e.Message}", "OK");
                }
            }
        }

        private float CalculateOptimalLeftWidth()
        {
            if (_data == null) return 400f;
            
            const float MinWidth = 200f;
            const float DefaultWidth = 400f;
            const float PaddingForScrollbars = 40f;
            const float MaxWidthRatio = 0.7f;
            const float BaseListWidth = 170f;
            const float TextFieldPadding = 50f;
            
            float maxWidth = MinWidth;
            var labelStyle = EditorStyles.label;
            var textFieldStyle = EditorStyles.textField;
            
            maxWidth = Mathf.Max(maxWidth, 
                MeasureTextFieldWidth("Session ID", _data.SessionId?.Substring(0, 8) ?? "", labelStyle, textFieldStyle, TextFieldPadding));
            maxWidth = Mathf.Max(maxWidth, 
                MeasureTextFieldWidth("Application", _data.ApplicationName, labelStyle, textFieldStyle, TextFieldPadding));
            maxWidth = Mathf.Max(maxWidth, 
                MeasureTextFieldWidth("Start Time", _data.StartTime, labelStyle, textFieldStyle, TextFieldPadding));
            if (!string.IsNullOrEmpty(_data.EndTime))
                maxWidth = Mathf.Max(maxWidth, 
                    MeasureTextFieldWidth("End Time", _data.EndTime, labelStyle, textFieldStyle, TextFieldPadding));
            maxWidth = Mathf.Max(maxWidth, 
                MeasureTextFieldWidth("Total Points", _data.TotalPoints.ToString(), labelStyle, textFieldStyle, TextFieldPadding));
            if (!string.IsNullOrEmpty(_data.Description))
                maxWidth = Mathf.Max(maxWidth, 
                    MeasureTextFieldWidth("Description", _data.Description, labelStyle, textFieldStyle, TextFieldPadding));
            
            if (_data.TracePoints != null && _data.TracePoints.Count > 0)
            {
                float listMinWidth = BaseListWidth;
                
                foreach (var point in _data.TracePoints)
                {
                    float promptWidth = labelStyle.CalcSize(new GUIContent(Truncate(point.Prompt, 30))).x;
                    float rowWidth = BaseListWidth + promptWidth;
                    listMinWidth = Mathf.Max(listMinWidth, rowWidth);
                }
                
                maxWidth = Mathf.Max(maxWidth, listMinWidth);
            }
            
            maxWidth += PaddingForScrollbars;
            maxWidth = Mathf.Min(maxWidth, position.width * MaxWidthRatio);
            
            return maxWidth;
        }

        private float MeasureTextFieldWidth(string label, string value, GUIStyle labelStyle, GUIStyle textFieldStyle, float padding)
        {
            float labelWidth = labelStyle.CalcSize(new GUIContent(label)).x;
            float valueWidth = textFieldStyle.CalcSize(new GUIContent(value ?? "")).x;
            return labelWidth + valueWidth + padding;
        }

        static string Truncate(string s, int maxLength)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= maxLength)
                return s ?? "";
            return s.Substring(0, maxLength - 3) + "...";
        }
    }
}
#endif

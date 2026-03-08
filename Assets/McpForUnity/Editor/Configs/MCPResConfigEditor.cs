#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ModelContextProtocol.Editor
{
    [CustomEditor(typeof(MCPResConfig))]
    public class MCPResConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _customAssetDirectoriesProp;
        private SerializedProperty _customAssetFormatsProp;

        private int _directoriesPage = 0;
        private int _directoriesPerPage = 10;

        private void OnEnable()
        {
            _customAssetDirectoriesProp = serializedObject.FindProperty("_customAssetDirectories");
            _customAssetFormatsProp = serializedObject.FindProperty("_customAssetFormats");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Custom Settings", MessageType.Info);

            DrawDirectoriesList();

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_customAssetFormatsProp, true);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox("Default Settings (Read-Only)", MessageType.Info);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Default Directory", MCPResConfig.DefaultDirectory);
            EditorGUILayout.TextField("Image Formats", string.Join(", ", MCPResConfig.DefaultImageFormats));
            EditorGUILayout.TextField("Model Formats", string.Join(", ", MCPResConfig.DefaultModelFormats));
            EditorGUILayout.TextField("Binary Formats", string.Join(", ", MCPResConfig.DefaultBinaryFormats));
            EditorGUILayout.TextField("Text Formats", string.Join(", ", MCPResConfig.DefaultTextFormats));
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDirectoriesList()
        {
            EditorGUILayout.LabelField("Additional Directories", EditorStyles.boldLabel);

            int count = _customAssetDirectoriesProp.arraySize;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)count / _directoriesPerPage));
            _directoriesPage = Mathf.Clamp(_directoriesPage, 0, totalPages - 1);

            int startIndex = _directoriesPage * _directoriesPerPage;
            int endIndex = Mathf.Min(startIndex + _directoriesPerPage, count);

            if (count > 0)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    var element = _customAssetDirectoriesProp.GetArrayElementAtIndex(i);
                    string path = element.stringValue;

                    path = EditorGUILayout.TextField(path);
                    path = path.Replace("\\", "/");
                    element.stringValue = path;

                    if (GUILayout.Button("Del", GUILayout.Width(50)))
                    {
                        _customAssetDirectoriesProp.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No additional directories configured.", MessageType.None);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = _directoriesPage > 0;
            if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(25)))
                _directoriesPage--;
            GUI.enabled = true;

            EditorGUILayout.LabelField($"Page {_directoriesPage + 1}/{totalPages}", GUILayout.Width(80));

            GUI.enabled = _directoriesPage < totalPages - 1;
            if (GUILayout.Button("▶", EditorStyles.miniButtonMid, GUILayout.Width(25)))
                _directoriesPage++;
            GUI.enabled = true;

            EditorGUILayout.LabelField("Per Page:", GUILayout.Width(60));
            _directoriesPerPage = EditorGUILayout.IntPopup(_directoriesPerPage,
                new[] { "5", "10", "20" },
                new[] { 5, 10, 20 },
                GUILayout.Width(50));

            if (GUILayout.Button("Add", EditorStyles.miniButtonRight, GUILayout.Width(50)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Directory", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    selectedPath = selectedPath.Replace("\\", "/");

                    if (string.Equals(selectedPath, MCPResConfig.DefaultDirectory, System.StringComparison.OrdinalIgnoreCase))
                    {
                        EditorUtility.DisplayDialog("Invalid Directory",
                            $"Cannot add '{MCPResConfig.DefaultDirectory}' because it is the default directory and is always included.",
                            "OK");
                    }
                    else
                    {
                        _customAssetDirectoriesProp.arraySize++;
                        _customAssetDirectoriesProp.GetArrayElementAtIndex(_customAssetDirectoriesProp.arraySize - 1).stringValue = selectedPath;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif

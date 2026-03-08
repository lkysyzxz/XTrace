# EditorWindow Template

## Usage

```
task(
  category="visual-engineering",
  load_skills=["unity-dev"],
  prompt="Create an EditorWindow called LevelEditorWindow in Editor.Tools namespace with fields for selected level prefab and a button to generate enemies"
)
```

## Template

```csharp
using UnityEngine;
using UnityEditor;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// {{DESCRIPTION}}
    /// </summary>
    public class {{CLASS_NAME}} : EditorWindow
    {
        #region Constants

        private const string WINDOW_TITLE = "{{WINDOW_TITLE}}";
        private const float WINDOW_MIN_WIDTH = 400f;
        private const float WINDOW_MIN_HEIGHT = 300f;

        #endregion

        #region Serialized Fields

        [Header("Selection")]
        [SerializeField] private GameObject _selectedPrefab;
        
        [Header("Actions")]
        [SerializeField] private int _enemyCount = 10;
        [SerializeField] private float _spawnRadius = 5f;

        #endregion

        #region Private Fields

        private Vector2 _scrollPosition;
        private bool _showDebugInfo;

        #endregion

        #region Menu Item

        [MenuItem("Tools/{{MENU_PATH}}")]
        public static void ShowWindow()
        {
            var window = GetWindow<{{CLASS_NAME}}>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
            window.Show();
        }

        #endregion

        #region Unity Lifecycle

        private void OnGUI()
        {
            DrawHeader();
            DrawSelectionArea();
            DrawActionButtons();
            
            if (_showDebugInfo)
            {
                DrawDebugInfo();
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        #endregion

        #region GUI Drawing

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(WINDOW_TITLE, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
        }

        private void DrawSelectionArea()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _selectedPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Prefab",
                _selectedPrefab,
                typeof(GameObject),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                OnSelectionChanged();
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(_selectedPrefab == null);
            
            _enemyCount = EditorGUILayout.IntSlider("Enemy Count", _enemyCount, 1, 100);
            _spawnRadius = EditorGUILayout.Slider("Spawn Radius", _spawnRadius, 1f, 50f);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Generate Enemies", GUILayout.Height(30)))
            {
                GenerateEnemies();
            }
            
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDebugInfo()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Debug Info");
            EditorGUILayout.LabelField($"Selected: {_selectedPrefab?.name ?? "None"}");
            EditorGUILayout.LabelField($"Enemy Count: {_enemyCount}");
        }

        #endregion

        #region Event Handlers

        private void OnSelectionChanged()
        {
            Debug.Log($"Selection changed to: {_selectedPrefab?.name ?? "None"}");
        }

        #endregion

        #region Private Methods

        private void GenerateEnemies()
        {
            if (_selectedPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a prefab first!", "OK");
                return;
            }

            for (int i = 0; i < _enemyCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
                Vector3 spawnPosition = new Vector3(randomCircle.x, 0, randomCircle.y);
                
                var enemy = PrefabUtility.InstantiatePrefab(_selectedPrefab);
                enemy.transform.position = spawnPosition;
            }

            Debug.Log($"Generated {_enemyCount} enemies with radius {_spawnRadius}");
        }

        #endregion
    }
}
```

## Template Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `{{NAMESPACE}}` | C# namespace | `Editor.Tools` |
| `{{CLASS_NAME}}` | Class name (matches filename) | `LevelEditorWindow` |
| `{{DESCRIPTION}}` | XML doc summary | `Editor window for managing level objects` |
| `{{WINDOW_TITLE}}` | Window title displayed | `Level Editor` |
| `{{MENU_PATH}}` | MenuItem path in Unity menu | `Level Editor` |

## Best Practices

1. **MenuItem attribute** - Enables menu access via Tools menu
2. **minSize** - Set minimum window dimensions
3. **OnInspectorUpdate** - Refresh when Inspector changes
4. **EditorGUI.BeginDisabledGroup** - Disable UI when prerequisites not met
5. **EditorGUI.BeginChangeCheck** - Track changes for undo support
6. **PrefabUtility** - Use for prefab instantiation in Editor context
7. **EditorUtility.DisplayDialog** - Show modal dialogs

## Common Patterns

### Object Selection
```csharp
_selectedObject = Selection.activeGameObject;
```

### Scene Access
```csharp
Scene activeScene = SceneManager.GetActiveScene();
```

### Undo Support
```csharp
Undo.RecordObject(this, "Action description");
```

### Gizmos
```csharp
private void OnDrawGizmos()
{
    if (_selectedPrefab == null) return;
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, _spawnRadius);
}
```

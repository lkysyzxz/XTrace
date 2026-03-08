---
name: unity-dev
description: Unity development automation expert. Use for Unity project creation, scene management, prefab operations, asset pipeline, C# script generation, build automation, and Unity-specific workflows. Integrates with Unity MCP for direct Editor control. Covers MonoBehaviour, ScriptableObject, Editor scripting, serialization rules, and Unity 2022.3 LTS+ patterns.
license: MIT
compatibility: opencode
metadata:
  audience: game-developers
  category: game-engine
  engine: unity
  version-min: "2022.3"
  mcp-support: "CoderGamester/mcp-unity, CoplayDev/unity-mcp"
---

# Unity Development Skill

Expert Unity game development assistant with deep knowledge of Unity Editor automation, C# patterns, and production workflows.

## When to Use This Skill

Use this skill when:
- Creating or modifying Unity projects (`*.unity`, `*.prefab`, `*.asset`)
- Generating C# scripts for Unity (`MonoBehaviour`, `ScriptableObject`, `EditorWindow`)
- Managing scenes, GameObjects, components, and prefabs
- Setting up asset pipelines and import settings
- Configuring builds for multiple platforms
- Writing Editor tools and custom inspectors
- Debugging Unity-specific issues (serialization, lifecycle, performance)
- Setting up CI/CD for Unity projects

## Quick Reference

| Task | Delegation Pattern |
|------|-------------------|
| Quick script fix | `task(category="quick", load_skills=["unity-dev"], prompt="Fix...")` |
| Scene/GameObject ops | `task(category="visual-engineering", load_skills=["unity-dev"], prompt="Create...")` |
| Architecture design | `task(subagent_type="oracle", load_skills=[], prompt="Review...")` |
| Complex system | `task(category="deep", load_skills=["unity-dev"], prompt="Implement...")` |

---

## Unity MCP Integration

### Supported MCP Implementations

This skill integrates with existing Unity MCP servers for direct Editor control:

| MCP Server | Stars | Transport | Best For |
|------------|-------|-----------|----------|
| **CoderGamester/mcp-unity** | 1.4k | WebSocket | Batch operations, atomic rollback |
| **CoplayDev/unity-mcp** | 6.8k | HTTP | Tool discovery, reflection |
| **Unity Official MCP** | - | IPC | Unity 6.2+ native integration |

### MCP Detection Priority

1. **Unity Official MCP** (if Unity 6.2+ with `com.unity.ai.assistant`)
2. **CoderGamester/mcp-unity** (primary community choice)
3. **CoplayDev/unity-mcp** (fallback)
4. **Unity CLI batch mode** (no MCP available)

### Using MCP Tools

When Unity MCP is available, use these tools directly:

```
# GameObject operations
mcp_unity: create_gameobject, update_gameobject, delete_gameobject, duplicate_gameobject
mcp_unity: move_gameobject, rotate_gameobject, scale_gameobject, set_transform

# Scene operations
mcp_unity: create_scene, load_scene, save_scene, unload_scene, get_scene_info

# Component operations
mcp_unity: update_component, get_gameobject (returns components)

# Asset operations
mcp_unity: add_asset_to_scene, create_prefab, create_material, modify_material

# Build & Test
mcp_unity: run_tests, recompile_scripts, execute_menu_item

# Console monitoring
mcp_unity: get_console_logs, send_console_log
```

### Fallback: Unity CLI (Batch Mode)

When no MCP is available, use Unity CLI:

```bash
Unity.exe -batchmode -nographics -quit \
  -projectPath "PROJECT_PATH" \
  -executeMethod BuildScript.MethodName \
  -logFile - 
```

---

## Core Capabilities

### 1. C# Script Generation

Generate production-ready Unity C# scripts following best practices.

#### MonoBehaviour Template

```csharp
using UnityEngine;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// {{DESCRIPTION}}
    /// </summary>
    public class {{CLASS_NAME}} : MonoBehaviour
    {
        #region Serialized Fields

        [Header("{{HEADER_NAME}}")]
        [Tooltip("{{TOOLTIP}}")]
        [SerializeField] private {{TYPE}} _{{FIELD_NAME}}{{DEFAULT_VALUE}};

        #endregion

        #region Cached Components

        private {{COMPONENT_TYPE}} _{{COMPONENT_NAME}};

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnValidate()
        {
            // Validate Inspector values
            {{VALIDATION_LOGIC}}
        }

        #endregion

        #region Private Methods

        private void InitializeComponents()
        {
            _{{COMPONENT_NAME}} = GetComponent<{{COMPONENT_TYPE}}>();
        }

        #endregion
    }
}
```

#### ScriptableObject Template

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "New{{CLASS_NAME}}", menuName = "{{MENU_PATH}}")]
public class {{CLASS_NAME}} : ScriptableObject
{
    #region Serialized Fields

    [Header("{{HEADER_NAME}}")]
    [SerializeField] private {{TYPE}} _{{FIELD_NAME}};

    #endregion

    #region Public Properties

    public {{TYPE}} {{PROPERTY_NAME}} => _{{FIELD_NAME}};

    #endregion

    #region Validation

    private void OnValidate()
    {
        // Runtime validation
    }

    #endregion
}
```

### 2. Scene & GameObject Operations

#### Standard Operations

```yaml
# Create GameObject hierarchy
- action: create_gameobject
  name: "Player"
  parent: null
  components:
    - type: Rigidbody
      mass: 1.0
      useGravity: true
    - type: CapsuleCollider
      height: 2.0
      radius: 0.5
  transform:
    position: [0, 1, 0]
    rotation: [0, 0, 0]
    scale: [1, 1, 1]
```

#### Batch Operations (MCP)

```json
{
  "tool": "batch_execute",
  "operations": [
    {"tool": "create_gameobject", "name": "Enemy_1"},
    {"tool": "create_gameobject", "name": "Enemy_2"},
    {"tool": "create_gameobject", "name": "Enemy_3"}
  ],
  "atomic": true,
  "rollbackOnFailure": true
}
```

### 3. Asset Pipeline

#### Import Settings Patterns

```yaml
# Texture import
texture:
  maxSize: 2048
  format: "BC7"
  generateMips: true
  compression: "High Quality"

# Audio import
audio:
  loadType: "Decompress on Load"
  compressionFormat: "Vorbis"
  quality: 0.7

# Model import
model:
  scale: 1.0
  meshCompression: "Medium"
  animationType: "Generic"
```

### 4. Build Automation

#### Platform-Specific Builds

```bash
# Windows (64-bit)
Unity.exe -batchmode -quit -buildWindows64Player "Builds/Game.exe"

# macOS (Universal)
Unity.exe -batchmode -quit -buildOSXUniversalPlayer "Builds/Game.app"

# Linux
Unity.exe -batchmode -quit -buildLinux64Player "Builds/Game"

# WebGL
Unity.exe -batchmode -quit -buildTarget WebGL "Builds/WebGL"

# Android
Unity.exe -batchmode -quit -buildTarget Android "Builds/Game.apk"

# iOS (requires macOS)
Unity.exe -batchmode -quit -buildTarget iOS "Builds/Xcode"
```

#### Build Script Template

```csharp
// Assets/Editor/BuildScript.cs
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/Windows/Game.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        
        if (report.summary.result == BuildResult.Succeeded)
            EditorApplication.Exit(0);
        else
            EditorApplication.Exit(1);
    }
}
```

---

## Code Generation Rules (MANDATORY)

### MonoBehaviour Requirements

1. **File naming**: Filename MUST match class name exactly
2. **One component per file**: Never multiple MonoBehaviour classes in one file
3. **Use [SerializeField] private**: Never expose `public` fields for Inspector
4. **Cache components in Awake()**: Never `GetComponent()` in `Update()`
5. **Use [RequireComponent]**: Add for required dependencies
6. **Add [Tooltip] and [Header]**: Improve Inspector UX
7. **Implement OnValidate()**: Validate Inspector values
8. **Use #region directives**: Organize code (Fields, Cached, Lifecycle, Methods)

### Performance Patterns

```csharp
// CORRECT: Cache in Awake
private Rigidbody _rigidbody;

private void Awake()
{
    _rigidbody = GetComponent<Rigidbody>();
}

// WRONG: GetComponent every frame
private void Update()
{
    GetComponent<Rigidbody>().AddForce(force); // NEVER DO THIS
}
```

### Serialization Rules

```csharp
// CORRECT: Serialize private with property access
[SerializeField] private int _health = 100;
public int Health => _health;

// CORRECT (Unity 2022.3+): field target
[field: SerializeField] public int MaxHealth { get; private set; } = 100;

// WRONG: Public field
public int health = 100; // Breaks encapsulation
```

### Naming Conventions

| Type | Convention | Example |
|------|-----------|----------|
| Class | PascalCase | `PlayerController` |
| Method | PascalCase | `GetDamage()` |
| Property | PascalCase | `MaxHealth` |
| Private field | _camelCase | `_rigidbody` |
| Serialized field | _camelCase | `_moveSpeed` |
| Local variable | camelCase | `currentHealth` |
| Constant | PascalCase | `MaxPlayers` |
| Event | PascalCase + verb | `OnPlayerDeath` |

---

## Anti-Patterns (NEVER DO)

### 1. GetComponent in Update

```csharp
// WRONG - Performance killer
private void Update()
{
    GetComponent<Rigidbody>().velocity = Vector3.forward;
}

// CORRECT
private Rigidbody _rigidbody;

private void Awake()
{
    _rigidbody = GetComponent<Rigidbody>();
}

private void Update()
{
    _rigidbody.velocity = Vector3.forward;
}
```

### 2. Public Fields for Inspector

```csharp
// WRONG - Breaks encapsulation
public float speed = 5f;

// CORRECT
[SerializeField] private float _speed = 5f;
public float Speed => _speed;
```

### 3. Missing RequireComponent

```csharp
// WRONG - Runtime NullReferenceException
public class PhysicsObject : MonoBehaviour
{
    private void Update()
    {
        GetComponent<Rigidbody>().velocity = Vector3.forward;
    }
}

// CORRECT
[RequireComponent(typeof(Rigidbody))]
public class PhysicsObject : MonoBehaviour
{
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }
}
```

### 4. String Concatenation in Update

```csharp
// WRONG - Allocates every frame
private void Update()
{
    Debug.Log("Frame: " + Time.frameCount);
}

// CORRECT - Use string interpolation sparingly or StringBuilder
private void Update()
{
    if (debugEnabled && Time.frameCount % 60 == 0)
        Debug.Log($"Frame: {Time.frameCount}");
}
```

### 5. Editor Code in Build

```csharp
// WRONG - Crashes in build
private void Update()
{
    Selection.activeGameObject = gameObject;
}

// CORRECT
private void Update()
{
#if UNITY_EDITOR
    Selection.activeGameObject = gameObject;
#endif
}
```

---

## Project Structure Convention

```
Assets/
├── Scripts/
│   ├── Runtime/
│   │   ├── Gameplay/
│   │   │   ├── Player/
│   │   │   │   ├── PlayerController.cs
│   │   │   │   └── PlayerInput.cs
│   │   │   └── Enemies/
│   │   ├── UI/
│   │   └── Core/
│   ├── Editor/
│   │   ├── Tools/
│   │   └── Inspectors/
│   └── Tests/
│       ├── EditMode/
│       └── PlayMode/
├── Prefabs/
│   ├── Player/
│   ├── Enemies/
│   └── UI/
├── Scenes/
│   ├── MainMenu.unity
│   └── GameLevel.unity
├── Art/
│   ├── Models/
│   ├── Textures/
│   └── Materials/
├── Audio/
│   ├── Music/
│   └── SFX/
├── Data/
│   └── ScriptableObjects/
└── Resources/  # Only for runtime-loaded assets
```

---

## Testing Patterns

### EditMode Tests (Unit Tests)

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerControllerTests
{
    [Test]
    public void PlayerController_TakeDamage_ReducesHealth()
    {
        // Arrange
        var go = new GameObject();
        var controller = go.AddComponent<PlayerController>();
        controller.SetHealth(100);

        // Act
        controller.TakeDamage(20);

        // Assert
        Assert.AreEqual(80, controller.Health);
    }
}
```

### PlayMode Tests (Integration Tests)

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class PlayerMovementTests
{
    [UnityTest]
    public IEnumerator PlayerMoves_WhenInputProvided()
    {
        // Arrange
        var go = new GameObject("Player");
        var rb = go.AddComponent<Rigidbody>();
        var controller = go.AddComponent<PlayerController>();

        yield return new WaitForFixedUpdate();

        // Act
        controller.Move(Vector3.forward);
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.Greater(go.transform.position.z, 0);
    }
}
```

---

## CI/CD Integration

### GitHub Actions (game-ci)

```yaml
# .github/workflows/unity-ci.yml
name: Unity CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Cache Library
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
      
      - name: Run Tests
        uses: game-ci/unity-test-runner@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
        with:
          projectPath: .
          testMode: all
          coverageOptions: 'generateAdditionalMetrics'

  build:
    needs: test
    runs-on: ubuntu-latest
    strategy:
      matrix:
        targetPlatform:
          - StandaloneWindows64
          - StandaloneLinux64
          - WebGL
    steps:
      - uses: actions/checkout@v4
      
      - name: Build
        uses: game-ci/unity-builder@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
        with:
          targetPlatform: ${{ matrix.targetPlatform }}
```

---

## Popular Package Patterns

### DOTween Integration

```csharp
using DG.Tweening;
using UnityEngine;

public class DOTweenExample : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _duration = 1f;

    private void Start()
    {
        _target.DOMoveY(5f, _duration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => Debug.Log("Animation complete"));
    }
}
```

### Addressables Pattern

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesLoader : MonoBehaviour
{
    [SerializeField] private AssetReference _prefabReference;

    private async void Start()
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(_prefabReference);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Instantiate(handle.Result);
        }
    }
}
```

---

## Debugging Checklist

When encountering Unity issues, check:

1. **Console errors**: `Debug.LogError`, `NullReferenceException`
2. **Missing components**: `[RequireComponent]` satisfied?
3. **Serialization**: Fields marked `[SerializeField]`?
4. **Lifecycle timing**: Using `Awake` vs `Start` correctly?
5. **Play Mode state**: Is script executing during Play Mode?
6. **Assembly reload**: Static state cleared after compilation?
7. **Build target**: Platform-specific APIs used correctly?
8. **Script execution order**: `DefaultExecutionOrder` needed?

---

## Version-Specific Notes

### Unity 2022.3 LTS

- Use legacy Input Manager by default (`Input.GetKeyDown`)
- `[SerializeField]` with private fields
- `Addressables` package for asset management

### Unity 6.0+

- Unity AI (formerly Muse) integration available
- Official MCP support via `com.unity.ai.assistant`
- New Input System default (`InputSystem`)
- Active Build Profiles for multi-platform
- `[field: SerializeField]` recommended

---

## Related Skills

| Skill | Use When |
|-------|----------|
| `git-master` | Atomic commits, version control |
| `frontend-ui-ux` | UI/UX design for game interfaces |
| `playwright` | Browser-based WebGL testing |

## Resources

- **Unity Documentation**: https://docs.unity3d.com/6000.3/Documentation/Manual/
- **Unity MCP (CoderGamester)**: https://github.com/CoderGamester/mcp-unity
- **Unity MCP (CoplayDev)**: https://github.com/CoplayDev/unity-mcp
- **game-ci**: https://game.ci/
- **Unity Patterns**: https://github.com/Unity-Technologies/game-programming-patterns-demo

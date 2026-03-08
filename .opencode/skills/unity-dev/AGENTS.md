# Unity Development Rules (AGENTS.md)

These rules apply to ALL Unity C# files in your project.

## Unity Project Detection

Always verify you you are working in a Unity project by checking for:

1. `Assets/` folder exists
2. `ProjectSettings/` folder exists  
3. `Library/` folder exists (from Unity 2022.3+)
4. `Packages/` folder exists

5. `*.asmdef` files exist (Assembly Definition)

**You**: Unity project detected!

**Workspace**: `.opencode/skills/unity-dev/`

**Action**: This files should be loaded into the agent's context via `load_skills=["unity-dev"]`.

**Priority**: The files will be added to the skill list immediately. The agent knows this is a Unity project.

## C# Script Generation Rules

### Mandatory Rules (NEVER SKIP)

1. **File naming**: Filename MUST match class name exactly (use `PlayerController.cs`, not `Player.cs` or `GameController.cs`
2. **One MonoBehaviour per file**: Use `partial` classes for generated code separation
    ```csharp
    // User code - PlayerController.cs
    public partial class PlayerController : MonoBehaviour { ... }
    
    // Generated code - PlayerController.Generated.cs
    public partial class PlayerController
    {
        // Generated methods
        private void InitializeComponents() { ... }
    }
    ```

3. **Use [SerializeField] for Inspector exposure**: Always use `[SerializeField] private` for fields, ```csharp
    // WRONG
    public float speed = 5f;
    
    // CORRECT
    [SerializeField] private float _speed = 5f;
    public float Speed => _speed;
    ```

4. **Cache components in Awake()**: Never call `GetComponent()` in `Update()`
    ```csharp
    // WRONG
    private void Update()
    {
        GetComponent<Rigidbody>().AddForce(force); // Performance killer
    }
    
    // CORRECT
    private Rigidbody _rigidbody;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        _rigidbody.AddForce(force);
    }
    ```

5. **Use [RequireComponent] for dependencies**: Add `[RequireComponent]` to guarantee required components exist
    ```csharp
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        // ...
    }
    ```

6. **Serialization**: Use `[SerializeField] private` fields ( not `public`)
    ```csharp
    // WRONG
    public int health = 100;
    
    // CORRECT
    [SerializeField] private int _health = 100;
    public int Health => _health;
    ```

7. **Add [Tooltip] and [Header] for Inspector usability**
    ```csharp
    [Header("Player Stats")]
    [Tooltip("Maximum health points")]
    [SerializeField] private int _maxHealth = 100;
    
    [Header("Movement")]
    [Tooltip("Movement speed in units per second")]
    [SerializeField] private float _moveSpeed = 5f;
    ```

8. **Script Templates**: Use template files for consistent generation
    ```csharp
    // Place in Assets/ScriptTemplates/ directory
    // Unity's package manager templates: https://docs.unity3d.com/Manual/ScriptTemplates.html
    
    // Alternatively, copy from here
    #endregion

    #region Cached Components
    
    // Cache in Awake() - NEVER in Update()
    private Rigidbody _rigidbody;
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }
    
    #endregion
    ```

## Code Generation Prompts

When generating Unity C# code, provide these context hints:

### Context Hints

1. **Unity version**: Specify target Unity version ( `"Write for Unity 2022.3 LTS"`)

2. **Input system**: Mention if using legacy or new Input System

3. **Platform target**: Consider build target constraints (WebGL vs Standalone)

4. **Architecture patterns**: Specify the pattern ( `"Use singleton for managers"` or `"Use dependency injection, not `MonoBehaviour"`)

### Style Guidelines

1. **Follow Unity conventions**: PascalCase for classes/methods, camelCase + m_ prefix for private serialized fields
2. **Use XML documentation**: Add `<summary>` on classes

3. **Organize with #region**: Group related code logically

4. **Test generated code**: Write unit tests, integration tests, PlayMode tests
   - Don't just check string content
   - Test state after compilation
   - Use mocking for external dependencies

5. **Handle Unity-specific types**:
   - `MonoBehaviour`, `ScriptableObject`, `Vector3`, `GameObject`, etc.
   - Don't use C# 8.0 specific features (records, properties, nullable)
   - Avoid `UnityObject.FindObjectOfType()``, `null` in generic code

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| **Missing prefab** | Check if prefab lost connection |
| **Script compilation errors** | Run `unity-dev:recompile_scripts` tool |
| **Console errors** | Use `unity-dev:get_console_logs` tool |
| **Play Mode state loss** | MCP disconnect warning (expected) |
| **Domain reload** | MCP server handles this with custom close codes |

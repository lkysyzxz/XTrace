# MonoBehaviour Template

## Usage

```
task(
  category="quick",
  load_skills=["unity-dev"],
  prompt="Create a MonoBehaviour called PlayerController in Gameplay.Player namespace with movement, jump, and health fields"
)
```

## Template

```csharp
using UnityEngine;
using UnityEngine.Events;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// {{DESCRIPTION}}
    /// </summary>
    [RequireComponent(typeof({{REQUIRED_COMPONENT}}))]
    public class {{CLASS_NAME}} : MonoBehaviour
    {
        #region Events

        [Header("Events")]
        [SerializeField] private UnityEvent _on{{EVENT_NAME}};

        #endregion

        #region Serialized Fields

        [Header("{{HEADER_CATEGORY}}")]
        [Tooltip("{{TOOLTIP_TEXT}}")]
        [SerializeField] private {{TYPE}} _{{FIELD_NAME}}{{DEFAULT_VALUE}};
        
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 10f;
        
        [Header("Jump")]
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private float _groundCheckDistance = 0.1f;

        #endregion

        #region Cached Components

        private Rigidbody _rigidbody;
        private Collider _collider;
        private Animator _animator;

        #endregion

        #region Properties

        public {{PROPERTY_TYPE}} {{PROPERTY_NAME}} => _{{FIELD_NAME}};

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            // Initialization that depends on other components
        }

        private void Update()
        {
            HandleInput();
        }

        private void FixedUpdate()
        {
            HandlePhysicsMovement();
        }

        private void OnDrawGizmosSelected()
        {
            // Debug visualization
        }

        #endregion

        #region Public Methods

        public void {{METHOD_NAME}}({{PARAMETERS}})
        {
            // Implementation
        }

        #endregion

        #region Private Methods

        private void InitializeComponents()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _animator = GetComponent<Animator>();
        }

        private void HandleInput()
        {
            // Input processing
        }

        private void HandlePhysicsMovement()
        {
            // Physics-based movement
        }

        private bool IsGrounded()
        {
            return Physics.CheckCapsule(
                transform.position,
                transform.position + Vector3.down * _groundCheckDistance,
                _groundCheckDistance,
                _groundLayer
            );
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (_moveSpeed < 0)
                _moveSpeed = 0;
            
            if (_jumpForce < 0)
                _jumpForce = 0;
        }

        #endregion
    }
}
```

## Template Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `{{NAMESPACE}}` | C# namespace | `Gameplay.Player` |
| `{{CLASS_NAME}}` | Class name (matches filename) | `PlayerController` |
| `{{DESCRIPTION}}` | XML doc summary | `Controls player movement and actions` |
| `{{HEADER_CATEGORY}}` | Inspector header | `Movement` |
| `{{TYPE}}` | Field type | `float`, `int`, `GameObject` |
| `{{FIELD_NAME}}` | Private field name | `moveSpeed` |
| `{{DEFAULT_VALUE}}` | Default value | ` = 5f` |
| `{{TOOLTIP_TEXT}}` | Inspector tooltip | `Movement speed in units per second` |

## Generation Checklist

Before generating, ensure:

- [ ] Filename matches `{{CLASS_NAME}}` exactly
- [ ] Namespace reflects folder structure
- [ ] All `[SerializeField]` fields are `private`
- [ ] `[RequireComponent]` added for required types
- [ ] Components cached in `Awake()`
- [ ] `[Tooltip]` and `[Header]` attributes added
- [ ] `OnValidate()` for data validation
- [ ] `#region` directives organize code

## Common Variants

### Singleton Pattern

```csharp
public class {{CLASS_NAME}} : MonoBehaviour
{
    public static {{CLASS_NAME}} Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeComponents();
    }
}
```

### Poolable Object

```csharp
public class {{CLASS_NAME}} : MonoBehaviour, IPoolable
{
    private ObjectPool<{{CLASS_NAME}}> _pool;

    public void Initialize(ObjectPool<{{CLASS_NAME}}> pool)
    {
        _pool = pool;
    }

    public void ReturnToPool()
    {
        _pool.Return(this);
    }

    public void OnSpawn()
    {
        // Reset state when spawned from pool
    }

    public void OnDespawn()
    {
        // Cleanup before returning to pool
    }
}
```

### State Machine Integration

```csharp
public class {{CLASS_NAME}} : MonoBehaviour
{
    private StateMachine _stateMachine;

    private void Awake()
    {
        InitializeComponents();
        InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        _stateMachine = new StateMachine();
        
        var idleState = new IdleState(this);
        var moveState = new MoveState(this);
        
        _stateMachine.AddTransition(idleState, moveState, () => IsMoving());
        _stateMachine.AddTransition(moveState, idleState, () => !IsMoving());
        
        _stateMachine.SetState(idleState);
    }

    private void Update()
    {
        _stateMachine.Tick();
    }
}
```

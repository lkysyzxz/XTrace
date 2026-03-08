# ScriptableObject Template

## Usage

```
task(
  category="quick",
  load_skills=["unity-dev"],
  prompt="Create a ScriptableObject called WeaponData in Gameplay.Data namespace with damage, fire rate, and ammo fields"
)
```

## Template

```csharp
using UnityEngine;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// {{DESCRIPTION}}
    /// </summary>
    [CreateAssetMenu(fileName = "{{FILENAME}}", menuName = "{{MENU_PATH}}")]
    public class {{CLASS_NAME}} : ScriptableObject
    {
        #region Serialized Fields

        [Header("{{HEADER_CATEGORY}}")]
        [Tooltip("{{TOOLTIP_TEXT}}")]
        [SerializeField] private {{TYPE}} _{{FIELD_NAME}}{{DEFAULT_VALUE}};

        [Header("Weapon Stats")]
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _fireRate = 1f;
        [SerializeField] private int _ammo = 30;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _fireSound;
        [SerializeField] private AudioClip _reloadSound;

        #endregion

        #region Public Properties

        public int Damage => _damage;
        public float FireRate => _fireRate;
        public int Ammo => _ammo;

        #endregion

        #region Computed Properties

        public bool HasAmmo => _ammo > 0;
        public float TimeBetweenShots => 1f / _fireRate;

        #endregion

        #region Unity Lifecycle

        private void OnValidate()
        {
            // Validate inspector values
            _damage = Mathf.Max(1, _damage);
            _fireRate = Mathf.Max(0.1f, _fireRate);
            _ammo = Mathf.Max(0, _ammo);
        }

        #endregion

        #region Public Methods

        public void Fire()
        {
            if (!HasAmmo)
            {
                Debug.LogWarning("No ammo remaining!");
                return;
            }

            _ammo--;
            PlaySound(_fireSound);
        }

        public void Reload()
        {
            _ammo = Mathf.Max(0, _ammo);
            PlaySound(_reloadSound);
        }

        #endregion
    }
}
```

## Template Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `{{NAMESPACE}}` | C# namespace | `Gameplay.Data` |
| `{{CLASS_NAME}}` | Class name (matches filename) | `WeaponData` |
| `{{FILENAME}}` | Asset filename | `NewWeaponData` |
| `{{MENU_PATH}}` | CreateAssetMenu path | `Game/Weapon Data` |
| `{{DESCRIPTION}}` | XML doc summary | `ScriptableObject containing weapon configuration data` |

## Best Practices

1. **Use [CreateAssetMenu]** - Enables right-click creation in Project window
2. **Private serialized fields** - Use `[SerializeField]` for Inspector exposure
3. **Public properties** - Provide read-only access via properties
4. **OnValidate()** - Validate Inspector values for data integrity
5. **Computed properties** - Add derived properties (e.g., `HasAmmo`)

## Usage Examples

### Configuration Data

```csharp
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Game/Player Configuration")]
public class PlayerConfig : ScriptableObject
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _moveSpeed = 5f;

    public int MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
}
```

### Game Settings

```csharp
[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game")]
public class GameSettings : ScriptableObject
{
    [SerializeField] private float _dayLength = 300f;
    [SerializeField] private int _maxEnemies = 50;

    public float DayLength => _dayLength;
    public int MaxEnemies => _maxEnemies;
}
```

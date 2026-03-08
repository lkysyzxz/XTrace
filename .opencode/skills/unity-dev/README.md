# Unity OpenCode Skill

A Unity development automation skill that enables AI agents to interact directly with Unity Editor through the Model Context Protocol (MCP) and

## Installation

### Prerequisites

- **OpenCode** with MCP support
- **Unity 2022.3 LTS+** (for Skill templates) or Unity 6.2+ (for Unity Official MCP)
- **Node.js 18+** (for Unity MCP server)
- **Unity Editor** running

### Quick Install

1. **Install Unity MCP** (choose one):

#### Option A: CoderGamester/mcp-unity (Recommended)
```bash
# Via Unity Package Manager
# Window > Package Manager > + > Add package from git URL
# Enter: https://github.com/CoderGamester/mcp-unity.git
# Click Add
```

#### Option B: CoplayDev/unity-mcp
```bash
# Via Unity Package Manager
# Window > Package Manager > + > Add package from git URL
# Enter: https://github.com/CoplayDev/unity-mcp.git
# Click Add
```

#### Option C: Unity Official MCP (Unity 6.2+)
```bash
# Via Unity Package Manager
# Window > Package Manager > Unity Registry > Search for "Unity AI Assistant"
# Install `com.unity.ai.assistant` package
```
**Note**: Unity Official MCP requires Unity 6.2+ and the `com.unity.ai.assistant` package installed.
```

2. **Configure MCP in OpenCode**

Add to your `opencode.json`:

```json
{
  "mcp": {
    "unity-mcp": {
      "command": ["node", "${UNITY_MCP_SERVER_PATH}"]
    }
  }
}
```

Replace `${UNITY_MCP_SERVER_PATH}` with the actual path to your Unity MCP installation.

**Windows example**:
```
C:\\Users\\YourName\\Documents\\Unity\\MyProject\\Packages\\mcp-unity\\Server~\\build\\index.js
`` ```
        
**mac/Linux example**:
```
~/Documents/Unity/MyProject/Packages/mcp-unity/Server~/build/index.js
        ```
3. **Start Unity and the MCP Server**

   - Open Unity Editor
   - Navigate to `Tools > MCP Unity > Server Window`
   - Click "Start Server"
   
4. **Configure your IDE/Editor**
   - **Cursor/Windsurf/Claude Code**: Add to `.cursor/mcp.json`:
     ```json
     "mcpServers": {
       "mcp-unity": {
         "command": ["node", "PATH/TO/mcp-unity/Server~/build/index.js"]
       }
     }
     ```
     
   - **VS Code Copilot**: Install "C# Dev Kit" extension
   - **Claude Code**: Add to `mcpServers.json`:
     ```json
     "mcpServers": {
       "mcp-unity": {
         "command": ["node", "PATH/TO/mcp-unity/Server~/build/index.js"]
       }
     }
     ```
     
   - **Rider**: Requires "Mcp-unity" NuGet package:
     https://www.nuget.org/packages/mcp-unity

4. **Restart IDE** after configuration

5. **Start using the Skill**

```
task(
  category="visual-engineering",
  load_skills=["unity-dev"],
  prompt="Create a Player GameObject with Rigidbody and a capsule collider"
)
```

## Usage Examples

### Create a Simple GameObject

```
Create an empty GameObject named 'Player' at position (0, 0, 0) with a BoxCollider component
```

### Generate a ScriptableObject

```
Create a ScriptableObject asset in `Assets/Data/WeaponData.asset` with the following fields:
- Damage (int)
- Fire Rate (float)
- Ammo (int)
- Clip (AudioClip) - optional
```

### Create a Prefab from script

```
Create a prefab from the PlayerController.cs script located at Assets/Scripts/Player/ folder.
The prefab should have:
- A PlayerController component attached
- Damage field set to 10
- FireRate set to 0.5f
- MoveSpeed set to 5f
```

### Run Unity tests

```
Run all EditMode tests in the project using Unity's Test Runner
```

## Troubleshooting

### Unity MCP not connecting

1. **Verify MCP server is running**:
   - Check Unity Console for errors
   - Verify WebSocket server status in MCP Server Window
   - Check `EditorApplication.isConnecting` property

2. **Unity domain reload issues**:
   - Unity may disconnect MCP during script compilation
   - This is expected behavior
   - Reconnect after compilation completes

3. **Play Mode issues**:
   - MCP connection will be lost when entering Play Mode
   - This is expected behavior
   - Disable "Reload Domain" in Play Mode Settings or or use batch mode for testing

### Fallback: No MCP available

If no MCP is detected, use Unity CLI batch mode:

**Check for batch mode support**:
```bash
# Check if Unity supports -batchmode
Unity.exe -batchmode -nographics -quit -projectPath "YOUR_project" -executeMethod EditorScript.CheckMCP - -logFile -
```

## File Structure

```
.opencode/
└── skills/
    └── unity-dev/
        ├── SKILL.md          # Main skill file
        ├── AGENTS.md         # Unity-specific rules
        ├── templates/
        │   ├── MonoBehaviour.md
        │   ├── ScriptableObject.md
        │   ├── EditorWindow.md
        │   └── mcp-config.md    # MCP configuration template
        └── README.md          # Installation guide
```

## Next Steps

1. **Test the skill**: Try loading it in an OpenCode session
2. **Customize templates**: Modify templates to match your project conventions
3. **Add custom tools**: Extend with project-specific MCP tools
4. **Contribute**: Submit improvements via GitHub issues

## Resources

- **Unity MCP Documentation**: https://github.com/CoderGamester/mcp-unity
- **OpenCode Skills Documentation**: https://opencode.ai/docs/skills/
- **Unity Scripting Reference**: https://docs.unity3d.com/ScriptReference/
- **game-ci Actions**: https://game.ci/

# Unity MCP Configuration Template

## Overview

This template configures how Open-source Unity MCP servers integrate with the OpenCode Skill.

## Supported Implementations

### CoderGamester/mcp-unity (Recommended - Primary)

- Community-mcp-unity: 1.4k stars, WebSocket transport, Best for batch operations
### CoplayDev/unity-mcp (alternative - HTTP transport, more tools, better reflection-based discovery

### Unity Official MCP
- Unity 6.2+ only
- IPC transport ( Unity 6.2+ native

- requires `com.unity.ai.assistant` package

### Key Differences

| Feature | CoderGamester | CoplayDev | | Unity Official |
|------------------- | 
| ---|--- | ---
| `Vector3` | Vector3` positions/ | |
| `MonoBehaviour` | `Create/ update_gameobject` | |
| `update_component`    Add/remove components
    |
| `batch_execute` | Run multiple operations atomically
- `GameObject.Find` | Find GameObject by name/path/tag/layer
    |
            `GameObject result = await mcpUnity.sendToUnity("manage_gameobject", args);
```

                }
            };
            
            return new GameObjectResult
            {
                EditorUtility.DisplayDialog("Failed to create GameObject");
                return null;
            }

            return result;
        }
        
        /// <summary>
        /// Successfully created GameObject
        /// </summary>
        EditorUtility.DisplayDialog("GameObject created successfully!");
    }
    catch (Exception ex)
    {
        Debug.LogError($"Failed to create GameObject: {ex.Message}");
        return null;
    }
}

```

## Installation

### Option 1: Install via Unity Package Manager

```
Git URL: https://github.com/CoderGamester/mcp-unity.git
```
Note: Restart Unity after installation.
```

3. Configure Unity Hub (if using MCP):
    ```
    unityhub -- --headless install --version 2022.3.62f1
    ```

4. Start Unity, open the project

5. **Configure OpenCode MCP client** (Cursor, Windsurf, Claude Code):
    Add this to your `opencode.json`:

    ```json
    {
      "mcp": {
        "unity-mcp": {
          "command": ["node", "ABSOLUTE/PATH/to/your/project/mcp-unity/Server/build/index.js"]
        }
      }
    }
  }
}
```

### Option 2: Use Existing MCP installation

```
Unity Hub CLI: Install editor version
unityhub -- --headless install --version 6000.3.62f1 --module android
```

2. **Open Unity Editor** → Navigate to project

3. **Restart Unity Hub** if no MCP server is were detected

    ```
    unityhub -- --headless editors --installed
    ```

3. **Clone and build Unity MCP server**
    ```
    git clone https://github.com/CoderGamester/mcp-unity.git
    cd mcp-unity/Server~
    npm install
    npm run build
    node build/index.js
            echo "MCP Unity server started");
        }
    }
}
```

4. **Restart Unity Hub** (if already installed)
    ```
    unityhub -- --headless install --version 6000.3.62f1 --module android --no-browser
    ```

### Without MCP (CLI-based Automation

Use Unity CLI batch mode for See the templates.

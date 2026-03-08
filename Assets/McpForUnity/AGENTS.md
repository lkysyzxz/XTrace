# MCP For Unity - Submodule Guide

**Generated:** 2026-03-09
**Parent:** Root AGENTS.md (contains full API reference)
**Purpose:** MCP server implementation for Unity Editor

## OVERVIEW

Model Context Protocol (MCP) server enabling AI assistants to control Unity Editor and runtime. 70 C# files across 6 top-level modules.

## STRUCTURE

```
Assets/McpForUnity/
├── Core/                    # Server foundation
│   ├── Protocol/           # JSON-RPC & MCP types (14 dirs)
│   ├── IMcpServerHost.cs   # Host interface
│   ├── McpServerHost.cs    # Pure C# host (no built-in tools)
│   ├── McpException.cs     # Error handling
│   └── Throw.cs            # Validation helpers
├── Server/                  # Main server logic
│   ├── McpServer.cs        # Core implementation
│   ├── Tools/              # [McpServerTool] attributes
│   ├── TypeHandlers/       # Geometry type processing
│   └── Handlers/           # Request handlers
├── Transport/               # Communication layer
│   ├── HttpListenerServerTransport.cs  # HTTP + SSE server
│   ├── SseParser.cs        # Server-sent events
│   └── SseEventWriter.cs   # SSE encoding
├── Editor/                  # Editor-only server (80 tools)
│   ├── GlobalEditorMcpServer.cs  # Editor server manager
│   ├── Tools/EditorToolsList.cs  # Built-in editor tools
│   ├── Window/             # Editor windows
│   └── Configs/            # MCP resources config
├── Utilities/              # Helpers
│   ├── UnityLogger.cs      # Logging abstraction
│   ├── MainThreadDispatcher.cs
│   └── Extensions/         # Extension methods
└── Samples/                # Usage examples
    ├── CustomTypes/        # PersonInfo, AddressInfo examples
    ├── InstanceTools/      # Player/Enemy instance examples
    └── GeometryTypeExamples.cs
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add custom tools | `Server/McpServer.cs` → `AddTool()` |
| Tool registration | `Server/McpServer.cs` → `RegisterToolsFromClass<T>()` |
| Transport setup | `Transport/HttpListenerServerTransport.cs` |
| Editor tools | `Editor/Tools/EditorToolsList.cs` (80 tools) |
| Protocol types | `Core/Protocol/` (JSON-RPC, MCP requests) |
| Geometry handlers | `Server/TypeHandlers/GeometryTypeHandler.cs` |
| Custom type examples | `Samples/CustomTypes/` |
| Instance tools | `Samples/InstanceTools/` |

## CONVENTIONS

### Code Style
- **Imports order**: System.* → Third-party → UnityEngine → ModelContextProtocol.*
- **Private fields**: `_underscorePrefix`
- **Interfaces**: `I` prefix (`ITransport`, `IMcpServerHost`)
- **Braces**: Allman style (opening on new line)
- **Indentation**: 4 spaces
- **Max line length**: ~120 chars

### Async Pattern
```csharp
public async Task<CallToolResult> HandleAsync(CallToolRequestParams params, CancellationToken ct)
{
    await _server.ProcessAsync(ct);
    return result;
}
```

### Error Handling
```csharp
Throw.IfNull(arg);
Throw.IfNullOrWhiteSpace(name);
throw new McpException(McpErrorCode.InvalidParams, "Required");
return new CallToolResult { IsError = true, Content = ... };
```

### Tool Definition
```csharp
[McpServerTool("tool_name", Description = "...")]
public static CallToolResult MyTool(
    [McpArgument(Description = "...", Required = true)] string param)
{
    return new CallToolResult { Content = new List<ContentBlock> { ... } };
}
```

## KEY TYPES

| Type | Purpose | Location |
|------|---------|----------|
| `McpServerHost` | Pure C# host (no tools) | `Core/McpServerHost.cs` |
| `GlobalEditorMcpServer` | Editor server (80 tools) | `Editor/GlobalEditorMcpServer.cs` |
| `McpServer` | Main server class | `Server/McpServer.cs` |
| `CallToolResult` | Tool execution result | `Core/Protocol/Types/Tool.cs` |
| `ITransport` | Transport interface | `Transport/ITransport.cs` |

## EDITOR VS RUNTIME

| Feature | Editor Server | Runtime Server |
|---------|--------------|----------------|
| Manager | `GlobalEditorMcpServer` | `McpServerHost` |
| Default Port | 8090 | 3000 |
| Built-in Tools | 80 | 0 (manual registration) |
| Access | `Tools > MCP For Unity > Server Window` | Code only |

## ANTI-PATTERNS

- **NEVER** register tools directly to `McpServerHost` (use `Server.RegisterToolsFromClass()`)
- **NEVER** use `McpArgument.Name` for custom type fields (use `JsonProperty.PropertyName` instead)
- **NEVER** use `async void` except for Unity event handlers (`Start`, `OnDestroy`)
- **NEVER** forget to call `DisposeAsync()` on `McpServerHost` instances

## CUSTOM TYPES

Custom types require BOTH attributes:
```csharp
public class ExampleType
{
    [JsonProperty("userName")]
    [McpArgument(Description = "User name", Required = true)]
    public string UserName;  // ✓ Valid
    
    [JsonProperty("email")]
    [McpArgument(Name = "userEmail", Description = "Email")]  // ✗ Name ignored!
    public string Email;
}
```

## INSTANCE TOOLS

Register multiple instances with unique IDs:
```csharp
var player1 = new PlayerInstance { Name = "Alice" };
_server.RegisterToolsFromInstance(player1, "player_1");
// Tool names: player_1.GetHealth, player_1.SetHealth
```

## GEOMETRY TYPES

Automatic parameter expansion for Unity types:
- `Vector3` → `param_x`, `param_y`, `param_z`
- `Bounds` → `center_x,y,z`, `size_x,y,z`
- `Ray` → `origin_x,y,z`, `direction_x,y,z`
- `Color` → `r`, `g`, `b`, `a`

Arrays use flat format: `[x1,y1,z1, x2,y2,z2, ...]`

## TESTING

- Test assembly: `ModelContextProtocol.Unity.Tests`
- Use Unity Test Framework (`[Test]`, `[UnityTest]`)
- `InternalsVisibleTo` configured for internal access

## NOTES

- **UTF-8 enforced** for all JSON encoding (RFC 8259)
- **80 editor tools** available: scene, GameObject, transform, component, build, asset, prefab operations
- **Runtime server** has NO built-in tools - must register manually
- **Transport**: HTTP listener + Server-Sent Events (SSE)
- **Editor window**: `Tools > MCP For Unity > Server Window`

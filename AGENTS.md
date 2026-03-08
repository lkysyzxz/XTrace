# PROJECT KNOWLEDGE BASE

**Generated:** 2026-03-05
**Updated:** 2026-03-09
**Commit:** f2cb307
**Branch:** master
**Project:** XTrace
**Stack:** Unity 6 (6000.2.7f2) + URP + C# (.NET Standard 2.1)

## OVERVIEW

Runtime state collection/tracing module for Unity. Provides static sampling interfaces for recording parameter values, logs, and call stacks. Exports to compressed `.xtrace` files.

## STRUCTURE

```
XTrace/
├── Assets/
│   ├── XTrace/                     # Runtime tracing module
│   │   ├── Runtime/                # Core: TracePoint, XTraceSampler, XTraceSession
│   │   └── Editor/                 # XTraceViewerWindow
│   └── McpForUnity/                # MCP server integration (70 files) → see AGENTS.md
│       ├── Core/                   # McpServerHost, Protocol types
│       ├── Server/                 # McpServer, Tool handlers
│       ├── Transport/              # HTTP + SSE transport
│       ├── Editor/                 # 80 editor tools
│       └── Samples/                # Usage examples
├── XTrace.Tests/                   # dotnet unit tests → see AGENTS.md
│   ├── XTrace.Tests.csproj
│   └── XTraceCoreTests.cs
├── .opencode/skills/unity-dev/     # Unity development skill
├── Packages/
└── ProjectSettings/
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Public API | `Assets/XTrace/Runtime/XTraceSampler.cs` |
| Data structures | `Assets/XTrace/Runtime/TracePoint.cs`, `XTraceData.cs` |
| Session management | `Assets/XTrace/Runtime/XTraceSession.cs` |
| File I/O | `Assets/XTrace/Runtime/XTraceExporter.cs`, `XTraceImporter.cs` |
| Editor window | `Assets/XTrace/Editor/XTraceViewerWindow.cs` |
| Unit tests | `XTrace.Tests/XTraceCoreTests.cs` |

## CONVENTIONS

- **Language**: C# (.NET Standard 2.1 compatible)
- **Naming**: PascalCase for public API
- **Namespace**: `XTrace`
- **Internal classes**: Marked `internal`

## ANTI-PATTERNS

- DO NOT modify files in `Library/` (Unity cache)
- DO NOT commit `Library/`, `Temp/`, `Logs/`, `obj/`
- DO NOT place custom code outside `Assets/XTrace/`

## API USAGE

```csharp
using XTrace;

// Capture trace points
XTraceSampler.Sample(42, "Player health");
XTraceSampler.Sample(3.14f, "Movement speed");
XTraceSampler.Sample(true, "Is jumping");
XTraceSampler.Sample(playerName, "Current player");

// Export to .xtrace file (compressed binary format)
XTraceSampler.Export("trace_output.xtrace", "Debug session");

// Import from .xtrace file
var data = XTraceSampler.Import("trace_output.xtrace");

// Convert .xtrace to JSON format
XTraceSampler.ConvertToJson("trace.xtrace", "trace.json");

// Get JSON string representation
string json = XTraceSampler.ConvertToJsonString("trace.xtrace");

// Session management
XTraceSampler.Clear();          // Clear current session
XTraceSampler.ResetSession();   // Start new session
int count = XTraceSampler.TraceCount;
```

## FILE FORMAT

`.xtrace` file structure:
```
[4 bytes] Magic: "XTRC"
[4 bytes] Format version (int32)
[4 bytes] Uncompressed JSON length (int32)
[N bytes] GZip compressed JSON data
```

JSON schema:
```json
{
  "sessionId": "...",
  "startTime": "ISO8601",
  "endTime": "ISO8601",
  "totalPoints": N,
  "tracePoints": [
    {
      "id": 1,
      "timestamp": ticks,
      "value": "...",
      "valueType": "Int32",
      "prompt": "...",
      "callStack": [
        { "methodName": "...", "declaringType": "...", "fileName": "...", "lineNumber": N }
      ]
    }
  ]
}
```

## COMMANDS

```bash
# Run unit tests
cd XTrace.Tests && dotnet test

# Open in Unity Editor
start .
```

## EDITOR WINDOW

Open via: `Tools > XTrace Viewer`

Features:
- Load .xtrace files via file browser
- View session info (ID, app name, timestamps)
- Browse trace points list with type/value/prompt
- Inspect individual trace point details
- Expandable call stack frames with file/line info
- **Export to JSON**: Convert .xtrace files to readable JSON format
- **Adaptive width layout**: Left panel auto-adjusts to content width
- **Smart constraints**: Min 200px, max 70% of window width
- **No horizontal scrollbars**: Vertical scrolling only
- **Auto-recalculate**: Width updates on file reload

Layout:
- Left panel: SessionInfo + TracePoints list (fixed width, auto-adaptive)
- Right panel: DetailView (fills remaining horizontal space)

## NOTES

**Implementation Status**: Core functionality complete.
- Static sampling API for all basic types
- Call stack capture
- GZip compressed .xtrace file format
- Import/export with lossless compression
- JSON export functionality (2026-03-07)
- Editor window with adaptive width layout (2026-03-07)
- 18 unit tests passing

**Supported Types**: int, bool, float, double, string, long, byte, short, decimal, char, DateTime, TimeSpan, Vector2, Vector3, Vector4, Quaternion, Color, object

**Key Dependencies**:
- Input System (1.14.2)
- URP (17.2.0)
- Test Framework (1.6.0)

# Unity MCP Integration - Agent Guide

## Project Overview

Unity implementation of the Model Context Protocol (MCP) server, enabling AI assistants to control Unity Editor and runtime applications. Built on Unity 2021.3.45f1, C# 9.0, .NET Standard 2.1.

## Build Commands

### Compile Check (Unity Batch Mode)
```bash
/Applications/Unity/Hub/Editor/2021.3.45f1c2/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath . -quit -logFile -
```

### Solution File
- `UnityMCPIntergrate.sln` - Main solution (regenerate via Assets > Open C# Project)

## Test Commands

### Run All Tests
```bash
/Applications/Unity/Hub/Editor/2021.3.45f1c2/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults ./TestResults.xml \
  -quit -logFile -
```

### Run Single Test (Filter by Name)
```bash
/Applications/Unity/Hub/Editor/2021.3.45f1c2/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath . \
  -runTests -testPlatform EditMode \
  -testFilter "YourTestClass.YourTestMethod" \
  -testResults ./TestResults.xml \
  -quit -logFile -
```

### Editor Test Runner
- Window > General > Test Runner > EditMode/PlayMode

## Code Style Guidelines

### Imports Order
1. `System.*` namespaces
2. Third-party libraries (Newtonsoft.Json, etc.)
3. `UnityEngine.*` / `UnityEditor.*`
4. Project namespaces (`ModelContextProtocol.*`)

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ModelContextProtocol.Protocol;
```

### Naming Conventions
- **Classes/Methods/Properties:** PascalCase (`McpServer`, `StartAsync`)
- **Private fields:** `_underscorePrefix` (`_server`, `_isRunning`)
- **Constants:** PascalCase or ALL_CAPS
- **Interfaces:** `I` prefix (`ITransport`, `ILogger`)
- **Parameters:** camelCase

### Formatting
- **Braces:** Allman style (opening brace on new line)
- **Indentation:** 4 spaces
- **Max line length:** ~120 characters

### Async/Await Pattern
- Use `CancellationToken` for all async operations
- Return `Task<T>` for async methods
- Use `IAsyncDisposable` for resource cleanup
- `async void` ONLY for Unity event handlers (`Start`, `OnDestroy`)

```csharp
public async Task<CallToolResult> HandleAsync(CallToolRequestParams params, CancellationToken ct)
{
    await _server.ProcessAsync(ct);
    return result;
}
```

### Error Handling
- Use `McpException` with `McpErrorCode` for protocol errors
- Use `Throw` helper class for argument validation
- Return `CallToolResult { IsError = true }` for tool errors

```csharp
Throw.IfNull(arg);
Throw.IfNullOrWhiteSpace(name);

throw new McpException(McpErrorCode.InvalidParams, "Parameter required");

return new CallToolResult { IsError = true, Content = ... };
```

### Unity-Specific Patterns
- Use `[SerializeField]` for private fields exposed in Inspector
- Inherit from `MonoBehaviour` for scene components
- Use `Debug.Log*` for logging (or `UnityLogger` wrapper)
- Prefer `Object.Destroy()` over `DestroyImmediate()` in runtime

### JSON Serialization
- Use `Newtonsoft.Json` (already included via `com.unity.nuget.newtonsoft-json`)
- Annotate properties with `[JsonProperty("name")]`

```csharp
[JsonProperty("name")]
public string Name { get; set; }

[JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
public string Description { get; set; }
```

## Vector Type Support

### Supported Vector Types
| Type | JSON Schema | Protocol Format |
|------|-------------|-----------------|
| `Vector2` | `paramName_x`, `paramName_y` (number) | Individual properties |
| `Vector3` | `paramName_x`, `paramName_y`, `paramName_z` (number) | Individual properties |
| `Vector4` | `paramName_x`, `paramName_y`, `paramName_z`, `paramName_w` (number) | Individual properties |
| `Quaternion` | `paramName_x`, `paramName_y`, `paramName_z`, `paramName_w` (number) | Individual properties |
| `Vector2Int` | `paramName_x`, `paramName_y` (integer) | Individual properties |
| `Vector3Int` | `paramName_x`, `paramName_y`, `paramName_z` (integer) | Individual properties |

### Vector Array Types
| Type | JSON Schema | Protocol Format |
|------|-------------|-----------------|
| `Vector2[]` / `List<Vector2>` | `{ "type": "array", "items": { "type": "number" } }` | Flat float array `[x1,y1, x2,y2, ...]` |
| `Vector3[]` / `List<Vector3>` | `{ "type": "array", "items": { "type": "number" } }` | Flat float array `[x1,y1,z1, x2,y2,z2, ...]` |
| `Vector4[]` / `List<Vector4>` | `{ "type": "array", "items": { "type": "number" } }` | Flat float array `[x1,y1,z1,w1, x2,y2,z2,w2, ...]` |
| `Quaternion[]` / `List<Quaternion>` | `{ "type": "array", "items": { "type": "number" } }` | Flat float array `[x1,y1,z1,w1, x2,y2,z2,w2, ...]` |
| `Vector2Int[]` / `List<Vector2Int>` | `{ "type": "array", "items": { "type": "integer" } }` | Flat integer array `[x1,y1, x2,y2, ...]` |
| `Vector3Int[]` / `List<Vector3Int>` | `{ "type": "array", "items": { "type": "integer" } }` | Flat integer array `[x1,y1,z1, x2,y2,z2, ...]` |

### Example Tool with Vector Array
```csharp
[McpServerTool("set_path_points", Description = "Set path points from position array")]
public static CallToolResult SetPathPoints(
    [McpArgument(Description = "Flat array of positions [x1,y1,z1, x2,y2,z2, ...]", Required = true)]
    Vector3[] points)
{
    foreach (var point in points)
        Debug.Log($"Point: {point}");
    return new CallToolResult 
    { 
        Content = new List<ContentBlock> { new TextContentBlock { Text = $"Set {points.Length} points" } } 
    };
}
```

### Protocol Example
```json
{
  "name": "set_path_points",
  "arguments": {
    "points": [0, 0, 0, 1, 2, 3, 4, 5, 6]
  }
}
```
This represents 3 Vector3 points: (0,0,0), (1,2,3), (4,5,6)

## Geometry Type Support

### Overview
Support for Unity geometry types (Bounds, Rect, Ray, etc.) with automatic parameter expansion.

### Supported Geometry Types

#### Shape Types
| Type | Expanded Parameters | Array Format |
|------|---------------------|--------------|
| `Bounds` | `center_x`, `center_y`, `center_z`, `size_x`, `size_y`, `size_z` | `[cx,cy,cz,sx,sy,sz, ...]` |
| `BoundsInt` | `position_x`, `position_y`, `position_z`, `size_x`, `size_y`, `size_z` (integer) | `[px,py,pz,sx,sy,sz, ...]` |
| `Rect` | `x`, `y`, `width`, `height` | `[x,y,w,h, ...]` |
| `RectInt` | `x`, `y`, `width`, `height` (integer) | `[x,y,w,h, ...]` |
| `RectOffset` | `left`, `right`, `top`, `bottom` (integer) | `[l,r,t,b, ...]` |

#### Raycast Types
| Type | Expanded Parameters | Array Format |
|------|---------------------|--------------|
| `Ray` | `origin_x`, `origin_y`, `origin_z`, `direction_x`, `direction_y`, `direction_z` | `[ox,oy,oz,dx,dy,dz, ...]` |
| `Ray2D` | `origin_x`, `origin_y`, `direction_x`, `direction_y` | `[ox,oy,dx,dy, ...]` |
| `Plane` | `normal_x`, `normal_y`, `normal_z`, `distance` | `[nx,ny,nz,d, ...]` |

#### Color Types
| Type | Expanded Parameters | Array Format |
|------|---------------------|--------------|
| `Color` | `r`, `g`, `b`, `a` (0-1 range) | `[r,g,b,a, ...]` |
| `Color32` | `r`, `g`, `b`, `a` (0-255 integer) | `[r,g,b,a, ...]` |

#### Matrix Type
| Type | Expanded Parameters | Array Format |
|------|---------------------|--------------|
| `Matrix4x4` | `m00`, `m01`, `m02`, `m03`, `m10`, ..., `m33` (16 values) | `[m00-m33, ...]` |

### Example: Bounds Tool

```csharp
[McpServerTool("check_bounds", Description = "Check if point is inside bounds")]
public static CallToolResult CheckBounds(
    [McpArgument(Description = "Bounding box to check")] Bounds bounds,
    [McpArgument(Description = "Point to test")] Vector3 point)
{
    bool contains = bounds.Contains(point);
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = contains ? "Inside" : "Outside" }
        }
    };
}
```

### Protocol Example: Bounds
```json
{
  "name": "check_bounds",
  "arguments": {
    "bounds_center_x": 0,
    "bounds_center_y": 0,
    "bounds_center_z": 0,
    "bounds_size_x": 10,
    "bounds_size_y": 10,
    "bounds_size_z": 10,
    "point_x": 5,
    "point_y": 5,
    "point_z": 5
  }
}
```

### Example: Color Tool
```csharp
[McpServerTool("set_color", Description = "Set material color")]
public static CallToolResult SetColor(
    [McpArgument(Description = "RGBA color (0-1)")] Color color)
{
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = $"Color: RGBA({color.r}, {color.g}, {color.b}, {color.a})" }
        }
    };
}
```

### Example: Ray Tool
```csharp
[McpServerTool("get_ray_point", Description = "Get point along ray")]
public static CallToolResult GetRayPoint(
    [McpArgument(Description = "Ray")] Ray ray,
    [McpArgument(Description = "Distance")] float distance)
{
    Vector3 point = ray.GetPoint(distance);
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = $"Point: {point}" }
        }
    };
}
```

### Geometry Array Example
```csharp
[McpServerTool("process_rects", Description = "Process rectangle array")]
public static CallToolResult ProcessRects(
    [McpArgument(Description = "Rect array [x,y,w,h, ...]", Required = true)] Rect[] rects)
{
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = $"Processed {rects.Length} rectangles" }
        }
    };
}
```

Protocol call with array:
```json
{
  "name": "process_rects",
  "arguments": {
    "rects": [0, 0, 100, 50, 100, 100, 200, 100]
  }
}
```
This represents 2 Rects: (0,0,100,50) and (100,100,200,100)

## Custom Type Parameter Support

### Overview
Support for custom types as tool parameters. Custom types are serialized as JSON objects with automatic schema generation.

### Validation Rules (Strict Mode)
- **Required**: Fields must have BOTH `[JsonProperty]` AND `[McpArgument]` attributes
- If a field uses only one attribute, the type is marked as **invalid**
- At least one valid field is required per custom type

### Required Field Determination

Required 状态由以下特性决定（优先级从高到低）：

| Priority | Attribute | Effect |
|----------|-----------|--------|
| 1 | `[JsonRequired]` | Required = true (highest priority) |
| 2 | `[McpArgument(Required = true)]` | Required = true (fallback) |
| 3 | None | Required = false |

Example:
```csharp
public class ExampleType
{
    // Using JsonRequired
    [JsonProperty("name")]
    [JsonRequired]
    [McpArgument(Description = "Name")]
    public string Name;  // Required = true (JsonRequired)
    
    // Using McpArgument.Required
    [JsonProperty("email")]
    [McpArgument(Description = "Email", Required = true)]
    public string Email;  // Required = true
    
    // Optional field
    [JsonProperty("phone")]
    [McpArgument(Description = "Phone")]
    public string Phone;  // Required = false
}
```

### Supported Type Forms
| Type | JSON Schema | Example |
|------|-------------|---------|
| Basic custom type | `{ "type": "object", "properties": {...} }` | `PersonInfo` |
| Nested custom type | Nested object schema | `PersonInfo.Address` |
| Custom type array | `{ "type": "array", "items": {...} }` | `PersonInfo[]`, `List<PersonInfo>` |
| Mixed with primitive arrays | `{ "type": "array", "items": { "type": "string" } }` | `List<string>` |

### Constraints
- Public fields only (not properties)
- Must be non-primitive, non-Unity, non-System types
- Circular references handled gracefully

### McpArgument.Name Limitation

**For custom type fields**, `McpArgument.Name` is ignored. Field names are determined by `JsonProperty.PropertyName`.

```csharp
public class ExampleType
{
    // ✓ Correct: Only use JsonProperty for name
    [JsonProperty("userName")]
    [McpArgument(Description = "User name", Required = true)]
    public string UserName;  // JSON field name: "userName"
    
    // ✗ Avoid: McpArgument.Name will be ignored
    [JsonProperty("email")]
    [McpArgument(Name = "userEmail", Description = "Email")]  // Name ignored
    public string Email;  // JSON field name still: "email"
}
```

**Note:** For Method parameters, `McpArgument.Name` is still effective.

| Context | McpArgument.Name | JsonProperty.PropertyName |
|---------|-----------------|--------------------------|
| Method Parameter | ✓ Effective | N/A |
| Custom Type Field | ✗ Ignored | ✓ Effective |

A warning will be logged if `McpArgument.Name` conflicts with `JsonProperty.PropertyName`.

### Example: Define Custom Types

```csharp
// Basic custom type
public class AddressInfo
{
    [JsonProperty("street")]
    [JsonRequired]  // Optional: mark as required
    [McpArgument(Description = "街道名称")]
    public string Street;

    [JsonProperty("city")]
    [McpArgument(Description = "城市名称", Required = true)]
    public string City;

    [JsonProperty("zipCode")]
    [McpArgument(Description = "邮政编码")]
    public string ZipCode;
}

// Nested custom type
public class PersonInfo
{
    [JsonProperty("name")]
    [McpArgument(Description = "姓名", Required = true)]
    public string Name;

    [JsonProperty("age")]
    [McpArgument(Description = "年龄")]
    public int Age;

    [JsonProperty("address")]
    [McpArgument(Description = "地址信息（嵌套对象）")]
    public AddressInfo Address;
}

// Array custom type
public class TeamInfo
{
    [JsonProperty("teamName")]
    [McpArgument(Description = "团队名称", Required = true)]
    public string TeamName;

    [JsonProperty("members")]
    [McpArgument(Description = "团队成员列表")]
    public PersonInfo[] Members;
}
```

### Example: Tool with Custom Type

```csharp
[McpServerTool("register_person", Description = "注册新用户")]
public static CallToolResult RegisterPerson(
    [McpArgument(Description = "用户信息", Required = true)]
    PersonInfo person)
{
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = $"Registered: {person.Name}" }
        }
    };
}

[McpServerTool("create_team", Description = "创建团队")]
public static CallToolResult CreateTeam(
    [McpArgument(Description = "团队信息", Required = true)]
    TeamInfo team)
{
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = $"Team '{team.TeamName}' with {team.Members?.Length ?? 0} members" }
        }
    };
}
```

### Protocol Example
```json
{
  "name": "register_person",
  "arguments": {
    "person": {
      "name": "张三",
      "age": 25,
      "address": {
        "city": "北京",
        "zipCode": "100000"
      }
    }
  }
}
```

## UTF-8 Encoding

### RFC 8259 Compliance
All JSON request/response encoding enforces UTF-8 per RFC 8259 specification.

### Chinese Character Handling
- Request body: Read with `Encoding.UTF8`
- Response body: Written with `Encoding.UTF8`
- Content-Type: `application/json; charset=utf-8` / `text/event-stream; charset=utf-8`

### Implementation Location
`HttpListenerServerTransport.cs`:
- `HandlePostRequestAsync()`: UTF-8 request reading (line 198)
- `HandleGetRequestAsync()`: SSE stream encoding (line 160)
- Response headers include `charset=utf-8`

## Editor Window Features

### Tool Status Display
| Status | Icon | Color | Condition |
|--------|------|-------|-----------|
| Valid | ✓ | Green | `IsValid == true && !IsDisabled` |
| Disabled | ○ | Gray | `IsDisabled == true` |
| Invalid | ✗ | Red | `IsValid == false` |

### Error Information Display
- Invalid tools show `ValidationError` in a HelpBox
- Red styling for tool name with "[Invalid]" suffix
- Description still displayed below error

### Window Access
- Menu: `Tools > MCP For Unity > Server Window`
- Shows all registered tools with status indicators
- Pagination support for large tool lists

## Project Structure

```
Assets/McpForUnity/
├── Core/
│   ├── McpException.cs       # Custom exception types
│   ├── McpErrorCode.cs       # Error codes enum
│   ├── Throw.cs              # Argument validation helpers
│   ├── IMcpServerHost.cs     # Server host interface
│   ├── McpServerHost.cs      # Pure C# server host (no built-in tools)
│   ├── McpServerHostOptions.cs # Server host configuration
│   └── Protocol/             # JSON-RPC and MCP protocol types
├── Server/
│   ├── McpServer.cs          # Main server implementation
│   ├── McpServerOptions.cs   # Configuration
│   ├── McpSessionHandler.cs  # Session management
│   ├── Tools/
│   │   └── Attributes.cs     # [McpServerTool], [McpArgument]
│   └── TypeHandlers/
│       ├── GeometryTypeDefinitions.cs  # Geometry type metadata
│       └── GeometryTypeHandler.cs      # Geometry type processing
├── Transport/
│   ├── ITransport.cs         # Transport interface
│   ├── TransportBase.cs      # Base transport implementation
│   ├── HttpListenerServerTransport.cs
│   ├── SseParser.cs          # SSE parsing utilities
│   └── SseEventWriter.cs     # SSE event writing
├── Editor/                    # Editor-only MCP server
│   ├── Tools/
│   │   └── EditorToolsList.cs    # 80 built-in editor tools
│   ├── Resources/
│   │   └── EditorResourcesService.cs
│   ├── Window/
│   │   └── McpServerEditorWindow.cs
│   ├── Menu/
│   │   └── McpServerMenu.cs
│   ├── Configs/
│   │   ├── MCPResConfig.cs
│   │   └── MCPResConfigEditor.cs
│   └── GlobalEditorMcpServer.cs  # Editor server manager
├── Utilities/
│   ├── UnityLogger.cs        # Logging abstraction
│   ├── MainThreadDispatcher.cs
│   ├── UriTemplate.cs        # URI template matching
│   └── Extensions/
│       └── CommonExtensions.cs
└── Samples/
    ├── MCPExampleUsage.cs    # Usage examples
    ├── GeometryTypeExamples.cs # Geometry type examples (Bounds, Rect, Ray, etc.)
    ├── CustomTypes/          # Custom type examples
    │   ├── PersonInfo.cs
    │   ├── TeamInfo.cs
    │   ├── AddressInfo.cs
    │   └── InvalidCustomType.cs
    └── InstanceTools/        # Instance tool examples
        ├── PlayerInstance.cs
        └── EnemyInstance.cs
```

## Key Types

| Type | Purpose |
|------|---------|
| `IMcpServerHost` | Interface for MCP server host, supports `IAsyncDisposable` |
| `McpServerHost` | Pure C# implementation, auto-manages Unity lifecycle (no built-in tools) |
| `McpServerHostOptions` | Configuration for server host (port, name, version, etc.) |
| `McpServer` | Main server class, use `AddTool()`, `RegisterToolsFromClass<T>()` |
| `GlobalEditorMcpServer` | Editor-only server manager with 80 built-in tools |
| `EditorToolsList` | 80 editor tools: scene, GameObject, transform, component, build, asset, prefab |
| `CallToolResult` | Tool execution result with `Content` list and `IsError` flag |
| `ContentBlock` | Base for `TextContentBlock`, `ImageContentBlock` |
| `Tool` | MCP tool definition with `Name`, `Description`, `InputSchema`, `IsValid`, `ValidationError` |

## Editor vs Runtime

| Feature | Editor Server | Runtime Server |
|---------|--------------|----------------|
| Managed by | `GlobalEditorMcpServer` | `McpServerHost` |
| Default Port | 8090 | 3000 |
| Built-in Tools | 80 (via `EditorToolsList`) | None |
| Usage | Editor scripting | Game runtime |
| Window | `McpServerEditorWindow` | Code only |
| Resources | Supported | Not included |

## Editor MCP Server

### Overview
`GlobalEditorMcpServer` provides a standalone MCP server for Unity Editor with 80 built-in tools for scene management, GameObject operations, asset management, and more.

### Basic Usage
```csharp
using ModelContextProtocol.Editor;

// Start server
GlobalEditorMcpServer.Port = 8090;
GlobalEditorMcpServer.StartServer();

// Stop server
GlobalEditorMcpServer.StopServer();

// Access server instance
var server = GlobalEditorMcpServer.Server;
```

### Editor Window
- Menu: `Tools > MCP For Unity > Server Window`
- Configure port and resources
- View all registered tools with status indicators

### Configuration Options
| Option | Default | Description |
|--------|---------|-------------|
| `Port` | 8090 | Server port |
| `ResourcesEnabled` | false | Enable resources service |
| `FileWatchingEnabled` | false | Enable file watching |

### Built-in Editor Tools (80 tools)

| Category | Count | Examples |
|----------|-------|----------|
| Scene Management | 10 | `EditorGetActiveScene`, `EditorLoadScene`, `EditorSaveScene` |
| GameObject Operations | 15 | `EditorCreateGameObject`, `EditorFindGameObject`, `EditorSetParent` |
| Transform Operations | 10 | `EditorSetPosition`, `EditorSetRotation`, `EditorResetTransform` |
| Component Operations | 10 | `EditorAddComponent`, `EditorSetComponentProperty` |
| Compilation & Build | 10 | `EditorIsCompiling`, `EditorGetBuildTarget`, `EditorSetDefineSymbols` |
| Asset Management | 15 | `EditorFindAssets`, `EditorCreateFolder`, `EditorMoveAsset` |
| Prefab Operations | 10 | `EditorInstantiatePrefab`, `EditorCreatePrefab`, `EditorApplyPrefab` |

### Tool Implementation Location
```
Assets/McpForUnity/Editor/Tools/EditorToolsList.cs
```

## McpServerHost Usage

### Overview
`McpServerHost` is a pure C# implementation that does not require `MonoBehaviour`. It automatically listens to `Application.quitting` for lifecycle management.

> **Important**: Runtime `McpServerHost` does NOT include any built-in tools. All tools must be registered manually using `RegisterToolsFromClass()` or `AddCustomTool()`.

### Basic Usage
```csharp
using ModelContextProtocol.Unity;

public class Example : MonoBehaviour
{
    private McpServerHost _host;

    async void Start()
    {
        var options = new McpServerHostOptions
        {
            Port = 3000,
            ServerName = "UnityMCP",
            ServerVersion = "1.0.0"
        };

        _host = new McpServerHost(options);
        await _host.StartAsync();
        
        // Register custom tools (no built-in tools)
        _host.Server.RegisterToolsFromClass(typeof(MyGameTools));
    }

    private async void OnDestroy()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }
}
```

### Configuration Options
| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Port` | int | 3000 | Server port |
| `ServerName` | string | "UnityMCP" | Server name |
| `ServerVersion` | string | "1.0.0" | Server version |
| `Instructions` | string | "Unity MCP Server..." | Server instructions |
| `LogLevel` | LogLevel | Information | Log level |

### Events
```csharp
_host.OnServerStarted += () => Debug.Log("Server started");
_host.OnServerStopped += () => Debug.Log("Server stopped");
_host.OnServerError += (error) => Debug.LogError(error);
```

### Interface Usage
```csharp
IMcpServerHost host = new McpServerHost(options);
await host.StartAsync();
await host.StopAsync();
await host.DisposeAsync();
```

## Adding New Tools

### Method 1: Lambda Handler (via Host)
```csharp
_host.AddCustomTool("tool_name", "Description", async (args, ct) =>
{
    return new CallToolResult { Content = ... };
});
```

### Method 2: Lambda Handler (via Server)
```csharp
_host.Server.AddTool("tool_name", "Description", async (args, ct) =>
{
    return new CallToolResult { Content = ... };
});
```

### Method 3: Attribute-based
```csharp
public static class MyTools
{
    [McpServerTool("tool_name", Description = "...")]
    public static CallToolResult MyTool(
        [McpArgument(Description = "...", Required = true)] string param)
    {
        return new CallToolResult { Content = ... };
    }
}
// Register: _host.Server.RegisterToolsFromClass(typeof(MyTools));
```

### Method 4: With Custom Type
```csharp
public static class PersonTools
{
    [McpServerTool("update_user", Description = "Update user information")]
    public static CallToolResult UpdateUser(
        [McpArgument(Description = "User data", Required = true)] PersonInfo user)
    {
        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = $"Updated: {user.Name}" }
            }
        };
    }
}
// Register: _host.Server.RegisterToolsFromClass(typeof(PersonTools));
```

## Instance Tool Support

### Overview
Register tools from class instances, allowing multiple instances of the same type to be registered with unique IDs.

### Defining Instance Tool Class
Use `[McpInstanceTool]` attribute on the class:

```csharp
[McpInstanceTool(Name = "Player", Description = "Player instance tools")]
public class PlayerInstance
{
    public int Health { get; set; }
    public string Name { get; set; }

    [McpServerTool(Description = "Get player health")]
    public int GetHealth()
    {
        return Health;
    }

    [McpServerTool(Description = "Set player health")]
    public CallToolResult SetHealth(
        [McpArgument(Description = "Health value", Required = true)] int value)
    {
        Health = value;
        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = $"Health set to {Health}" }
            }
        };
    }
}
```

### Registering Instance Tools

```csharp
// Create instances
var player1 = new PlayerInstance { Name = "Alice", Health = 100 };
var player2 = new PlayerInstance { Name = "Bob", Health = 80 };

// Register with unique IDs
_host.Server.RegisterToolsFromInstance(player1, "player_1");
_host.Server.RegisterToolsFromInstance(player2, "player_2");

// Tool names will be:
// - player_1.GetHealth
// - player_1.SetHealth
// - player_2.GetHealth
// - player_2.SetHealth
```

### Unregistering Instance Tools

```csharp
_host.Server.UnregisterInstanceTools("player_1");
```

### Tool Name Format

| Type | Format | Example |
|------|--------|---------|
| Static Tool | `{methodName}` | `test_address` |
| Instance Tool | `{instanceId}.{methodName}` | `player_1.GetHealth` |

### Description Enhancement

Instance tool descriptions are formatted as:
`[Instance: {instanceId}] {classDescription} {methodDescription}`

Where:
- `{instanceId}`: The unique ID provided during registration
- `{classDescription}`: Description from `[McpInstanceTool]` attribute
- `{methodDescription}`: Description from `[McpServerTool]` attribute

Example:
- Class: `[McpInstanceTool(Description = "Player instance tools")]`
- Method: `[McpServerTool(Description = "Get player health")]`
- Result: `"[Instance: player_1] Player instance tools Get player health"`

### API Reference

```csharp
// Register instance tools
void RegisterToolsFromInstance(object instance, string instanceId)

// Unregister all tools for an instance
void UnregisterInstanceTools(string instanceId)
```

### Important Notes

- Instance ID must be unique across all registered instances
- Only methods with `[McpServerTool]` attribute are registered
- Instance must be kept alive for tools to work (server holds reference)
- Always unregister instances when they're no longer needed to prevent memory leaks

## Testing

- Test assembly: `ModelContextProtocol.Unity.Tests`
- Use Unity Test Framework (`[Test]`, `[UnityTest]`)
- `InternalsVisibleTo` configured for internal member access

# XTrace

**Runtime State Collection & Tracing for Unity**

[![Unity](https://img.shields.io/badge/Unity-6.0%2B-000?logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![C#](https://img.shields.io/badge/C%23-.NET%20Standard%202.1-purple.svg)](https://docs.microsoft.com/en-us/dotnet/)

A lightweight, zero-dependency runtime tracing module for Unity. Capture parameter values, debug logs, and call stacks with a single line of code, then export to compressed `.xtrace` files for offline analysis.

---

## Features

✅ **Zero-Setup Tracing** - Single static API call to capture any value  
✅ **Automatic Call Stack Capture** - Full stack trace for every sample  
✅ **Compressed Binary Format** - GZip-compressed `.xtrace` files  
✅ **JSON Export** - Convert traces to human-readable JSON  
✅ **Editor Window** - Built-in viewer for `.xtrace` files  
✅ **Type Support** - 18 built-in types (primitives, vectors, colors)  
✅ **Session Management** - Multiple isolated tracing sessions  
✅ **18 Unit Tests** - Comprehensive test coverage  

---

## Installation

### Via Git URL (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/lkysyzxz/XTrace.git`
4. Click `Add`

### Manual Installation

```bash
cd YourProject/Assets
git clone https://github.com/lkysyzxz/XTrace.git XTrace
```

---

## Quick Start

### 1. Basic Usage

```csharp
using XTrace;

public class PlayerHealth : MonoBehaviour
{
    private int health = 100;
    
    void Update()
    {
        // Capture current health with descriptive prompt
        XTraceSampler.Sample(health, "Player health");
        
        // Capture position
        XTraceSampler.Sample(transform.position, "Player position");
    }
}
```

### 2. Export Trace Data

```csharp
// At application quit or scene unload
void OnDestroy()
{
    XTraceSampler.Export("session_2026.xtrace", "Debug Session");
}
```

### 3. View in Editor

Open `Tools > XTrace Viewer` → Load your `.xtrace` file

---

## API Reference

### Sampling Methods

```csharp
// Capture any value with a descriptive prompt
XTraceSampler.Sample<T>(T value, string prompt)

// Supported types:
// Primitives: int, bool, float, double, string, long, byte, short, decimal, char
// Unity types: Vector2, Vector3, Vector4, Quaternion, Color
// Other: DateTime, TimeSpan, object
```

### Export & Import

```csharp
// Export to compressed .xtrace file
XTraceSampler.Export(string filename, string appName = null)

// Import from .xtrace file
XTraceData XTraceSampler.Import(string filename)

// Convert to JSON file
XTraceSampler.ConvertToJson(string xtracePath, string jsonPath)

// Get JSON string
string XTraceSampler.ConvertToJsonString(string xtracePath)
```

### Session Management

```csharp
// Clear current session data
XTraceSampler.Clear()

// Reset and start new session
XTraceSampler.ResetSession()

// Get trace count
int count = XTraceSampler.TraceCount
```

---

## File Format

### `.xtrace` Binary Structure

```
[4 bytes]  Magic number: "XTRC"
[4 bytes]  Format version (int32)
[4 bytes]  Uncompressed JSON length (int32)
[N bytes]  GZip compressed JSON data
```

### JSON Schema

```json
{
  "sessionId": "uuid-string",
  "startTime": "2026-03-09T10:30:00Z",
  "endTime": "2026-03-09T11:45:00Z",
  "totalPoints": 1542,
  "tracePoints": [
    {
      "id": 1,
      "timestamp": 638452345678901234,
      "value": "42",
      "valueType": "Int32",
      "prompt": "Player health",
      "callStack": [
        {
          "methodName": "Update",
          "declaringType": "PlayerHealth",
          "fileName": "PlayerHealth.cs",
          "lineNumber": 15
        }
      ]
    }
  ]
}
```

---

## Editor Window

Access via `Tools > XTrace Viewer`

### Features

- **File Browser** - Load `.xtrace` files with one click
- **Session Info** - View ID, app name, timestamps
- **Trace Points List** - Browse by type/value/prompt
- **Detail Inspector** - Inspect individual trace points
- **Call Stack Viewer** - Expandable frames with file/line info
- **JSON Export** - Convert to readable JSON format
- **Adaptive Layout** - Auto-adjusting panel widths

---

## Examples

### Game State Tracking

```csharp
public class GameManager : MonoBehaviour
{
    void Update()
    {
        XTraceSampler.Sample(GameManager.Instance.Score, "Current score");
        XTraceSampler.Sample(GameManager.Instance.Lives, "Remaining lives");
        XTraceSampler.Sample(Time.time, "Game time");
    }
    
    void GameOver()
    {
        XTraceSampler.Export($"gameover_{System.DateTime.Now:yyyy-MM-dd_HH-mm}.xtrace", "GameOver");
    }
}
```

### Performance Profiling

```csharp
public class PerformanceMonitor : MonoBehaviour
{
    private Stopwatch updateTimer = new Stopwatch();
    
    void Update()
    {
        updateTimer.Restart();
        
        // Your update logic here
        HeavyCalculation();
        
        updateTimer.Stop();
        XTraceSampler.Sample(updateTimer.ElapsedMilliseconds, "Update duration (ms)");
    }
}
```

### Network Debugging

```csharp
public class NetworkManager : MonoBehaviour
{
    void OnPacketReceived(NetworkPacket packet)
    {
        XTraceSampler.Sample(packet.SequenceId, "Packet sequence");
        XTraceSampler.Sample(packet.Timestamp, "Packet timestamp");
        XTraceSampler.Sample(packet.Size, "Packet size (bytes)");
    }
}
```

---

## Testing

### Run Unit Tests

```bash
cd XTrace.Tests
dotnet test
```

### Test Coverage

- ✅ Serialization tests (TracePoint, XTraceData)
- ✅ Compression tests (GZip roundtrip)
- ✅ Import/Export tests (file I/O)
- ✅ Session management tests (lifecycle)
- ✅ Configuration tests (XTraceSessionConfig)

**18 tests total** | 100% passing

---

## Requirements

- **Unity**: 2022.3 LTS or later (tested on Unity 6)
- **.NET**: .NET Standard 2.1 compatible
- **Dependencies**: None (standalone)

---

## Project Structure

```
XTrace/
├── Assets/
│   └── XTrace/
│       ├── Runtime/           # Core tracing module
│       │   ├── TracePoint.cs
│       │   ├── XTraceData.cs
│       │   ├── XTraceSession.cs
│       │   ├── XTraceSampler.cs    # Public API
│       │   ├── XTraceExporter.cs
│       │   └── XTraceImporter.cs
│       └── Editor/           # Editor window
│           └── XTraceViewerWindow.cs
├── XTrace.Tests/            # Unit tests (xUnit)
│   ├── XTrace.Tests.csproj
│   └── XTraceCoreTests.cs
└── README.md
```

---

## Advanced Usage

### Custom Session Configuration

```csharp
var config = new XTraceSessionConfig
{
    EnableCallStack = true,
    MaxTracePoints = 10000
};

XTraceSampler.Configure(config);
```

### Multiple Sessions

```csharp
// Clear and start fresh
XTraceSampler.ResetSession();

// Your tracing code...

// Export this session
XTraceSampler.Export("level1.xtrace", "Level 1 Gameplay");
```

---

## Troubleshooting

### Issue: Empty `.xtrace` files

**Cause**: No samples recorded before export  
**Solution**: Ensure `XTraceSampler.Sample()` is called at least once

### Issue: Call stack not captured

**Cause**: Call stack capture disabled  
**Solution**: Enable in config or use default settings (enabled by default)

### Issue: Large file sizes

**Cause**: Too many trace points  
**Solution**: Limit trace frequency or use session clearing

---

## Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Changelog

### v1.0.0 (2026-03-09)
- Initial release
- Core tracing functionality
- GZip compressed file format
- Editor window
- JSON export
- 18 unit tests

---

## Author

**lkysyzxz**  
GitHub: [@lkysyzxz](https://github.com/lkysyzxz)

---

## Acknowledgments

- Built for Unity 6 with URP
- Inspired by the need for lightweight runtime debugging
- Thanks to all contributors

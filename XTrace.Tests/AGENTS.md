# XTrace.Tests - Unit Test Guide

**Generated:** 2026-03-09
**Parent:** Root AGENTS.md
**Purpose:** dotnet unit tests for XTrace core (separate from Unity)

## OVERVIEW

Standalone .NET 8.0 test project for XTrace core functionality. 18 tests covering serialization, compression, import/export. Uses xUnit, not Unity Test Framework.

## STRUCTURE

```
XTrace.Tests/
├── XTrace.Tests.csproj       # .NET 8.0 project
├── XTraceCoreTests.cs        # 18 unit tests (496 lines)
├── bin/                      # Build output
└── obj/                      # Intermediate files
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Test cases | `XTraceCoreTests.cs` |
| Project config | `XTrace.Tests.csproj` |
| Test output | `bin/Debug/net8.0/` |

## COMMANDS

```bash
# Run all tests
cd XTrace.Tests && dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test
dotnet test --filter "TestName"

# Watch mode (auto-rerun on changes)
dotnet watch test
```

## CONVENTIONS

- **Framework**: xUnit (NOT Unity Test Framework)
- **Target**: .NET 8.0
- **Location**: Separate project outside Assets/
- **Scope**: Core XTrace logic only (no Unity dependencies)
- **Coverage**: Serialization, compression, import/export, data models

## TEST CATEGORIES

| Category | Count | Focus |
|----------|-------|-------|
| Serialization | ~6 | TracePoint, XTraceData JSON roundtrip |
| Compression | ~4 | GZip compression/decompression |
| Import/Export | ~5 | .xtrace file format |
| Data Models | ~3 | Session metadata, trace point structure |

## ANTI-PATTERNS

- **DO NOT** reference Unity assemblies (UnityEngine, UnityEditor)
- **DO NOT** use Unity Test Framework attributes (`[Test]`, `[UnityTest]`)
- **DO NOT** test Unity-specific code here (use Assets/XTrace/Tests/ for that)
- **DO NOT** commit `bin/` or `obj/` directories

## KEY FILES

| File | Purpose |
|------|---------|
| `XTraceCoreTests.cs` | Main test suite (496 lines) |
| `XTrace.Tests.csproj` | Project configuration |

## RELATION TO UNITY TESTS

| Location | Framework | Purpose |
|----------|-----------|---------|
| `XTrace.Tests/` | xUnit (.NET 8.0) | Core logic tests |
| `Assets/XTrace/Tests/` | Unity Test Framework | Unity integration tests |

## NOTES

- **Why separate project**: Core XTrace has no Unity dependencies
- **18 tests passing** as of 2026-03-07
- **Fast execution**: No Unity overhead
- **CI-friendly**: Standard dotnet test commands

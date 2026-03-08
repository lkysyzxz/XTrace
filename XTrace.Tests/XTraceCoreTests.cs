using System;
using System.IO;
using Xunit;
using XTrace;

namespace XTrace.Tests;

public class XTraceCoreTests
{
    [Fact]
    public void TracePoint_ShouldInitializeCorrectly()
    {
        var point = new TracePoint(1, 1000, "test_value", "String", "test prompt");

        Assert.Equal(1, point.Id);
        Assert.Equal(1000, point.Timestamp);
        Assert.Equal("test_value", point.Value);
        Assert.Equal("String", point.ValueType);
        Assert.Equal("test prompt", point.Prompt);
        Assert.Empty(point.CallStack);
    }

    [Fact]
    public void StackFrame_ShouldInitializeCorrectly()
    {
        var frame = new StackFrame("TestMethod", "TestClass", "Test.cs", 42);

        Assert.Equal("TestMethod", frame.MethodName);
        Assert.Equal("TestClass", frame.DeclaringType);
        Assert.Equal("Test.cs", frame.FileName);
        Assert.Equal(42, frame.LineNumber);
    }

    [Fact]
    public void StackFrame_ToString_ShouldFormatCorrectly()
    {
        var frame = new StackFrame("TestMethod", "TestClass", "Test.cs", 42);
        var result = frame.ToString();

        Assert.Contains("TestClass.TestMethod", result);
        Assert.Contains("Test.cs:42", result);
    }

    [Fact]
    public void XTraceData_ShouldInitializeCorrectly()
    {
        var data = new XTraceData("session-123", "TestApp");

        Assert.Equal("session-123", data.SessionId);
        Assert.Equal("TestApp", data.ApplicationName);
        Assert.NotNull(data.StartTime);
        Assert.NotNull(data.TracePoints);
        Assert.NotNull(data.Metadata);
        Assert.Equal(0, data.TotalPoints);
    }

    [Fact]
    public void XTraceData_AddTracePoint_ShouldIncreaseCount()
    {
        var data = new XTraceData("session-123");
        var point = new TracePoint(1, 1000, "value", "String", "prompt");

        data.AddTracePoint(point);

        Assert.Single(data.TracePoints);
        Assert.Equal(1, data.TotalPoints);
        Assert.Equal(point, data.TracePoints[0]);
    }

    [Fact]
    public void XTraceData_Complete_ShouldSetEndTime()
    {
        var data = new XTraceData("session-123");
        Assert.Null(data.EndTime);

        data.Complete();

        Assert.NotNull(data.EndTime);
    }

    [Fact]
    public void XTraceSession_Current_ShouldReturnSameInstance()
    {
        var session1 = XTraceSession.Current;
        var session2 = XTraceSession.Current;

        Assert.Same(session1, session2);
    }

    [Fact]
    public void XTraceSession_Capture_ShouldCreateTracePoint()
    {
        var session = XTraceSession.Current;
        session.Clear();

        var point = session.Capture("test-sampler", 42, "test prompt");

        Assert.NotNull(point);
        Assert.Equal(1, point.Id);
        Assert.Equal("42", point.Value);
        Assert.Equal("Int32", point.ValueType);
        Assert.Equal("test prompt", point.Prompt);
        Assert.Equal("test-sampler", point.SamplerUniqueName);
    }

    [Fact]
    public void XTraceSession_Capture_ShouldIncrementId()
    {
        var session = XTraceSession.Current;
        session.Clear();

        var point1 = session.Capture("test-sampler", 1, "first");
        var point2 = session.Capture("test-sampler", 2, "second");

        Assert.Equal(1, point1.Id);
        Assert.Equal(2, point2.Id);
    }

    [Fact]
    public void XTraceSession_Clear_ShouldRemoveAllPoints()
    {
        var session = XTraceSession.Current;
        session.Capture("test-sampler", 1, "test1");
        session.Capture("test-sampler", 2, "test2");

        session.Clear();

        Assert.Equal(0, session.Data.TotalPoints);
    }

    [Fact]
    public void XTraceSession_Complete_ShouldEndSession()
    {
        var session = XTraceSession.Current;
        session.Capture("test-sampler", 1, "test");

        var data = session.Complete();

        Assert.NotNull(data.EndTime);
    }
}

public class XTraceFileTests : IDisposable
{
    private readonly string _testDir;

    public XTraceFileTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "XTraceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void Export_ShouldCreateValidFile()
    {
        var data = new XTraceData("test-session", "TestApp");
        data.AddTracePoint(new TracePoint(1, 1000, "42", "Int32", "test"));
        data.Complete();

        var filePath = Path.Combine(_testDir, "test.xtrace");
        XTraceSession.ExportData(data, filePath);

        Assert.True(File.Exists(filePath));
        
        var bytes = File.ReadAllBytes(filePath);
        Assert.True(bytes.Length > 4);
        
        Assert.Equal((byte)'X', bytes[0]);
        Assert.Equal((byte)'T', bytes[1]);
        Assert.Equal((byte)'R', bytes[2]);
        Assert.Equal((byte)'C', bytes[3]);
    }

    [Fact]
    public void Export_ShouldAutoAddExtension()
    {
        var data = new XTraceData("test-session");
        data.Complete();

        var filePath = Path.Combine(_testDir, "test");
        XTraceSession.ExportData(data, filePath);

        Assert.True(File.Exists(filePath + ".xtrace"));
    }

    [Fact]
    public void Import_ShouldRestoreTraceData()
    {
        var originalData = new XTraceData("test-session-123", "TestApp");
        originalData.Description = "Test description";
        originalData.Metadata["key1"] = "value1";
        originalData.AddTracePoint(new TracePoint(1, 1000, "42", "Int32", "first point"));
        originalData.AddTracePoint(new TracePoint(2, 2000, "hello", "String", "second point"));
        originalData.Complete();

        var filePath = Path.Combine(_testDir, "roundtrip.xtrace");
        XTraceSession.ExportData(originalData, filePath);

        var restoredData = XTraceImporter.Import(filePath);

        Assert.Equal(originalData.SessionId, restoredData.SessionId);
        Assert.Equal(originalData.ApplicationName, restoredData.ApplicationName);
        Assert.Equal(originalData.Description, restoredData.Description);
        Assert.Equal(originalData.TotalPoints, restoredData.TotalPoints);
        Assert.Equal(originalData.Metadata["key1"], restoredData.Metadata["key1"]);
        
        Assert.Equal(2, restoredData.TracePoints.Count);
        Assert.Equal("42", restoredData.TracePoints[0].Value);
        Assert.Equal("hello", restoredData.TracePoints[1].Value);
    }

    [Fact]
    public void Import_WithCallStack_ShouldRestoreStackFrames()
    {
        var data = new XTraceData("test-session");
        var point = new TracePoint(1, 1000, "value", "String", "prompt");
        point.CallStack.Add(new StackFrame("MethodA", "ClassA", "FileA.cs", 10));
        point.CallStack.Add(new StackFrame("MethodB", "ClassB", "FileB.cs", 20));
        data.AddTracePoint(point);
        data.Complete();

        var filePath = Path.Combine(_testDir, "stacktest.xtrace");
        XTraceSession.ExportData(data, filePath);

        var restored = XTraceImporter.Import(filePath);

        Assert.Single(restored.TracePoints);
        Assert.Equal(2, restored.TracePoints[0].CallStack.Count);
        Assert.Equal("MethodA", restored.TracePoints[0].CallStack[0].MethodName);
        Assert.Equal("ClassB", restored.TracePoints[0].CallStack[1].DeclaringType);
    }

    [Fact]
    public void Import_InvalidFile_ShouldThrow()
    {
        var filePath = Path.Combine(_testDir, "invalid.xtrace");
        File.WriteAllBytes(filePath, new byte[] { 1, 2, 3, 4, 5 });

        Assert.Throws<InvalidDataException>(() => XTraceImporter.Import(filePath));
    }

    [Fact]
    public void Import_NonexistentFile_ShouldThrow()
    {
        Assert.Throws<FileNotFoundException>(() => XTraceImporter.Import("nonexistent.xtrace"));
    }
}

public class XTraceCompressionTests : IDisposable
{
    private readonly string _testDir;

    public XTraceCompressionTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "XTraceCompressionTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void Export_CompressionShouldReduceSize()
    {
        var data = new XTraceData("test-session");
        
        for (int i = 0; i < 1000; i++)
        {
            data.AddTracePoint(new TracePoint(i, i * 1000, 
                "This is a repetitive test value that should compress well", 
                "String", 
                "This is a repetitive prompt that should also compress well"));
        }
        data.Complete();

        var filePath = Path.Combine(_testDir, "compressed.xtrace");
        XTraceSession.ExportData(data, filePath);

        var fileSize = new FileInfo(filePath).Length;
        
        Assert.True(fileSize < 100000, $"File size {fileSize} should be compressed");
    }
}

public class SamplerTests
{
    public SamplerTests()
    {
        XTraceSession.Reset();
    }

    [Fact]
    public void Sampler_ShouldHaveUniqueNameAndDescription()
    {
        XTraceSession.Reset();
        var sampler = XTraceSession.Current.CreateSampler("test-sampler", "Test description");

        Assert.Equal("test-sampler", sampler.UniqueName);
        Assert.Equal("Test description", sampler.Description);
    }

    [Fact]
    public void Sampler_ShouldNotCaptureWhenDisabled()
    {
        XTraceSession.Reset();
        var sampler = XTraceSession.Current.CreateSampler("disabled-sampler", "", enabled: false);

        var point = sampler.Sample(42, "test prompt");

        Assert.Null(point);
    }

    [Fact]
    public void Sampler_ShouldCaptureWhenEnabled()
    {
        XTraceSession.Reset();
        var sampler = XTraceSession.Current.CreateSampler("enabled-sampler", "", enabled: true);

        var point = sampler.Sample(42, "test prompt");

        Assert.NotNull(point);
        Assert.Equal("42", point.Value);
        Assert.Equal("test prompt", point.Prompt);
        Assert.Equal("enabled-sampler", point.SamplerUniqueName);
    }

    [Fact]
    public void Sampler_ShouldThrowOnNullOrEmptyUniqueName()
    {
        XTraceSession.Reset();
        Assert.Throws<ArgumentNullException>(() => XTraceSession.Current.CreateSampler(null));
        Assert.Throws<ArgumentNullException>(() => XTraceSession.Current.CreateSampler(""));
    }

    [Fact]
    public void XTraceSession_CreateSampler_ShouldRegisterAndGet()
    {
        XTraceSession.Reset();
        var sampler = XTraceSession.Current.CreateSampler("registered-sampler");

        var retrieved = XTraceSession.Current.GetSampler("registered-sampler");

        Assert.Same(sampler, retrieved);
    }

    [Fact]
    public void XTraceSession_EnableSampler_ShouldEnableSampler()
    {
        XTraceSession.Reset();
        var sampler = XTraceSession.Current.CreateSampler("to-enable", enabled: false);
        Assert.False(XTraceSession.Current.IsSamplerEnabled("to-enable"));

        XTraceSession.Current.EnableSampler("to-enable");

        Assert.True(XTraceSession.Current.IsSamplerEnabled("to-enable"));
    }

    [Fact]
    public void XTraceSession_DisableSampler_ShouldDisableSampler()
    {
        XTraceSession.Reset();
        var sampler = XTraceSession.Current.CreateSampler("to-disable", enabled: true);

        XTraceSession.Current.DisableSampler("to-disable");

        Assert.False(XTraceSession.Current.IsSamplerEnabled("to-disable"));
    }

    [Fact]
    public void XTraceSession_UnregisterSampler_ShouldRemoveSampler()
    {
        XTraceSession.Reset();
        var sampler = XTraceSession.Current.CreateSampler("to-remove", enabled: true);

        var removed = XTraceSession.Current.UnregisterSampler("to-remove");

        Assert.True(removed);
        Assert.Null(XTraceSession.Current.GetSampler("to-remove"));
    }

    [Fact]
    public void Sampler_DuplicateUniqueName_ShouldThrow()
    {
        XTraceSession.Reset();
        var sampler1 = XTraceSession.Current.CreateSampler("duplicate-id");

        Assert.Throws<InvalidOperationException>(() => XTraceSession.Current.CreateSampler("duplicate-id"));
    }

    [Fact]
    public void TracePoint_ShouldIncludeSamplerUniqueName()
    {
        var point = new TracePoint(1, 1000, "value", "String", "prompt", "my-sampler");

        Assert.Equal("my-sampler", point.SamplerUniqueName);
    }

    [Fact]
    public void XTraceSession_EnableDisable_ShouldControlAllSampling()
    {
        XTraceSession.Reset();
        var sampler = XTraceSession.Current.CreateSampler("global-test", enabled: true);

        XTraceSession.Current.Disable();
        Assert.False(XTraceSession.Current.IsEnabled);

        var point1 = sampler.Sample(1, "should not capture");
        Assert.Null(point1);

        XTraceSession.Current.Enable();
        Assert.True(XTraceSession.Current.IsEnabled);

        var point2 = sampler.Sample(2, "should capture");
        Assert.NotNull(point2);
    }

    [Fact]
    public void XTraceSession_GetSamplersInfo_ShouldReturnJson()
    {
        XTraceSession.Reset();
        XTraceSession.Current.CreateSampler("sampler-1", "First sampler", enabled: true);
        XTraceSession.Current.CreateSampler("sampler-2", "Second sampler", enabled: false);

        var json = XTraceSession.Current.GetSamplersInfoJson();

        Assert.Contains("sampler-1", json);
        Assert.Contains("First sampler", json);
        Assert.Contains("sampler-2", json);
        Assert.Contains("Second sampler", json);
    }

    [Fact]
    public void XTraceSessionConfig_ShouldInitializeSamplers()
    {
        XTraceSession.Reset();
        var config = new XTraceSessionConfig()
            .AddSampler("config-sampler-1", "From config", enabledByDefault: true)
            .AddSampler("config-sampler-2", "Also from config", enabledByDefault: false);

        var session = XTraceSession.Create(config);

        Assert.True(session.IsSamplerEnabled("config-sampler-1"));
        Assert.False(session.IsSamplerEnabled("config-sampler-2"));
    }

    [Fact]
    public void XTraceSession_GetOrCreateSampler_ShouldReturnExistingOrCreate()
    {
        XTraceSession.Reset();
        var sampler1 = XTraceSession.Current.GetOrCreateSampler("auto-sampler", "Auto created");

        var sampler2 = XTraceSession.Current.GetOrCreateSampler("auto-sampler", "Should be ignored");

        Assert.Same(sampler1, sampler2);
        Assert.Equal("Auto created", sampler1.Description);
    }
}

public class XTraceSessionConfigTests : IDisposable
{
    private readonly string _testDir;

    public XTraceSessionConfigTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "XTraceConfigTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void XTraceSessionConfig_FromJson_ValidInput_ReturnsConfig()
    {
        string json = @"{
            ""Samplers"": [
                {
                    ""UniqueName"": ""TestSampler"",
                    ""Description"": ""Test description"",
                    ""EnabledByDefault"": true
                }
            ],
            ""EnabledByDefault"": false
        }";

        var config = XTraceSessionConfig.FromJson(json);

        Assert.NotNull(config);
        Assert.Equal(1, config.Samplers.Count);
        Assert.Equal("TestSampler", config.Samplers[0].UniqueName);
        Assert.True(config.Samplers[0].EnabledByDefault);
        Assert.False(config.EnabledByDefault);
    }

    [Fact]
    public void XTraceSessionConfig_ToJson_ReturnsValidJson()
    {
        var config = new XTraceSessionConfig();
        config.Samplers.Add(new SamplerDefinition("Sampler1", "Desc1", true));
        config.EnabledByDefault = false;

        string json = config.ToJson();

        Assert.NotNull(json);
        Assert.Contains("Samplers", json);
        Assert.Contains("Sampler1", json);
    }

    [Fact]
    public void XTraceSessionConfig_LoadFromFile_ValidPath_ReturnsConfig()
    {
        string testPath = Path.Combine(_testDir, "test_config.json");
        string json = @"{""Samplers"": [], ""EnabledByDefault"": true}";
        
        File.WriteAllText(testPath, json);
        
        var config = XTraceSessionConfig.LoadFromFile(testPath);
        
        Assert.NotNull(config);
        Assert.True(config.EnabledByDefault);
        Assert.Empty(config.Samplers);
    }

    [Fact]
    public void XTraceSessionConfig_LoadFromFile_FileNotFound_ThrowsException()
    {
        Assert.Throws<FileNotFoundException>(() => 
            XTraceSessionConfig.LoadFromFile("nonexistent_config.json")
        );
    }

    [Fact]
    public void XTraceSessionConfig_SaveToFile_ValidPath_SavesJson()
    {
        string testPath = Path.Combine(_testDir, "save_test.json");
        
        var config = new XTraceSessionConfig();
        config.Samplers.Add(new SamplerDefinition("Test", "Test Desc", true));
        config.EnabledByDefault = true;
        
        config.SaveToFile(testPath);
        
        Assert.True(File.Exists(testPath));
        
        string savedJson = File.ReadAllText(testPath);
        Assert.Contains("Test", savedJson);
        Assert.Contains("Test Desc", savedJson);
    }
}

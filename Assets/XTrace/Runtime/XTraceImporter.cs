using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace XTrace
{
    /// <summary>
    /// Handles importing XTrace data from compressed .xtrace files.
    /// </summary>
    public static class XTraceImporter
    {
        /// <summary>
        /// Imports XTrace data from a file.
        /// </summary>
        /// <param name="filePath">The file path to read from.</param>
        /// <returns>The loaded trace data.</returns>
        public static XTraceData Import(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"XTrace file not found: {filePath}");

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Read and validate magic bytes
                var magicBytes = new byte[4];
                if (fileStream.Read(magicBytes, 0, 4) != 4)
                    throw new InvalidDataException("Invalid XTrace file: too short");

                var expectedMagic = Encoding.ASCII.GetBytes("XTRC");
                for (int i = 0; i < 4; i++)
                {
                    if (magicBytes[i] != expectedMagic[i])
                        throw new InvalidDataException("Invalid XTrace file: wrong magic bytes");
                }

                // Read format version
                var versionBytes = new byte[4];
                if (fileStream.Read(versionBytes, 0, 4) != 4)
                    throw new InvalidDataException("Invalid XTrace file: missing version");

                var version = BitConverter.ToInt32(versionBytes, 0);
                if (version > XTraceData.FormatVersion)
                    throw new InvalidDataException($"Unsupported XTrace format version: {version}");

                // Read uncompressed length
                var lengthBytes = new byte[4];
                if (fileStream.Read(lengthBytes, 0, 4) != 4)
                    throw new InvalidDataException("Invalid XTrace file: missing length");

                var uncompressedLength = BitConverter.ToInt32(lengthBytes, 0);

                // Decompress data
                string json;
                using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }

                // Parse JSON
                return ParseJson(json);
            }
        }

        private static XTraceData ParseJson(string json)
        {
            var data = new XTraceData();
            var parser = new JsonParser(json);

            parser.Expect('{');

            while (!parser.Peek('}'))
            {
                var key = parser.ReadString();
                parser.Expect(':');

                switch (key)
                {
                    case "formatVersion":
                        parser.ReadInt(); // Skip, we handle version at file level
                        break;
                    case "sessionId":
                        data.SessionId = parser.ReadString();
                        break;
                    case "startTime":
                        data.StartTime = parser.ReadString();
                        break;
                    case "endTime":
                        data.EndTime = parser.ReadString();
                        break;
                    case "applicationName":
                        data.ApplicationName = parser.ReadString();
                        break;
                    case "description":
                        data.Description = parser.ReadString();
                        break;
                    case "totalPoints":
                        data.TotalPoints = parser.ReadInt();
                        break;
                    case "metadata":
                        ParseMetadata(parser, data.Metadata);
                        break;
                    case "tracePoints":
                        ParseTracePoints(parser, data.TracePoints);
                        break;
                    default:
                        parser.SkipValue();
                        break;
                }

                if (parser.Peek(',')) parser.Advance();
            }

            parser.Expect('}');
            return data;
        }

        private static void ParseMetadata(JsonParser parser, Dictionary<string, string> metadata)
        {
            parser.Expect('{');
            while (!parser.Peek('}'))
            {
                var key = parser.ReadString();
                parser.Expect(':');
                var value = parser.ReadString();
                metadata[key] = value;
                if (parser.Peek(',')) parser.Advance();
            }
            parser.Expect('}');
        }

        private static void ParseTracePoints(JsonParser parser, List<TracePoint> points)
        {
            parser.Expect('[');
            while (!parser.Peek(']'))
            {
                points.Add(ParseTracePoint(parser));
                if (parser.Peek(',')) parser.Advance();
            }
            parser.Expect(']');
        }

        private static TracePoint ParseTracePoint(JsonParser parser)
        {
            var point = new TracePoint();
            parser.Expect('{');

            while (!parser.Peek('}'))
            {
                var key = parser.ReadString();
                parser.Expect(':');

                switch (key)
                {
                    case "id":
                        point.Id = parser.ReadInt();
                        break;
                    case "timestamp":
                        point.Timestamp = parser.ReadLong();
                        break;
                    case "samplerUniqueName":
                        point.SamplerUniqueName = parser.ReadString();
                        break;
                    case "samplerId":
                        // Backward compatibility: map old samplerId to SamplerUniqueName
                        point.SamplerUniqueName = parser.ReadString();
                        break;
                    case "value":
                        point.Value = parser.ReadString();
                        break;
                    case "valueType":
                        point.ValueType = parser.ReadString();
                        break;
                    case "prompt":
                        point.Prompt = parser.ReadString();
                        break;
                    case "callStack":
                        ParseCallStack(parser, point.CallStack);
                        break;
                    default:
                        parser.SkipValue();
                        break;
                }

                if (parser.Peek(',')) parser.Advance();
            }

            parser.Expect('}');
            return point;
        }

        private static void ParseCallStack(JsonParser parser, List<StackFrame> callStack)
        {
            parser.Expect('[');
            while (!parser.Peek(']'))
            {
                callStack.Add(ParseStackFrame(parser));
                if (parser.Peek(',')) parser.Advance();
            }
            parser.Expect(']');
        }

        private static StackFrame ParseStackFrame(JsonParser parser)
        {
            var frame = new StackFrame();
            parser.Expect('{');

            while (!parser.Peek('}'))
            {
                var key = parser.ReadString();
                parser.Expect(':');

                switch (key)
                {
                    case "methodName":
                        frame.MethodName = parser.ReadString();
                        break;
                    case "declaringType":
                        frame.DeclaringType = parser.ReadString();
                        break;
                    case "fileName":
                        frame.FileName = parser.ReadString();
                        break;
                    case "lineNumber":
                        frame.LineNumber = parser.ReadInt();
                        break;
                    default:
                        parser.SkipValue();
                        break;
                }

                if (parser.Peek(',')) parser.Advance();
            }

            parser.Expect('}');
            return frame;
        }

        /// <summary>
        /// Simple JSON parser for XTrace format.
        /// </summary>
        private class JsonParser
        {
            private readonly string _json;
            private int _pos;

            public JsonParser(string json)
            {
                _json = json;
                _pos = 0;
                SkipWhitespace();
            }

            public void SkipWhitespace()
            {
                while (_pos < _json.Length && char.IsWhiteSpace(_json[_pos]))
                    _pos++;
            }

            public bool Peek(char c)
            {
                SkipWhitespace();
                return _pos < _json.Length && _json[_pos] == c;
            }

            public void Advance()
            {
                _pos++;
                SkipWhitespace();
            }

            public void Expect(char c)
            {
                SkipWhitespace();
                if (_pos >= _json.Length || _json[_pos] != c)
                    throw new InvalidDataException($"Expected '{c}' at position {_pos}");
                _pos++;
                SkipWhitespace();
            }

            public string ReadString()
            {
                SkipWhitespace();
                if (_json[_pos] != '"')
                    throw new InvalidDataException($"Expected string at position {_pos}");
                
                _pos++;
                var sb = new StringBuilder();
                while (_pos < _json.Length && _json[_pos] != '"')
                {
                    if (_json[_pos] == '\\' && _pos + 1 < _json.Length)
                    {
                        _pos++;
                        switch (_json[_pos])
                        {
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            default: sb.Append(_json[_pos]); break;
                        }
                    }
                    else
                    {
                        sb.Append(_json[_pos]);
                    }
                    _pos++;
                }
                _pos++; // Skip closing quote
                SkipWhitespace();
                return sb.ToString();
            }

            public int ReadInt()
            {
                SkipWhitespace();
                int start = _pos;
                if (_json[_pos] == '-') _pos++;
                while (_pos < _json.Length && char.IsDigit(_json[_pos]))
                    _pos++;
                var result = int.Parse(_json.Substring(start, _pos - start));
                SkipWhitespace();
                return result;
            }

            public long ReadLong()
            {
                SkipWhitespace();
                int start = _pos;
                if (_json[_pos] == '-') _pos++;
                while (_pos < _json.Length && char.IsDigit(_json[_pos]))
                    _pos++;
                var result = long.Parse(_json.Substring(start, _pos - start));
                SkipWhitespace();
                return result;
            }

            public void SkipValue()
            {
                SkipWhitespace();
                if (_json[_pos] == '"')
                {
                    ReadString();
                }
                else if (_json[_pos] == '{')
                {
                    int depth = 1;
                    _pos++;
                    while (depth > 0 && _pos < _json.Length)
                    {
                        if (_json[_pos] == '{') depth++;
                        else if (_json[_pos] == '}') depth--;
                        else if (_json[_pos] == '"') ReadString();
                        _pos++;
                    }
                }
                else if (_json[_pos] == '[')
                {
                    int depth = 1;
                    _pos++;
                    while (depth > 0 && _pos < _json.Length)
                    {
                        if (_json[_pos] == '[') depth++;
                        else if (_json[_pos] == ']') depth--;
                        else if (_json[_pos] == '"') ReadString();
                        _pos++;
                    }
                }
                else
                {
                    // Number, bool, null
                    while (_pos < _json.Length && 
                           _json[_pos] != ',' && 
                           _json[_pos] != '}' && 
                           _json[_pos] != ']')
                        _pos++;
                }
                SkipWhitespace();
            }
        }
    }
}

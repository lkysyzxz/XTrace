namespace ModelContextProtocol
{
    public enum McpErrorCode
    {
        ParseError = -32700,
        InvalidRequest = -32600,
        MethodNotFound = -32601,
        InvalidParams = -32602,
        InternalError = -32603,
        ServerNotInitialized = -32002,
        UnsupportedSseTransport = -32001,
        UrlElicitationRequired = -32003
    }
}

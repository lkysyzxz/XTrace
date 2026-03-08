using System;

namespace ModelContextProtocol
{
    public class McpException : Exception
    {
        public McpErrorCode ErrorCode { get; }

        public McpException(string message) : base(message)
        {
            ErrorCode = McpErrorCode.InternalError;
        }

        public McpException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = McpErrorCode.InternalError;
        }

        public McpException(McpErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public McpException(McpErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    public class McpProtocolException : McpException
    {
        public object Data { get; }

        public McpProtocolException(McpErrorCode errorCode, string message, object data = null) 
            : base(errorCode, message)
        {
            Data = data;
        }
    }
}

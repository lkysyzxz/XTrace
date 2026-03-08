using System.Threading;

namespace ModelContextProtocol.Protocol
{
    public class JsonRpcMessageContext
    {
        public ITransport RelatedTransport { get; set; }
        public System.Threading.ExecutionContext ExecutionContext { get; set; }
        
        public static JsonRpcMessageContext Create(ITransport transport)
        {
            return new JsonRpcMessageContext
            {
                RelatedTransport = transport,
                ExecutionContext = ExecutionContext.Capture()
            };
        }
    }
}

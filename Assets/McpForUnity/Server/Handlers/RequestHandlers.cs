using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ModelContextProtocol.Server
{
    public delegate Task<TResult> RequestHandler<TRequest, TResult>(TRequest request, CancellationToken cancellationToken) 
        where TRequest : class 
        where TResult : class;
    
    public delegate Task NotificationHandler<T>(T notification, CancellationToken cancellationToken) where T : class;

    public class RequestHandlers
    {
        private readonly Dictionary<string, object> _handlers = new Dictionary<string, object>();

        public void Set<TRequest, TResult>(string method, RequestHandler<TRequest, TResult> handler)
            where TRequest : class
            where TResult : class
        {
                _handlers[method] = handler;
            }

        public bool TryGet<TRequest, TResult>(string method, out RequestHandler<TRequest, TResult> handler)
            where TRequest : class
            where TResult : class
        {
                if (_handlers.TryGetValue(method, out var obj) && obj is RequestHandler<TRequest, TResult> h)
                {
                    handler = h;
                    return true;
                }
                handler = null;
                return false;
            }

        public bool Contains(string method) => _handlers.ContainsKey(method);
    }

    public class NotificationHandlers
    {
        private readonly Dictionary<string, object> _handlers = new Dictionary<string, object>();

        public void Add<T>(string method, NotificationHandler<T> handler) where T : class
        {
            if (_handlers.TryGetValue(method, out var existing))
            {
                if (existing is List<NotificationHandler<T>> list)
                {
                    list.Add(handler);
                }
            }
            else
            {
                _handlers[method] = new List<NotificationHandler<T>> { handler };
            }
        }

        public bool TryGet<T>(string method, out List<NotificationHandler<T>> handlers) where T : class
        {
            if (_handlers.TryGetValue(method, out var obj) && obj is List<NotificationHandler<T>> list)
            {
                handlers = list;
                return true;
            }
            handlers = null;
            return false;
        }
    }
}

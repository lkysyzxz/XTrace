using System;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server
{
    public class RequestContext
    {
        public McpServer Server { get; set; }
        public ProgressToken ProgressToken { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    public static class RequestContextExtensions
    {
        public static async Task ReportProgressAsync(this RequestContext context, double progress, double? total = null, string message = null)
        {
            if (context?.ProgressToken == null || context.Server == null)
            {
                return;
            }

            var notification = new ProgressNotificationParams
            {
                ProgressToken = context.ProgressToken,
                Progress = progress,
                Total = total,
                Message = message
            };

            await context.Server.SendNotificationAsync(NotificationMethods.ProgressNotification, notification, context.CancellationToken);
        }
    }
}

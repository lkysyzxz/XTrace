using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server
{
    public interface IMcpTaskStore
    {
        Task<McpTask> CreateTaskAsync(string type = null, McpTaskMetadata metadata = null, CancellationToken cancellationToken = default);
        Task<McpTask> GetTaskAsync(string taskId, CancellationToken cancellationToken = default);
        Task<List<McpTask>> ListTasksAsync(McpTaskStatus? status = null, CancellationToken cancellationToken = default);
        Task<McpTask> UpdateTaskStatusAsync(string taskId, McpTaskStatus status, CancellationToken cancellationToken = default);
        Task<McpTask> CompleteTaskAsync(string taskId, object result = null, CancellationToken cancellationToken = default);
        Task<McpTask> FailTaskAsync(string taskId, string error, CancellationToken cancellationToken = default);
        Task CancelTaskAsync(string taskId, string reason = null, CancellationToken cancellationToken = default);
        CancellationToken GetTaskCancellationToken(string taskId);
    }

    public class InMemoryMcpTaskStore : IMcpTaskStore
    {
        private readonly Dictionary<string, McpTaskRecord> _tasks = new Dictionary<string, McpTaskRecord>();
        private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new Dictionary<string, CancellationTokenSource>();
        private readonly ILogger _logger;

        public InMemoryMcpTaskStore(ILogger logger = null)
        {
            _logger = logger ?? new UnityLoggerImpl();
        }

        public Task<McpTask> CreateTaskAsync(string type = null, McpTaskMetadata metadata = null, CancellationToken cancellationToken = default)
        {
            var taskId = Guid.NewGuid().ToString("N");
            var cts = new CancellationTokenSource();
            var task = new McpTask
            {
                TaskId = taskId,
                Status = McpTaskStatus.Pending,
                Created = DateTime.UtcNow,
                Metadata = metadata ?? new McpTaskMetadata { Type = type }
            };

            lock (_tasks)
            {
                _tasks[taskId] = new McpTaskRecord { Task = task };
                _cancellationTokens[taskId] = cts;
            }

            _logger.Log(LogLevel.Debug, $"Task created: {taskId}");
            return Task.FromResult(task);
        }

        public Task<McpTask> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            lock (_tasks)
            {
                if (_tasks.TryGetValue(taskId, out var record))
                {
                    return Task.FromResult(record.Task);
                }
            }
            return Task.FromResult<McpTask>(null);
        }

        public Task<List<McpTask>> ListTasksAsync(McpTaskStatus? status = null, CancellationToken cancellationToken = default)
        {
            var result = new List<McpTask>();

            lock (_tasks)
            {
                foreach (var record in _tasks.Values)
                {
                    if (status == null || record.Task.Status == status.Value)
                    {
                        result.Add(record.Task);
                    }
                }
            }

            return Task.FromResult(result);
        }

        public Task<McpTask> UpdateTaskStatusAsync(string taskId, McpTaskStatus status, CancellationToken cancellationToken = default)
        {
            lock (_tasks)
            {
                if (_tasks.TryGetValue(taskId, out var record))
                {
                    record.Task.Status = status;
                    record.Task.Modified = DateTime.UtcNow;
                    _logger.Log(LogLevel.Debug, $"Task {taskId} status updated to {status}");
                    return Task.FromResult(record.Task);
                }
            }
            return Task.FromResult<McpTask>(null);
        }

        public Task<McpTask> CompleteTaskAsync(string taskId, object result = null, CancellationToken cancellationToken = default)
        {
            lock (_tasks)
            {
                if (_tasks.TryGetValue(taskId, out var record))
                {
                    record.Task.Status = McpTaskStatus.Completed;
                    record.Task.Modified = DateTime.UtcNow;
                    record.Result = result;
                    _logger.Log(LogLevel.Debug, $"Task {taskId} completed");
                    return Task.FromResult(record.Task);
                }
            }
            return Task.FromResult<McpTask>(null);
        }

        public Task<McpTask> FailTaskAsync(string taskId, string error, CancellationToken cancellationToken = default)
        {
            lock (_tasks)
            {
                if (_tasks.TryGetValue(taskId, out var record))
                {
                    record.Task.Status = McpTaskStatus.Failed;
                    record.Task.Modified = DateTime.UtcNow;
                    record.Error = error;
                    _logger.Log(LogLevel.Debug, $"Task {taskId} failed: {error}");
                    return Task.FromResult(record.Task);
                }
            }
            return Task.FromResult<McpTask>(null);
        }

        public Task CancelTaskAsync(string taskId, string reason = null, CancellationToken cancellationToken = default)
        {
            lock (_tasks)
            {
                if (_tasks.TryGetValue(taskId, out var record))
                {
                    record.Task.Status = McpTaskStatus.Cancelled;
                    record.Task.Modified = DateTime.UtcNow;

                    if (_cancellationTokens.TryGetValue(taskId, out var cts))
                    {
                        cts.Cancel();
                    }

                    _logger.Log(LogLevel.Debug, $"Task {taskId} cancelled: {reason}");
                }
            }

            return Task.CompletedTask;
        }

        public CancellationToken GetTaskCancellationToken(string taskId)
        {
            lock (_tasks)
            {
                if (_cancellationTokens.TryGetValue(taskId, out var cts))
                {
                    return cts.Token;
                }
            }
            return CancellationToken.None;
        }

        private class McpTaskRecord
        {
            public McpTask Task { get; set; }
            public object Result { get; set; }
            public string Error { get; set; }
        }
    }

    public static class McpTaskStoreExtensions
    {
        public static async Task<McpTask> RunAsTaskAsync(
            this IMcpTaskStore store,
            Func<CancellationToken, Task> action,
            string type = null,
            McpTaskMetadata metadata = null,
            CancellationToken cancellationToken = default)
        {
            var task = await store.CreateTaskAsync(type, metadata, cancellationToken);
            var taskToken = store.GetTaskCancellationToken(task.TaskId);

            _ = Task.Run(async () =>
            {
                try
                {
                    await store.UpdateTaskStatusAsync(task.TaskId, McpTaskStatus.Running);
                    await action(taskToken);
                    await store.CompleteTaskAsync(task.TaskId);
                }
                catch (OperationCanceledException)
                {
                    await store.CancelTaskAsync(task.TaskId);
                }
                catch (Exception ex)
                {
                    await store.FailTaskAsync(task.TaskId, ex.Message);
                }
            }, cancellationToken);

            return task;
        }

        public static async Task<McpTask> RunAsTaskAsync<T>(
            this IMcpTaskStore store,
            Func<CancellationToken, Task<T>> action,
            string type = null,
            McpTaskMetadata metadata = null,
            CancellationToken cancellationToken = default)
        {
            var task = await store.CreateTaskAsync(type, metadata, cancellationToken);
            var taskToken = store.GetTaskCancellationToken(task.TaskId);

            _ = Task.Run(async () =>
            {
                try
                {
                    await store.UpdateTaskStatusAsync(task.TaskId, McpTaskStatus.Running);
                    var result = await action(taskToken);
                    await store.CompleteTaskAsync(task.TaskId, result);
                }
                catch (OperationCanceledException)
                {
                    await store.CancelTaskAsync(task.TaskId);
                }
                catch (Exception ex)
                {
                    await store.FailTaskAsync(task.TaskId, ex.Message);
                }
            }, cancellationToken);

            return task;
        }
    }
}

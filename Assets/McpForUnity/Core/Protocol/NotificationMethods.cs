namespace ModelContextProtocol.Protocol
{
    public static class NotificationMethods
    {
        public const string ToolListChangedNotification = "notifications/tools/list_changed";
        public const string PromptListChangedNotification = "notifications/prompts/list_changed";
        public const string ResourceListChangedNotification = "notifications/resources/list_changed";
        public const string ResourceUpdatedNotification = "notifications/resources/updated";
        public const string RootsListChangedNotification = "notifications/roots/list_changed";
        public const string LoggingMessageNotification = "notifications/message";
        public const string ElicitationCompleteNotification = "notifications/elicitation/complete";
        public const string InitializedNotification = "notifications/initialized";
        public const string ProgressNotification = "notifications/progress";
        public const string CancelledNotification = "notifications/cancelled";
        public const string TaskStatusNotification = "notifications/tasks/status";
        public const string RelatedTaskMetaKey = "io.modelcontextprotocol/related-task";
    }
}

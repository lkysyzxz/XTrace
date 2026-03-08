namespace ModelContextProtocol.Protocol
{
    public static class RequestMethods
    {
        public const string ToolsList = "tools/list";
        public const string ToolsCall = "tools/call";
        public const string PromptsList = "prompts/list";
        public const string PromptsGet = "prompts/get";
        public const string ResourcesList = "resources/list";
        public const string ResourcesRead = "resources/read";
        public const string ResourcesTemplatesList = "resources/templates/list";
        public const string ResourcesSubscribe = "resources/subscribe";
        public const string ResourcesUnsubscribe = "resources/unsubscribe";
        public const string RootsList = "roots/list";
        public const string Ping = "ping";
        public const string LoggingSetLevel = "logging/setLevel";
        public const string CompletionComplete = "completion/complete";
        public const string SamplingCreateMessage = "sampling/createMessage";
        public const string ElicitationCreate = "elicitation/create";
        public const string Initialize = "initialize";
        public const string TasksGet = "tasks/get";
        public const string TasksResult = "tasks/result";
        public const string TasksList = "tasks/list";
        public const string TasksCancel = "tasks/cancel";
    }
}

#if UNITY_EDITOR
using UnityEditor;

namespace ModelContextProtocol.Editor
{
    public static class McpServerMenu
    {
        private const string MenuLaunch = "Tools/MCP For Unity/Launch Server";
        private const string MenuClose = "Tools/MCP For Unity/Close Server";
        private const string MenuResourcesEnable = "Tools/MCP For Unity/Resources/Enable";
        private const string MenuResourcesDisable = "Tools/MCP For Unity/Resources/Disable";

        [MenuItem(MenuLaunch)]
        public static void LaunchServer()
        {
            GlobalEditorMcpServer.StartServer();
        }

        [MenuItem(MenuLaunch, true)]
        public static bool ValidateLaunchServer()
        {
            return !GlobalEditorMcpServer.IsRunning;
        }

        [MenuItem(MenuClose)]
        public static void CloseServer()
        {
            GlobalEditorMcpServer.StopServer();
        }

        [MenuItem(MenuClose, true)]
        public static bool ValidateCloseServer()
        {
            return GlobalEditorMcpServer.IsRunning;
        }

        [MenuItem(MenuResourcesEnable)]
        public static void EnableResources()
        {
            GlobalEditorMcpServer.ResourcesEnabled = true;
        }

        [MenuItem(MenuResourcesEnable, true)]
        public static bool ValidateEnableResources()
        {
            return !GlobalEditorMcpServer.IsRunning && !GlobalEditorMcpServer.ResourcesEnabled;
        }

        [MenuItem(MenuResourcesDisable)]
        public static void DisableResources()
        {
            GlobalEditorMcpServer.ResourcesEnabled = false;
        }

        [MenuItem(MenuResourcesDisable, true)]
        public static bool ValidateDisableResources()
        {
            return !GlobalEditorMcpServer.IsRunning && GlobalEditorMcpServer.ResourcesEnabled;
        }

        private const string MenuFileWatchingEnable = "Tools/MCP For Unity/File Watching/Enable";
        private const string MenuFileWatchingDisable = "Tools/MCP For Unity/File Watching/Disable";

        [MenuItem(MenuFileWatchingEnable)]
        public static void EnableFileWatching()
        {
            GlobalEditorMcpServer.FileWatchingEnabled = true;
        }

        [MenuItem(MenuFileWatchingEnable, true)]
        public static bool ValidateEnableFileWatching()
        {
            return !GlobalEditorMcpServer.IsRunning && !GlobalEditorMcpServer.FileWatchingEnabled;
        }

        [MenuItem(MenuFileWatchingDisable)]
        public static void DisableFileWatching()
        {
            GlobalEditorMcpServer.FileWatchingEnabled = false;
        }

        [MenuItem(MenuFileWatchingDisable, true)]
        public static bool ValidateDisableFileWatching()
        {
            return !GlobalEditorMcpServer.IsRunning && GlobalEditorMcpServer.FileWatchingEnabled;
        }
    }
}
#endif

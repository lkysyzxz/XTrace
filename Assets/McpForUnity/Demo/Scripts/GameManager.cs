using System;
using System.Collections;
using System.Collections.Generic;
using ModelContextProtocol.Samples;
using ModelContextProtocol.Unity;
using UnityEngine;

namespace McpForUnity.Demo
{
    public class GameManager : MonoBehaviour
    {
        private McpServerHost _host;

        async void Start()
        {
            var options = new McpServerHostOptions
            {
                Port = 8090,
                ServerName = "UnityMCP"
            };

            _host = new McpServerHost(options);
            await _host.StartAsync();
            _host.Server.RegisterToolsFromClass(typeof(CustomTools));
        }

        private async void OnDestroy()
        {
            if (_host != null)
            {
                await _host.DisposeAsync();
            }
        }
    }
}
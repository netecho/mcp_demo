using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using dotenv.net;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mcp_client_demo
{
    /// <summary>
    /// Service for handling MCP client operations
    /// </summary>
    public class McpClientService
    {
        /// <summary>
        /// Creates and initializes an MCP client
        /// </summary>
        /// <returns>Initialized MCP client</returns>
        public static async Task<IMcpClient> GetMcpClientAsync()
        {
            DotEnv.Load();
            var envVars = DotEnv.Read();
            McpClientOptions options = new()
            {
                ClientInfo = new() { Name = "SimpleToolsConsole", Version = "1.0.0" }
            };

            var config = new McpServerConfig
            {
                Id = "test",
                Name = "Test",
                TransportType = TransportTypes.StdIo,
                TransportOptions = new Dictionary<string, string>
                {
                    ["command"] = envVars["MCPCommand"],
                    ["arguments"] = envVars["MCPArguments"],
                }
            };

            var factory = new McpClientFactory(
                new[] { config },
                options,
                NullLoggerFactory.Instance
            );

            return await factory.GetClientAsync("test");
        }
    }
} 
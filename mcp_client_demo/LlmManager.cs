using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Extensions.AI;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using dotenv.net;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mcp_client_demo
{
    public class LlmManager
    {
        private static async Task<IMcpClient> GetMcpClientAsync()
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

        public static async Task<(IMcpClient client, List<AITool> tools)> InitializeLlmAsync()
        {
            Console.WriteLine("Initializing MCP 'fetch' server");
            var client = await GetMcpClientAsync();
            Console.WriteLine("MCP 'everything' server initialized");
            Console.WriteLine("Listing tools...");
            var listToolsResult = await client.ListToolsAsync();
            var mappedTools = listToolsResult.Tools.Select(t => t.ToAITool(client)).ToList();
            return (client, mappedTools);
        }

        public static void DisplayAvailableTools(List<AITool> tools)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n🧰 可用工具列表");
            Console.WriteLine("========================================");
            
            foreach (var tool in tools)
            {
                string name = "未知工具";
                string description = "无描述";
                object parameters = null;
                
                // 使用反射安全地获取属性
                try {
                    var nameProperty = tool.GetType().GetProperty("Name");
                    if (nameProperty != null)
                        name = nameProperty.GetValue(tool)?.ToString() ?? "未知工具";
                    
                    var descProperty = tool.GetType().GetProperty("Description");
                    if (descProperty != null)
                        description = descProperty.GetValue(tool)?.ToString() ?? "无描述";
                    
                    var paramsProperty = tool.GetType().GetProperty("Parameters");
                    if (paramsProperty != null)
                        parameters = paramsProperty.GetValue(tool);
                } catch { }
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"📌 {name}");
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"   描述: {description}");
                
                try
                {
                    if (parameters != null)
                    {
                        // 尝试获取Properties属性
                        object properties = null;
                        try {
                            var propsProperty = parameters.GetType().GetProperty("Properties");
                            if (propsProperty != null)
                                properties = propsProperty.GetValue(parameters);
                        } catch { }
                        
                        if (properties != null)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("   参数:");
                            
                            // 尝试将properties转换为IDictionary
                            try {
                                var dict = properties as System.Collections.IDictionary;
                                if (dict != null)
                                {
                                    foreach (System.Collections.DictionaryEntry entry in dict)
                                    {
                                        string key = entry.Key?.ToString() ?? "未知参数";
                                        string paramDesc = "无描述";
                                        
                                        try {
                                            var descProp = entry.Value?.GetType().GetProperty("Description");
                                            if (descProp != null)
                                                paramDesc = descProp.GetValue(entry.Value)?.ToString() ?? "无描述";
                                        } catch { }
                                        
                                        Console.WriteLine($"     - {key}: {paramDesc}");
                                    }
                                }
                                else
                                {
                                    // 尝试使用反射获取枚举器
                                    var enumerableType = properties.GetType();
                                    var getEnumeratorMethod = enumerableType.GetMethod("GetEnumerator");
                                    if (getEnumeratorMethod != null)
                                    {
                                        var enumerator = getEnumeratorMethod.Invoke(properties, null);
                                        var moveNextMethod = enumerator.GetType().GetMethod("MoveNext");
                                        var currentProperty = enumerator.GetType().GetProperty("Current");
                                        
                                        while ((bool)moveNextMethod.Invoke(enumerator, null))
                                        {
                                            var current = currentProperty.GetValue(enumerator);
                                            var keyProp = current.GetType().GetProperty("Key");
                                            var valueProp = current.GetType().GetProperty("Value");
                                            
                                            if (keyProp != null && valueProp != null)
                                            {
                                                var key = keyProp.GetValue(current)?.ToString() ?? "未知参数";
                                                var value = valueProp.GetValue(current);
                                                string paramDesc = "无描述";
                                                
                                                try {
                                                    var descProp = value?.GetType().GetProperty("Description");
                                                    if (descProp != null)
                                                        paramDesc = descProp.GetValue(value)?.ToString() ?? "无描述";
                                                } catch { }
                                                
                                                Console.WriteLine($"     - {key}: {paramDesc}");
                                            }
                                        }
                                    }
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"     无法遍历参数: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   无法获取参数信息: {ex.Message}");
                }
                
                Console.WriteLine("----------------------------------------");
            }
            
            Console.WriteLine($"共 {tools.Count} 个工具可用");
            Console.WriteLine("========================================");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
} 
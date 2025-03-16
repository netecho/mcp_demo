using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Extensions.AI;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.AI;
//using Microsoft.Extensions.AI.PromptExecutor;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Linq;
using dotenv.net;
using System.Dynamic;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace mcp_client_demo
{
    internal class ChatDemo
    {
        public ChatDemo() 
        {
            InitIChatClient();
        }

        public IChatClient ChatClient;
        public IList<Microsoft.Extensions.AI.ChatMessage> Messages;
        public bool DebugMode { get; set; } = false;
        public bool ShowRawMode { get; set; } = false;
        
        // Add a method to display conversation history
        public void DisplayConversationHistory()
        {
            if (Messages.Count <= 1) // Only system message or empty
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("没有对话历史记录。");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n📜 对话历史记录");
            Console.WriteLine("========================================");
            
            for (int i = 1; i < Messages.Count; i++) // Skip system message
            {
                var message = Messages[i];
                
                if (message.Role == ChatRole.User)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"🧑 用户: {message.Text}");
                }
                else if (message.Role == ChatRole.Assistant)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"🤖 AI: {message.Text}");
                }
                else if (message.Role == ChatRole.Tool)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    var functionResultContent = message.Contents.FirstOrDefault() as FunctionResultContent;
                    if (functionResultContent != null)
                    {
                        // 使用反射获取属性
                        string toolName = "未知工具";
                        string result = "";
                        
                        try {
                            var nameProperty = functionResultContent.GetType().GetProperty("Name");
                            if (nameProperty != null)
                                toolName = nameProperty.GetValue(functionResultContent)?.ToString() ?? "未知工具";
                            
                            var resultProperty = functionResultContent.GetType().GetProperty("Result");
                            if (resultProperty != null)
                                result = resultProperty.GetValue(functionResultContent)?.ToString() ?? "";
                        } catch { }
                        
                        Console.WriteLine($"🔧 工具 ({toolName}): {result}");
                    }
                }
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("----------------------------------------");
            }
            
            Console.WriteLine("========================================");
            Console.ForegroundColor = ConsoleColor.White;
        }
        
        private void InitIChatClient()
        {
            DotEnv.Load();
            var envVars = DotEnv.Read();
            ApiKeyCredential apiKeyCredential = new ApiKeyCredential(envVars["API_KEY"]);

            OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions();
            openAIClientOptions.Endpoint = new Uri(envVars["BaseURL"]);

            IChatClient openaiClient = new OpenAIClient(apiKeyCredential, openAIClientOptions)
                .AsChatClient(envVars["ModelID"]);

            // Note: To use the ChatClientBuilder you need to install the Microsoft.Extensions.AI package
            ChatClient = new ChatClientBuilder(openaiClient)
                .UseFunctionInvocation()
                .Build();

            Messages =
            [
                // Add a system message
                new(ChatRole.System, "You are a helpful assistant, helping us test MCP server functionality."),
            ];
        }

        public async Task<string> ProcessQueryAsync(string query, List<AITool> tools)
        {
            if(Messages.Count == 0)
            {
                Messages =
                [
                 // Add a system message
                new(ChatRole.System, "You are a helpful assistant, helping us test MCP server functionality."),
                ];
            }
            
            // Add a user message
            Messages.Add(new(ChatRole.User, query));
            
            // 显示发送给大模型的原始信息
            if (ShowRawMode)
            {
                ToolUtil.DisplayRawMessages(Messages, tools);
                
                // 尝试将整个对话序列化为JSON
                ToolUtil.TrySerializeConversationToJson(Messages, tools);
            }
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] 发送请求到模型...");
            Console.ForegroundColor = ConsoleColor.White;
            
            var startTime = DateTime.Now;
            var response = await ChatClient.GetResponseAsync(
                   Messages,
                   new() { Tools = tools });
            var endTime = DateTime.Now;
            var duration = (endTime - startTime).TotalSeconds;
            
            // 显示模型返回的原始响应信息
            if (ShowRawMode)
            {
                ToolUtil.DisplayRawResponse(response);
                
                // 尝试将响应序列化为JSON
                ToolUtil.TrySerializeResponseToJson(response);
            }
            
            // 只在调试模式下输出ChatResponse对象的结构
            if (DebugMode)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("\n[调试信息]");
                ToolUtil.DebugOutputResponseStructure(response);
                Console.ForegroundColor = ConsoleColor.White;
            }
            
            Messages.AddMessages(response);
            var toolUseMessage = response.Messages.Where(m => m.Role == ChatRole.Tool);
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到响应 (耗时: {duration:F2}秒)");
            Console.WriteLine("----------------------------------------");
            
            // Display conversation details
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"消息数量: {response.Messages.Count}");
            
            if (response.Messages[0].Contents.Count > 1)
            {
                //var functionMessage = response.Messages.Where(m => (m.Role == ChatRole.Assistant && m.Text == "")).Last();
                var functionCall = (FunctionCallContent)response.Messages[0].Contents[1];
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n📞 工具调用信息:");
                string arguments = "";
                
                // 使用反射获取属性
                string functionName = "未知函数";
                IDictionary<string, object> args = null;
                
                try {
                    var nameProperty = functionCall.GetType().GetProperty("Name");
                    if (nameProperty != null)
                        functionName = nameProperty.GetValue(functionCall)?.ToString() ?? "未知函数";
                    
                    var argsProperty = functionCall.GetType().GetProperty("Arguments");
                    if (argsProperty != null)
                        args = argsProperty.GetValue(functionCall) as IDictionary<string, object>;
                } catch { }
                
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        arguments += $"\n   - {arg.Key}: {arg.Value}";
                    }
                    Console.WriteLine($" ▶ 函数名称: {functionName}");
                    Console.WriteLine($" ▶ 参数信息: {arguments}");
                    
                    if (toolUseMessage.Any())
                    {
                        Console.WriteLine("\n🔄 工具执行结果:");
                        foreach (var message in toolUseMessage)
                        {
                            var functionResultContent = (FunctionResultContent)message.Contents[0];
                            
                            // 使用反射获取属性
                            string toolName = "未知工具";
                            string result = "";
                            
                            try {
                                var nameProperty = functionResultContent.GetType().GetProperty("Name");
                                if (nameProperty != null)
                                    toolName = nameProperty.GetValue(functionResultContent)?.ToString() ?? "未知工具";
                                
                                var resultProperty = functionResultContent.GetType().GetProperty("Result");
                                if (resultProperty != null)
                                    result = resultProperty.GetValue(functionResultContent)?.ToString() ?? "";
                            } catch { }
                            
                            Console.WriteLine($" ▶ 工具名称: {toolName}");
                            Console.WriteLine($" ▶ 执行结果: {result}");
                        }
                    }
                    
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.WriteLine(" ▶ 调用工具参数缺失");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("📝 本次对话没有调用工具");               
            }
            
            // Display token usage if available
            try
            {
                if (response.Usage != null)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("\n📊 Token 使用情况:");
                    
                    // 尝试多种方式获取Token使用情况
                    var (promptTokens, completionTokens, totalTokens) = ToolUtil.GetTokenUsage(response);
                    
                    Console.WriteLine($" ▶ 输入 Tokens: {promptTokens}");
                    Console.WriteLine($" ▶ 输出 Tokens: {completionTokens}");
                    Console.WriteLine($" ▶ 总计 Tokens: {totalTokens}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n无法获取Token使用情况: {ex.Message}");
                
                // 输出Usage对象的类型和内容以便调试
                if (response.Usage != null && DebugMode)
                {
                    Console.WriteLine($"Usage类型: {response.Usage.GetType().FullName}");
                    Console.WriteLine($"Usage内容: {response.Usage}");
                }
            }
            
            Console.WriteLine("----------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            return response.Text;
        }
    }
    internal class Program
    {
        async static Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;  // 设置输出编码
            Console.InputEncoding = System.Text.Encoding.UTF8;   // 设置输入编码
            Console.ForegroundColor = ConsoleColor.White;
            
            var (client, mappedTools) = await LlmManager.InitializeLlmAsync();
            
            // Display available tools with better formatting
            LlmManager.DisplayAvailableTools(mappedTools);

            Console.WriteLine("\nMCP Client Started!");
            Console.WriteLine("Type your queries or 'quit' to exit.");
            Console.WriteLine("Type 'clear' to clear the conversation history.");
            Console.WriteLine("Type 'history' to view conversation history.");
            Console.WriteLine("Type 'tools' to view available tools.");
            Console.WriteLine("Type 'debug' to toggle debug mode.");
            Console.WriteLine("Type 'raw' to toggle raw message display mode.");

            ChatDemo chatDemo = new ChatDemo();

            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("\n🧑 Query: ");
                    string query = Console.ReadLine()?.Trim() ?? string.Empty;

                    if (query.ToLower() == "quit")
                        break;
                    else if (query.ToLower() == "clear")
                    {
                        Console.Clear();
                        chatDemo.Messages.Clear();
                        Console.WriteLine("会话历史已清除。");
                        Console.WriteLine("Type your queries or 'quit' to exit.");
                        Console.WriteLine("Type 'clear' to clear the conversation history.");
                        Console.WriteLine("Type 'history' to view conversation history.");
                        Console.WriteLine("Type 'tools' to view available tools.");
                        Console.WriteLine("Type 'debug' to toggle debug mode.");
                        Console.WriteLine("Type 'raw' to toggle raw message display mode.");
                    }
                    else if (query.ToLower() == "history")
                    {
                        chatDemo.DisplayConversationHistory();
                    }
                    else if (query.ToLower() == "tools")
                    {
                        LlmManager.DisplayAvailableTools(mappedTools);
                    }
                    else if (query.ToLower() == "debug")
                    {
                        chatDemo.DebugMode = !chatDemo.DebugMode;
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"调试模式: {(chatDemo.DebugMode ? "开启" : "关闭")}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else if (query.ToLower() == "raw")
                    {
                        chatDemo.ShowRawMode = !chatDemo.ShowRawMode;
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"原始信息显示模式: {(chatDemo.ShowRawMode ? "开启" : "关闭")}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else 
                    {
                        string response = await chatDemo.ProcessQueryAsync(query, mappedTools);
                        
                        // Display AI response with better formatting
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("\n🤖 AI回答:");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("----------------------------------------");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        
                        // Format the response with proper line breaks and indentation
                        string[] responseLines = response.Split('\n');
                        foreach (var line in responseLines)
                        {
                            Console.WriteLine($"  {line}");
                        }
                        
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("----------------------------------------");
                    }                      
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n❌ Error: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
    }
}

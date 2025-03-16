using Microsoft.Extensions.AI;
//using Microsoft.Extensions.AI.PromptExecutor;
using OpenAI;
using System.ClientModel;
using dotenv.net;

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
        public string ModelID { get; private set; }
        public string ApiEndpoint { get; private set; }
        
        // Add a method to display model information
        public void DisplayModelInfo()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n🤖 模型信息");
            Console.WriteLine("========================================");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"📌 模型ID: {ModelID}");
            Console.WriteLine($"🔗 API地址: {ApiEndpoint}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("========================================");
        }
        
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
            
            // Store model ID and API endpoint
            ModelID = envVars["ModelID"];
            ApiEndpoint = envVars["BaseURL"];

            IChatClient openaiClient = new OpenAIClient(apiKeyCredential, openAIClientOptions)
                .AsChatClient(ModelID);

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
            Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] 发送请求到模型 ({ModelID})...");
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
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到响应 (耗时: {duration:F2}秒)");
            Console.WriteLine("----------------------------------------");
            
            // Display conversation details
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"消息数量: {response.Messages.Count}");




            Messages.AddMessages(response);
            var toolUseMessage = response.Messages.Where(m => m.Role == ChatRole.Tool);
            if (response.Messages[0].Contents.Count > 1)
            {
                //var functionMessage = response.Messages.Where(m => (m.Role == ChatRole.Assistant && m.Text == "")).Last();
                var functionCall = (FunctionCallContent)response.Messages[0].Contents[1];
                Console.ForegroundColor = ConsoleColor.Green;
                string arguments = "";
                if (functionCall.Arguments != null)
                {
                    foreach (var arg in functionCall.Arguments)
                    {
                        arguments += $"{arg.Key}:{arg.Value};";
                    }
                    Console.WriteLine($"调用函数名:{functionCall.Name};参数信息：{arguments}");
                    foreach (var message in toolUseMessage)
                    {
                        var functionResultContent = (FunctionResultContent)message.Contents[0];
                        Console.WriteLine($"调用工具结果：{functionResultContent.Result}");
                    }

                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.WriteLine("调用工具参数缺失");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("本次没有调用工具");
            }
            Console.ForegroundColor = ConsoleColor.White;
            return response.Text;

        }
    }
}

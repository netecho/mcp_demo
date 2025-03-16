using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Extensions.AI;
using McpDotNet.Protocol.Transport;
//using Microsoft.Extensions.AI.PromptExecutor;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Chat;
using System.Linq;
using System.Dynamic;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace mcp_client_demo
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;  // 设置输出编码
            Console.InputEncoding = System.Text.Encoding.UTF8;   // 设置输入编码
            Console.ForegroundColor = ConsoleColor.White;
            
            var (client, mappedTools) = await MCPManager.InitializeMCPAsync();
            
            // Display available tools with better formatting
            MCPManager.DisplayAvailableTools(mappedTools);

            Console.WriteLine("\nMCP Client Started!");
            Console.WriteLine("Type your queries or 'quit' to exit.");
            Console.WriteLine("Type 'clear' to clear the conversation history.");
            Console.WriteLine("Type 'history' to view conversation history.");
            Console.WriteLine("Type 'tools' to view available tools.");
            Console.WriteLine("Type 'debug' to toggle debug mode.");
            Console.WriteLine("Type 'raw' to toggle raw message display mode.");
            Console.WriteLine("Type 'model' to view model information.");
            Console.WriteLine("Type 'interactions' to view interaction count.");

            ChatDemo chatDemo = new ChatDemo();
            
            // Display model information at startup
            chatDemo.DisplayModelInfo();

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
                        Console.WriteLine("Type 'model' to view model information.");
                        Console.WriteLine("Type 'interactions' to view interaction count.");
                    }
                    else if (query.ToLower() == "history")
                    {
                        chatDemo.DisplayConversationHistory();
                    }
                    else if (query.ToLower() == "tools")
                    {
                        MCPManager.DisplayAvailableTools(mappedTools);
                    }
                    else if (query.ToLower() == "model")
                    {
                        chatDemo.DisplayModelInfo();
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

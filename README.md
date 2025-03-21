# 使用C#创建一个MCP客户端

## 前言

网上使用Python创建一个MCP客户端的教程已经有很多了，而使用C#创建一个MCP客户端的教程还很少。

为什么要创建一个MCP客户端呢？

创建了一个MCP客户端之后，你就可以使用别人写好的一些MCP服务了。

## 效果展示

为了方便大家复现，我没有使用WPF/Avalonia之类的做界面。只是一个简单的控制台程序，可以很容易看懂。

![image-20250314173410130](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/image-20250314173410130.png)

接入了fetch_mcp可以实现获取网页内容了，使用的模型只要具有tool use能力的应该都可以。

我使用的是Qwen/Qwen2.5-72B-Instruct。

## 开始实践

主要使用的包如下所示：

![image-20250314173634157](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/image-20250314173634157.png)

首先获取MCP服务器：

```csharp
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
```

写死的话就是这样写：

```csharp
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
             ["command"] = node,
             ["arguments"] = D:/Learning/AI-related/fetch-mcp/dist/index.js,
         }
     };

     var factory = new McpClientFactory(
         new[] { config },
         options,
         NullLoggerFactory.Instance
     );

     return await factory.GetClientAsync("test");
 }
```

重点在：

```csharp
 TransportOptions = new Dictionary<string, string>
         {
             ["command"] = node,
             ["arguments"] = D:/Learning/AI-related/fetch-mcp/dist/index.js,
         }
```

用于连接你想连接的MCP服务器。

如果能正确显示你连接mcp服务器提供的工具，说明连接成功。

```csharp
  var listToolsResult = await client.ListToolsAsync();
  var mappedTools = listToolsResult.Tools.Select(t => t.ToAITool(client)).ToList();
  Console.WriteLine("Tools available:");
  foreach (var tool in mappedTools)
  {
      Console.WriteLine("  " + tool);
  }
```

![image-20250314174210161](https://mingupupup.oss-cn-wuhan-lr.aliyuncs.com/imgs/image-20250314174210161.png)

开启一个聊天循环：

```csharp
    Console.WriteLine("\nMCP Client Started!");
    Console.WriteLine("Type your queries or 'quit' to exit.");

    ChatDemo chatDemo = new ChatDemo();

    while (true)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("\nQuery: ");
            string query = Console.ReadLine()?.Trim() ?? string.Empty;

            if (query.ToLower() == "quit")
                break;
            if (query.ToLower() == "clear")
            {
                Console.Clear();
                chatDemo.Messages.Clear();                    
            }
            else 
            {
                string response = await chatDemo.ProcessQueryAsync(query, mappedTools);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"AI回答：{response}");
                Console.ForegroundColor = ConsoleColor.White;
            }                      
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }
}
```

处理每次询问：

```csharp
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

     var response = await ChatClient.GetResponseAsync(
            Messages,
            new() { Tools = tools });
     Messages.AddMessages(response);
     var toolUseMessage = response.Messages.Where(m => m.Role == ChatRole.Tool);

     if (toolUseMessage.Count() > 0)
     {
         var functionMessage = response.Messages.Where(m => m.Text == "").First();             
         var functionCall = (FunctionCallContent)functionMessage.Contents[1];
         Console.ForegroundColor = ConsoleColor.Green;
         string arguments = "";
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
         Console.ForegroundColor = ConsoleColor.Green;
         Console.WriteLine("本次没有调用工具");
         Console.ForegroundColor = ConsoleColor.White;
     }

     return response.Text;
 }
```

代码已经放到GitHub，地址：https://github.com/Ming-jiayou/mcp_demo。

将.env-example修改为.env应该就可以运行，如果报错，设置成嵌入的资源即可。

.env配置示例：

```csharp
API_KEY=sk-xxx
BaseURL=https://api.siliconflow.cn/v1
ModelID=Qwen/Qwen2.5-72B-Instruct
MCPCommand=node
MCPArguments=D:/Learning/AI-related/fetch-mcp/dist/index.js
```

## 最后

对C#使用MCP感兴趣的朋友可以关注这个项目：https://github.com/PederHP/mcpdotnet。

有问题欢迎一起交流学习。

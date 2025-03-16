using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.AI;
using McpDotNet.Protocol.Transport;

namespace mcp_client_demo
{
    internal static class ToolUtil
    {
        // 辅助方法：显示发送给大模型的原始信息
        public static void DisplayRawMessages(IList<Microsoft.Extensions.AI.ChatMessage> messages, List<AITool> tools)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n📋 发送给大模型的原始信息:");
            Console.WriteLine("========================================");
            
            // 显示消息
            Console.WriteLine("\n📨 消息列表:");
            foreach (var message in messages)
            {
                Console.WriteLine($"角色: {message.Role}");
                
                if (message.Contents.Count > 0)
                {
                    Console.WriteLine("内容:");
                    foreach (var content in message.Contents)
                    {
                        string contentType = content.GetType().Name;
                        Console.WriteLine($"  类型: {contentType}");
                        
                        if (content is TextContent textContent)
                        {
                            Console.WriteLine($"  文本: {textContent.Text}");
                        }
                        else if (content is FunctionCallContent functionCallContent)
                        {
                            try
                            {
                                // 使用反射获取属性
                                var nameProperty = functionCallContent.GetType().GetProperty("Name");
                                var argsProperty = functionCallContent.GetType().GetProperty("Arguments");
                                
                                string name = nameProperty?.GetValue(functionCallContent)?.ToString() ?? "未知函数";
                                Console.WriteLine($"  函数名: {name}");
                                
                                var args = argsProperty?.GetValue(functionCallContent) as IDictionary<string, object>;
                                if (args != null && args.Count > 0)
                                {
                                    Console.WriteLine("  参数:");
                                    foreach (var arg in args)
                                    {
                                        Console.WriteLine($"    {arg.Key}: {arg.Value}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("  参数: 无");
                                }
                            }
                            catch
                            {
                                Console.WriteLine($"  内容: {content}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  内容: {content}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"文本: {message.Text}");
                }
                
                Console.WriteLine("----------------------------------------");
            }
            
            // 显示工具信息
            Console.WriteLine("\n🔧 可用工具:");
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                foreach (var tool in tools)
                {
                    try
                    {
                        // 使用反射获取属性
                        var nameProperty = tool.GetType().GetProperty("Name");
                        var descProperty = tool.GetType().GetProperty("Description");
                        var paramsProperty = tool.GetType().GetProperty("Parameters");
                        
                        string name = nameProperty?.GetValue(tool)?.ToString() ?? "未知工具";
                        string description = descProperty?.GetValue(tool)?.ToString() ?? "无描述";
                        
                        Console.WriteLine($"工具名称: {name}");
                        Console.WriteLine($"描述: {description}");
                        
                        // 尝试序列化参数
                        try
                        {
                            var parameters = paramsProperty?.GetValue(tool);
                            if (parameters != null)
                            {
                                Console.WriteLine("参数:");
                                
                                // 尝试获取Properties属性
                                var propsProperty = parameters.GetType().GetProperty("Properties");
                                var properties = propsProperty?.GetValue(parameters);
                                
                                if (properties != null)
                                {
                                    // 尝试将properties转换为IDictionary
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
                                            
                                            Console.WriteLine($"  - {key}: {paramDesc}");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("  无法遍历参数");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("  无参数属性");
                                }
                            }
                            else
                            {
                                Console.WriteLine("参数: 无");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"参数: 无法序列化 ({ex.Message})");
                        }
                        
                        Console.WriteLine("----------------------------------------");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"工具: {tool}");
                        Console.WriteLine($"错误: {ex.Message}");
                        Console.WriteLine("----------------------------------------");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法显示工具信息: {ex.Message}");
            }
            
            Console.WriteLine("========================================");
            Console.ForegroundColor = ConsoleColor.White;
        }

        // 辅助方法：显示模型返回的原始响应信息
        public static void DisplayRawResponse(ChatResponse response)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n📥 模型返回的原始响应信息:");
            Console.WriteLine("========================================");
            
            // 显示响应消息
            Console.WriteLine("\n📨 响应消息列表:");
            foreach (var message in response.Messages)
            {
                Console.WriteLine($"角色: {message.Role}");
                
                if (message.Contents.Count > 0)
                {
                    Console.WriteLine("内容:");
                    foreach (var content in message.Contents)
                    {
                        string contentType = content.GetType().Name;
                        Console.WriteLine($"  类型: {contentType}");
                        
                        if (content is TextContent textContent)
                        {
                            Console.WriteLine($"  文本: {textContent.Text}");
                        }
                        else if (content is FunctionCallContent functionCallContent)
                        {
                            try
                            {
                                // 使用反射获取属性
                                var nameProperty = functionCallContent.GetType().GetProperty("Name");
                                var argsProperty = functionCallContent.GetType().GetProperty("Arguments");
                                
                                string name = nameProperty?.GetValue(functionCallContent)?.ToString() ?? "未知函数";
                                Console.WriteLine($"  函数名: {name}");
                                
                                var args = argsProperty?.GetValue(functionCallContent) as IDictionary<string, object>;
                                if (args != null && args.Count > 0)
                                {
                                    Console.WriteLine("  参数:");
                                    foreach (var arg in args)
                                    {
                                        Console.WriteLine($"    {arg.Key}: {arg.Value}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("  参数: 无");
                                }
                            }
                            catch
                            {
                                Console.WriteLine($"  内容: {content}");
                            }
                        }
                        else if (content is FunctionResultContent functionResultContent)
                        {
                            try
                            {
                                // 使用反射获取属性
                                var nameProperty = functionResultContent.GetType().GetProperty("Name");
                                var resultProperty = functionResultContent.GetType().GetProperty("Result");
                                
                                string name = nameProperty?.GetValue(functionResultContent)?.ToString() ?? "未知函数";
                                string result = resultProperty?.GetValue(functionResultContent)?.ToString() ?? "";
                                
                                Console.WriteLine($"  函数名: {name}");
                                Console.WriteLine($"  结果: {result}");
                            }
                            catch
                            {
                                Console.WriteLine($"  内容: {content}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  内容: {content}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"文本: {message.Text}");
                }
                
                Console.WriteLine("----------------------------------------");
            }
            
            // 显示Token使用情况
            if (response.Usage != null)
            {
                Console.WriteLine("\n📊 Token使用情况:");
                Console.WriteLine($"原始Usage对象: {response.Usage}");
                
                try
                {
                    // 使用反射获取Usage属性
                    var type = response.Usage.GetType();
                    var properties = type.GetProperties();
                    
                    if (properties.Length > 0)
                    {
                        Console.WriteLine("Usage属性:");
                        foreach (var prop in properties)
                        {
                            try
                            {
                                var value = prop.GetValue(response.Usage);
                                Console.WriteLine($"  {prop.Name}: {value}");
                            }
                            catch
                            {
                                Console.WriteLine($"  {prop.Name}: 无法访问");
                            }
                        }
                    }
                    else
                    {
                        // 尝试序列化为JSON
                        var usageJson = JsonSerializer.Serialize(response.Usage, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine($"Usage JSON: {usageJson}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法序列化Usage对象: {ex.Message}");
                }
            }
            
            Console.WriteLine("========================================");
            Console.ForegroundColor = ConsoleColor.White;
        }

        // 辅助方法：尝试将整个对话序列化为JSON
        public static void TrySerializeConversationToJson(IList<Microsoft.Extensions.AI.ChatMessage> messages, List<AITool> tools)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\n📄 对话的JSON表示:");
                
                // 创建一个匿名对象来表示对话
                var conversation = new
                {
                    Messages = messages.Select(m => new
                    {
                        Role = m.Role.ToString(),
                        Text = m.Text,
                        Contents = m.Contents.ToList().ConvertAll(c => 
                        {
                            var type = c.GetType().Name;
                            if (c is TextContent textContent)
                            {
                                return new { Type = type, Text = textContent.Text } as object;
                            }
                            return new { Type = type, ToString = c.ToString() } as object;
                        })
                    }).ToList(),
                    
                    Tools = tools.Select(t => 
                    {
                        try
                        {
                            // 使用反射获取属性
                            var nameProperty = t.GetType().GetProperty("Name");
                            var descProperty = t.GetType().GetProperty("Description");
                            
                            string name = nameProperty?.GetValue(t)?.ToString() ?? "未知工具";
                            string description = descProperty?.GetValue(t)?.ToString() ?? "无描述";
                            
                            return new
                            {
                                Name = name,
                                Description = description,
                                Parameters = "无法序列化"
                            } as object;
                        }
                        catch
                        {
                            return new
                            {
                                Name = "未知",
                                Description = "未知",
                                ToString = t.ToString()
                            } as object;
                        }
                    }).ToList()
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    MaxDepth = 10
                };
                
                string json = JsonSerializer.Serialize(conversation, options);
                Console.WriteLine(json);
                
                Console.WriteLine("----------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"无法序列化对话: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        // 辅助方法：尝试将响应序列化为JSON
        public static void TrySerializeResponseToJson(ChatResponse response)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\n📄 响应的JSON表示:");
                
                // 创建一个匿名对象来表示响应
                var responseObj = new
                {
                    Text = response.Text,
                    Messages = response.Messages.Select(m => new
                    {
                        Role = m.Role.ToString(),
                        Text = m.Text,
                        Contents = m.Contents.ToList().ConvertAll(c => 
                        {
                            var type = c.GetType().Name;
                            if (c is TextContent textContent)
                            {
                                return new { Type = type, Text = textContent.Text } as object;
                            }
                            return new { Type = type, ToString = c.ToString() } as object;
                        })
                    }).ToList(),
                    
                    Usage = response.Usage != null ? new { ToString = response.Usage.ToString() } : null
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    MaxDepth = 10
                };
                
                string json = JsonSerializer.Serialize(responseObj, options);
                Console.WriteLine(json);
                
                Console.WriteLine("----------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"无法序列化响应: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        // 辅助方法：输出对象结构
        public static void DebugOutputResponseStructure(object obj, string prefix = "")
        {
            if (obj == null)
            {
                Console.WriteLine($"{prefix}null");
                return;
            }

            var type = obj.GetType();
            Console.WriteLine($"{prefix}类型: {type.FullName}");

            // 获取所有公共属性
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    if (value == null)
                    {
                        Console.WriteLine($"{prefix}{prop.Name}: null");
                    }
                    else if (prop.Name == "Usage")
                    {
                        Console.WriteLine($"{prefix}{prop.Name}: {value}");
                        // 深入检查Usage对象
                        Console.WriteLine($"{prefix}  Usage类型: {value.GetType().FullName}");
                        foreach (var usageProp in value.GetType().GetProperties())
                        {
                            try
                            {
                                var usageValue = usageProp.GetValue(value);
                                Console.WriteLine($"{prefix}  {usageProp.Name}: {usageValue}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"{prefix}  {usageProp.Name}: 无法访问 ({ex.Message})");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{prefix}{prop.Name}: {value}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{prefix}{prop.Name}: 无法访问 ({ex.Message})");
                }
            }
        }

        // 辅助方法：使用反射获取属性值
        public static T GetPropertyValue<T>(object obj, string propName, T defaultValue)
        {
            if (obj == null) return defaultValue;
            
            try
            {
                var prop = obj.GetType().GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    if (value != null)
                        return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch { }
            
            return defaultValue;
        }

        // 辅助方法：尝试多种方式获取Token使用情况
        public static (int promptTokens, int completionTokens, int totalTokens) GetTokenUsage(ChatResponse response)
        {
            int promptTokens = 0;
            int completionTokens = 0;
            int totalTokens = 0;
            
            if (response.Usage == null)
                return (promptTokens, completionTokens, totalTokens);
            
            // 方法1: 尝试通过反射获取属性
            try
            {
                var type = response.Usage.GetType();
                
                var ptProp = type.GetProperty("PromptTokens");
                if (ptProp != null)
                {
                    var value = ptProp.GetValue(response.Usage);
                    if (value != null)
                        promptTokens = Convert.ToInt32(value);
                }
                
                var ctProp = type.GetProperty("CompletionTokens");
                if (ctProp != null)
                {
                    var value = ctProp.GetValue(response.Usage);
                    if (value != null)
                        completionTokens = Convert.ToInt32(value);
                }
                
                var ttProp = type.GetProperty("TotalTokens");
                if (ttProp != null)
                {
                    var value = ttProp.GetValue(response.Usage);
                    if (value != null)
                        totalTokens = Convert.ToInt32(value);
                }
                
                if (promptTokens > 0 || completionTokens > 0 || totalTokens > 0)
                    return (promptTokens, completionTokens, totalTokens);
            }
            catch { }
            
            // 方法2: 尝试通过字典访问
            try
            {
                var usageDict = response.Usage as IDictionary<string, object>;
                if (usageDict != null)
                {
                    if (usageDict.TryGetValue("PromptTokens", out var pt)) 
                        promptTokens = Convert.ToInt32(pt);
                    if (usageDict.TryGetValue("CompletionTokens", out var ct)) 
                        completionTokens = Convert.ToInt32(ct);
                    if (usageDict.TryGetValue("TotalTokens", out var tt)) 
                        totalTokens = Convert.ToInt32(tt);
                    
                    if (promptTokens > 0 || completionTokens > 0 || totalTokens > 0)
                        return (promptTokens, completionTokens, totalTokens);
                }
            }
            catch { }
            
            // 方法3: 尝试通过反射获取其他可能的属性名
            try
            {
                var type = response.Usage.GetType();
                
                // 尝试不同的属性名称
                string[] promptNames = { "PromptTokens", "InputTokens", "Prompt", "Input" };
                string[] completionNames = { "CompletionTokens", "OutputTokens", "Completion", "Output" };
                string[] totalNames = { "TotalTokens", "Total" };
                
                foreach (var name in promptNames)
                {
                    var prop = type.GetProperty(name);
                    if (prop != null)
                    {
                        var value = prop.GetValue(response.Usage);
                        if (value != null)
                            promptTokens = Convert.ToInt32(value);
                    }
                }
                
                foreach (var name in completionNames)
                {
                    var prop = type.GetProperty(name);
                    if (prop != null)
                    {
                        var value = prop.GetValue(response.Usage);
                        if (value != null)
                            completionTokens = Convert.ToInt32(value);
                    }
                }
                
                foreach (var name in totalNames)
                {
                    var prop = type.GetProperty(name);
                    if (prop != null)
                    {
                        var value = prop.GetValue(response.Usage);
                        if (value != null)
                            totalTokens = Convert.ToInt32(value);
                    }
                }
                
                if (promptTokens > 0 || completionTokens > 0 || totalTokens > 0)
                    return (promptTokens, completionTokens, totalTokens);
            }
            catch { }
            
            // 方法4: 尝试从字符串中提取数字
            try
            {
                string usageStr = response.Usage.ToString();
                
                var matches = System.Text.RegularExpressions.Regex.Matches(usageStr, @"(\d+)");
                if (matches.Count >= 3)
                {
                    promptTokens = int.Parse(matches[0].Value);
                    completionTokens = int.Parse(matches[1].Value);
                    totalTokens = int.Parse(matches[2].Value);
                }
            }
            catch { }
            
            return (promptTokens, completionTokens, totalTokens);
        }
    }
} 
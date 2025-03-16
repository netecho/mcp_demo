using McpDotNet.Client;
using Microsoft.Extensions.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mcp_client_demo
{
    /// <summary>
    /// Manages tool-related operations and display
    /// </summary>
    public class ToolsManager
    {
        /// <summary>
        /// Displays available tools with detailed information
        /// </summary>
        /// <param name="tools">List of AI tools</param>
        public static void DisplayAvailableTools(List<AITool> tools)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n🧰 Available Tools");
            Console.WriteLine("========================================");
            
            foreach (var tool in tools)
            {
                string name = "Unknown Tool";
                string description = "No description";
                object parameters = null;
                
                // Safely get properties using reflection
                try {
                    var nameProperty = tool.GetType().GetProperty("Name");
                    if (nameProperty != null)
                        name = nameProperty.GetValue(tool)?.ToString() ?? "Unknown Tool";
                    
                    var descProperty = tool.GetType().GetProperty("Description");
                    if (descProperty != null)
                        description = descProperty.GetValue(tool)?.ToString() ?? "No description";
                    
                    var paramsProperty = tool.GetType().GetProperty("Parameters");
                    if (paramsProperty != null)
                        parameters = paramsProperty.GetValue(tool);
                } catch { }
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"📌 {name}");
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"   Description: {description}");
                
                try
                {
                    if (parameters != null)
                    {
                        // Try to get Properties property
                        object properties = null;
                        try {
                            var propsProperty = parameters.GetType().GetProperty("Properties");
                            if (propsProperty != null)
                                properties = propsProperty.GetValue(parameters);
                        } catch { }
                        
                        if (properties != null)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("   Parameters:");
                            
                            // Try to convert properties to IDictionary
                            try {
                                var dict = properties as IDictionary;
                                if (dict != null)
                                {
                                    foreach (DictionaryEntry entry in dict)
                                    {
                                        string key = entry.Key?.ToString() ?? "Unknown parameter";
                                        string paramDesc = "No description";
                                        
                                        try {
                                            var descProp = entry.Value?.GetType().GetProperty("Description");
                                            if (descProp != null)
                                                paramDesc = descProp.GetValue(entry.Value)?.ToString() ?? "No description";
                                        } catch { }
                                        
                                        Console.WriteLine($"     - {key}: {paramDesc}");
                                    }
                                }
                                else
                                {
                                    // Try using reflection to get enumerator
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
                                                var key = keyProp.GetValue(current)?.ToString() ?? "Unknown parameter";
                                                var value = valueProp.GetValue(current);
                                                string paramDesc = "No description";
                                                
                                                try {
                                                    var descProp = value?.GetType().GetProperty("Description");
                                                    if (descProp != null)
                                                        paramDesc = descProp.GetValue(value)?.ToString() ?? "No description";
                                                } catch { }
                                                
                                                Console.WriteLine($"     - {key}: {paramDesc}");
                                            }
                                        }
                                    }
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"     Cannot enumerate parameters: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Cannot get parameter information: {ex.Message}");
                }
                
                Console.WriteLine("----------------------------------------");
            }
            
            Console.WriteLine($"Total {tools.Count} tools available");
            Console.WriteLine("========================================");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
} 
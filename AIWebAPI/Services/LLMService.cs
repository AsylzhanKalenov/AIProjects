using AIWebAPI.Interfaces;
using AIWebAPI.Models;
using AIWebAPI.Persistence.Entities;
using Betalgo.Ranul.OpenAI.Interfaces;
using Betalgo.Ranul.OpenAI.ObjectModels;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Betalgo.Ranul.OpenAI.ObjectModels.SharedModels;

public class LLMService : ILLMService
{
    private readonly IOpenAIService _openAI;
    private readonly IVectorService _vectorService;
    private readonly IFunctionToolService _functionToolService;

    public LLMService(
        IOpenAIService openAI,
        IVectorService vectorService,
        IFunctionToolService functionToolService)
    {
        _openAI = openAI;
        _vectorService = vectorService;
        _functionToolService = functionToolService;
    }

    public async Task<string> GenerateResponseAsync(QueryRequest request)
    {
        var queryEmbedding = await GenerateEmbeddingAsync(request.Query);

        var similarTools = await _vectorService.SearchSimilarAsync(queryEmbedding, 5);

        var tools = GetAvailableFunctions();

        var messages = new List<ChatMessage>
        {
            ChatMessage.FromSystem(BuildSystemPrompt(similarTools)),
            ChatMessage.FromUser(request.Query)
        };

        var chatRequest = new ChatCompletionCreateRequest
        {
            Model = Models.Gpt_4o_mini, // актуальная быстрая модель; при необходимости замените
            Messages = messages,
            Tools = tools,
            ToolChoice = ToolChoice.Auto
        };

        var response = await _openAI.ChatCompletion.CreateCompletion(chatRequest);

        if (response?.Choices?.Count > 0)
        {
            var choice = response.Choices[0];

            // Обработка вызовов инструментов (function calling)
            if (choice.Message?.ToolCalls?.Count > 0)
            {
                foreach (var call in choice.Message.ToolCalls)
                {
                    if (call.Type == "function" && call.FunctionCall is not null)
                    {
                        
                        var toolResult = await _functionToolService.ExecuteFunctionAsync(new FunctionCall
                        {
                            Name = call.FunctionCall.Name,
                            Arguments = call.FunctionCall.Arguments
                        });

                        messages.Add(ChatMessage.FromTool(toolResult, call.Id));
                    }
                }

                // Повторный вызов модели с результатами инструментов
                chatRequest.Messages = messages;
                response = await _openAI.ChatCompletion.CreateCompletion(chatRequest);
            }

            return response?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }

        return string.Empty;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embedReq = new EmbeddingCreateRequest
        {
            Input = text,
            Model = Models.TextEmbeddingV3Small // text-embedding-3-small
        };

        var result = await _openAI.Embeddings.CreateEmbedding(embedReq);

        var vector = result?.Data?.FirstOrDefault()?.Embedding;
        return vector?.Select(v => (float)v).ToArray() ?? Array.Empty<float>();
    }

    private List<ToolDefinition> GetAvailableFunctions()
    {
        // Описание инструментов для function calling
        return new List<ToolDefinition>
        {
            ToolDefinition.DefineFunction(
                new FunctionDefinition()
                {
                    Name = "search_tools",
                    Description = "Search for tools based on criteria",
                    Parameters = new PropertyDefinition()
                    {
                        Type = "object",
                        Properties = new Dictionary<string, PropertyDefinition>()
                        {
                            { "category", new PropertyDefinition { Type = "string", Description = "Tool category" }},
                            { "name", new PropertyDefinition { Type = "string", Description = "Tool name" }},
                            { "max_price", new PropertyDefinition { Type = "number", Description = "Maximum price" }}
                        },
                    }
                }
            ),
            ToolDefinition.DefineFunction(
                new FunctionDefinition()
                {
                    Name = "get_tool_details",
                    Description = "Get detailed information about a specific tool",
                    Parameters = new PropertyDefinition()
                    {
                        Type = "object",
                        Properties = new Dictionary<string, PropertyDefinition>()
                        {
                            { "tool_id", new PropertyDefinition { Type = "string", Description = "Tool ID" }}
                        },
                        Required = new List<string>()
                        {
                            "tool_id"
                        }
                    }
                }
            ),
            ToolDefinition.DefineFunction(
                new FunctionDefinition()
                {
                    Name = "check_availability",
                    Description = "Check if tools are available",
                    Parameters = new PropertyDefinition()
                    {
                        Type = "object",
                        Properties = new Dictionary<string, PropertyDefinition>()
                        {
                            { 
                                "tool_ids", new PropertyDefinition 
                                { 
                                    Type = "array",
                                    Items = new PropertyDefinition()
                                    {
                                        Type = "string"
                                    },
                                    Description = "List of tool IDs" 
                                }
                            }
                        }
                    }
                }
            )
        };
    }

    private string BuildSystemPrompt(List<Tool> contextTools)
    {
        var toolsContext = string.Join("\n",
            contextTools.Select(t => $"- {t.Name}: {t.Description}"));

        return $@"You are a helpful tool instructor assistant. 
                You have access to a database of tools and can help users with:
                - Finding the right tools for their needs
                - Explaining how to use tools safely
                - Comparing different tools
                - Checking tool availability

                Context tools that might be relevant:
                {toolsContext}

                Always provide accurate, helpful, and safety-conscious advice.";
    }
}
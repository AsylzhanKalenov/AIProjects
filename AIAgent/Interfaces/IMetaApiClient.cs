using AIAgent.Models;
using Refit;

namespace AIAgent.Interfaces;

public interface IMetaApiClient
{
    [Post("/v18.0/me/messages")]
    Task<ApiResponse<object>> SendMessage(
        [Body] MetaSendMessageRequest request,
        [Header("Authorization")] string authorization);
}

// Services/Interfaces/IOpenAIClient.cs
public interface IOpenAIClient
{
    [Post("/v1/chat/completions")]
    Task<OpenAIResponse> CreateChatCompletion(
        [Body] OpenAIRequest request,
        [Header("Authorization")] string authorization);
}
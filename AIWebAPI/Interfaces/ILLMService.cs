using AIWebAPI.Models;

namespace AIWebAPI.Interfaces;

public interface ILLMService
{
    Task<string> GenerateResponseAsync(QueryRequest request);
    Task<float[]> GenerateEmbeddingAsync(string text);
}
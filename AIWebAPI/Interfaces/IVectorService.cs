using AIWebAPI.Persistence.Entities;

namespace AIWebAPI.Interfaces;

public interface IVectorService
{
    Task<List<Tool>> SearchSimilarAsync(float[] queryVector, int limit = 5);
    Task UpsertAsync(string id, float[] vector, Dictionary<string, object> metadata);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}
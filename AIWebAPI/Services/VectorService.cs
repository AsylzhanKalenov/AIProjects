using System.Text.Json;
using AIWebAPI.Interfaces;
using AIWebAPI.Persistence.Entities;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AIWebAPI.Services;

public class VectorService : IVectorService
{
    private readonly QdrantClient _qdrantClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IToolService _toolService;
    private readonly ILogger<VectorService> _logger;
    private const string CollectionName = "tools";

    public VectorService(
        QdrantClient qdrantClient,
        IServiceProvider serviceProvider,
        ILogger<VectorService> logger)
    {
        _qdrantClient = qdrantClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<List<Tool>> SearchSimilarAsync(float[] queryVector, int limit = 5)
    {
        try
        {
            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: CollectionName,
                vector: queryVector,
                limit: (ulong)limit);

            var toolIds = searchResult
                .Select(r => Guid.Parse(r.Id.Uuid))
                .ToList();

            // Note: We need to inject IServiceProvider to resolve IToolService
            // to avoid circular dependency
            using var scope = _serviceProvider.CreateScope();
            var toolService = scope.ServiceProvider.GetRequiredService<IToolService>();
            
            return await toolService.GetToolsByIdsAsync(toolIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching similar tools");
            return new List<Tool>();
        }
    }

    public async Task UpsertAsync(string id, float[] vector, Dictionary<string, object> metadata)
    {
        try
        {
            var payload = new Dictionary<string, Value>();
            foreach (var kvp in metadata)
            {
                payload[kvp.Key] = ConvertToValue(kvp.Value);
            }
            
            var points = new List<PointStruct>
            {
                new PointStruct
                {
                    Id = new PointId { Uuid = id },
                    Vectors = vector,
                    //Payload = payload
                }
            };
            points.ForEach(p => p.Payload.Add(payload));

            await _qdrantClient.UpsertAsync(
                collectionName: CollectionName,
                points: points);
            
            _logger.LogInformation($"Upserted vector for id: {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error upserting vector for id: {id}");
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            await _qdrantClient.DeleteAsync(
                collectionName: CollectionName,
                ids: new List<PointId> { new PointId { Uuid = id } });
            
            _logger.LogInformation($"Deleted vector for id: {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting vector for id: {id}");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        try
        {
            var result = await _qdrantClient.RetrieveAsync(
                collectionName: CollectionName,
                ids: new List<PointId> { new PointId { Uuid = id } },
                withPayload: false);

            return result.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking existence for id: {id}");
            return false;
        }
    }
    
    private Dictionary<string, Qdrant.Client.Grpc.Value> ConvertToPayload(Dictionary<string, object> metadata)
    {
        var payload = new Dictionary<string, Value>();
    
        foreach (var kvp in metadata)
        {
            payload[kvp.Key] = ConvertToValue(kvp.Value);
        }
    
        return payload;
    }
    
    private Qdrant.Client.Grpc.Value ConvertToValue(object value)
    {
        return value switch
        {
            null => new Value { NullValue = NullValue.NullValue },
            string s => new Value { StringValue = s },
            int i => new Value { IntegerValue = i },
            long l => new Value { IntegerValue = l },
            double d => new Value { DoubleValue = d },
            float f => new Value { DoubleValue = f },
            bool b => new Value { BoolValue = b },
            Dictionary<string, object> dict => new Value 
            { 
                StructValue = new Struct 
                { 
                    Fields = { ConvertToPayload(dict) } 
                } 
            },
            IEnumerable<object> list => new Value 
            { 
                ListValue = new ListValue 
                { 
                    Values = { list.Select(ConvertToValue) } 
                } 
            },
            _ => throw new ArgumentException($"Unsupported type: {value.GetType()}")
        };
    }
}
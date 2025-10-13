namespace AIWebAPI.Models;

public class QueryRequest
{
    public string Query { get; set; }
    public string UserId { get; set; }
    public Dictionary<string, object> Context { get; set; }
}
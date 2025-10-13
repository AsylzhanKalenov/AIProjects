namespace AIWebAPI.Models;

public class QueryResponse
{
    public bool Success { get; set; }
    public string Response { get; set; }
    public string Error { get; set; }
    public DateTime Timestamp { get; set; }
}
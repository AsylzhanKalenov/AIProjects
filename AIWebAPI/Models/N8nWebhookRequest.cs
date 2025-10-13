namespace AIWebAPI.Models;

public class N8nWebhookRequest
{
    public string Message { get; set; }
    public string UserId { get; set; }
    public string WorkflowId { get; set; }
    public Dictionary<string, object> Context { get; set; }
}
namespace AIWebAPI.Persistence.Entities;

public class Tool
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Specifications { get; set; }
    public string Manufacturer { get; set; }
    public string ModelNumber { get; set; }
    public decimal Price { get; set; }
    public bool Availability { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
        
    public virtual ICollection<ToolUsageInstruction> Instructions { get; set; }
}

public class ToolUsageInstruction
{
    public Guid Id { get; set; }
    public Guid ToolId { get; set; }
    public int StepNumber { get; set; }
    public string Instruction { get; set; }
    public string SafetyNotes { get; set; }
        
    public virtual Tool Tool { get; set; }
}

public class UserQuery
{
    public Guid Id { get; set; }
    public string QueryText { get; set; }
    public string Response { get; set; }
    public Dictionary<string, object> Context { get; set; }
    public DateTime Timestamp { get; set; }
}
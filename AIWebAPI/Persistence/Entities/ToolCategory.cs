namespace AIWebAPI.Persistence.Entities;

public class ToolCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
        
    public virtual ToolCategory ParentCategory { get; set; }
    public virtual ICollection<ToolCategory> SubCategories { get; set; }
}
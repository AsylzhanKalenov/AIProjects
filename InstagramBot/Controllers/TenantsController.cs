using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstagramBot.Data;
using InstagramBot.Models;

namespace InstagramBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(AppDbContext db, ILogger<TenantsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get all tenants
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TenantDto>>> GetAll()
    {
        var tenants = await _db.Tenants
            .Select(t => new TenantDto
            {
                Id = t.Id,
                BusinessName = t.BusinessName,
                InstagramPageId = t.InstagramPageId,
                IsActive = t.IsActive,
                CurrentMonthMessages = t.CurrentMonthMessages,
                MonthlyMessageLimit = t.MonthlyMessageLimit,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(tenants);
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Tenant>> GetById(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        
        if (tenant == null)
            return NotFound();

        // Mask access token
        tenant.AccessToken = tenant.AccessToken.Length > 10 
            ? tenant.AccessToken[..10] + "..." 
            : "***";

        return Ok(tenant);
    }

    /// <summary>
    /// Create new tenant (new client)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Tenant>> Create([FromBody] CreateTenantRequest request)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            BusinessName = request.BusinessName,
            InstagramPageId = request.InstagramPageId,
            AccessToken = request.AccessToken,
            SystemPrompt = request.SystemPrompt,
            KnowledgeBase = request.KnowledgeBase,
            WelcomeMessage = request.WelcomeMessage,
            FallbackMessage = request.FallbackMessage,
            MonthlyMessageLimit = request.MonthlyMessageLimit,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created new tenant: {TenantName} ({TenantId})", tenant.BusinessName, tenant.Id);

        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
    }

    /// <summary>
    /// Update tenant settings
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        
        if (tenant == null)
            return NotFound();

        if (request.BusinessName != null)
            tenant.BusinessName = request.BusinessName;
        
        if (request.SystemPrompt != null)
            tenant.SystemPrompt = request.SystemPrompt;
        
        if (request.KnowledgeBase != null)
            tenant.KnowledgeBase = request.KnowledgeBase;
        
        if (request.WelcomeMessage != null)
            tenant.WelcomeMessage = request.WelcomeMessage;
        
        if (request.FallbackMessage != null)
            tenant.FallbackMessage = request.FallbackMessage;
        
        if (request.AccessToken != null)
            tenant.AccessToken = request.AccessToken;
        
        if (request.IsActive.HasValue)
            tenant.IsActive = request.IsActive.Value;
        
        if (request.MonthlyMessageLimit.HasValue)
            tenant.MonthlyMessageLimit = request.MonthlyMessageLimit.Value;

        tenant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated tenant: {TenantId}", id);

        return NoContent();
    }

    /// <summary>
    /// Delete tenant
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        
        if (tenant == null)
            return NotFound();

        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted tenant: {TenantId}", id);

        return NoContent();
    }

    /// <summary>
    /// Get tenant statistics
    /// </summary>
    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<TenantStats>> GetStats(Guid id)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Conversations)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (tenant == null)
            return NotFound();

        var totalMessages = await _db.Messages
            .CountAsync(m => m.Conversation.TenantId == id);

        var last24h = DateTime.UtcNow.AddDays(-1);
        var messagesLast24h = await _db.Messages
            .CountAsync(m => m.Conversation.TenantId == id && m.CreatedAt >= last24h);

        return Ok(new TenantStats
        {
            TenantId = id,
            TotalConversations = tenant.Conversations.Count,
            TotalMessages = totalMessages,
            MessagesLast24Hours = messagesLast24h,
            CurrentMonthMessages = tenant.CurrentMonthMessages,
            MonthlyMessageLimit = tenant.MonthlyMessageLimit,
            UsagePercent = tenant.MonthlyMessageLimit > 0 
                ? (double)tenant.CurrentMonthMessages / tenant.MonthlyMessageLimit * 100 
                : 0
        });
    }

    /// <summary>
    /// Reset monthly message counter (call at start of each month)
    /// </summary>
    [HttpPost("reset-counters")]
    public async Task<ActionResult> ResetMonthlyCounters()
    {
        await _db.Tenants.ExecuteUpdateAsync(s => s
            .SetProperty(t => t.CurrentMonthMessages, 0));

        _logger.LogInformation("Reset all tenant message counters");

        return Ok(new { message = "All counters reset" });
    }
}

// DTOs
public class TenantDto
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string InstagramPageId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CurrentMonthMessages { get; set; }
    public int MonthlyMessageLimit { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTenantRequest
{
    public required string BusinessName { get; set; }
    public required string InstagramPageId { get; set; }
    public required string AccessToken { get; set; }
    public required string SystemPrompt { get; set; }
    public required string KnowledgeBase { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? FallbackMessage { get; set; }
    public int MonthlyMessageLimit { get; set; } = 1000;
}

public class UpdateTenantRequest
{
    public string? BusinessName { get; set; }
    public string? AccessToken { get; set; }
    public string? SystemPrompt { get; set; }
    public string? KnowledgeBase { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? FallbackMessage { get; set; }
    public bool? IsActive { get; set; }
    public int? MonthlyMessageLimit { get; set; }
}

public class TenantStats
{
    public Guid TenantId { get; set; }
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public int MessagesLast24Hours { get; set; }
    public int CurrentMonthMessages { get; set; }
    public int MonthlyMessageLimit { get; set; }
    public double UsagePercent { get; set; }
}

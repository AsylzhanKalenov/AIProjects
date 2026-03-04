using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstagramBot.Data;
using InstagramBot.Models;

namespace InstagramBot.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/channels")]
public class ChannelsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ChannelsController> _logger;

    public ChannelsController(AppDbContext db, ILogger<ChannelsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get all channels for a tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ChannelDto>>> GetAll(Guid tenantId)
    {
        var channels = await _db.Channels
            .Where(c => c.TenantId == tenantId)
            .Select(c => new ChannelDto
            {
                Id = c.Id,
                Type = c.Type,
                DisplayName = c.DisplayName,
                ExternalId = c.ExternalId,
                PhoneNumber = c.PhoneNumber,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(channels);
    }

    /// <summary>
    /// Get channel by ID
    /// </summary>
    [HttpGet("{channelId:guid}")]
    public async Task<ActionResult<ChannelDto>> GetById(Guid tenantId, Guid channelId)
    {
        var channel = await _db.Channels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.TenantId == tenantId);

        if (channel == null)
            return NotFound();

        return Ok(new ChannelDto
        {
            Id = channel.Id,
            Type = channel.Type,
            DisplayName = channel.DisplayName,
            ExternalId = channel.ExternalId,
            PhoneNumber = channel.PhoneNumber,
            WhatsAppBusinessAccountId = channel.WhatsAppBusinessAccountId,
            IsActive = channel.IsActive,
            CreatedAt = channel.CreatedAt,
            AccessTokenMasked = channel.AccessToken.Length > 10
                ? channel.AccessToken[..10] + "..."
                : "***"
        });
    }

    /// <summary>
    /// Add a new channel to a tenant
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChannelDto>> Create(
        Guid tenantId,
        [FromBody] CreateChannelRequest request)
    {
        // Verify tenant exists
        var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == tenantId);
        if (!tenantExists)
            return NotFound("Tenant not found");

        // Check for duplicate ExternalId
        var duplicate = await _db.Channels.AnyAsync(c =>
            c.ExternalId == request.ExternalId && c.Type == request.Type);
        if (duplicate)
            return Conflict($"Channel with ExternalId '{request.ExternalId}' already exists");

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = request.Type,
            DisplayName = request.DisplayName,
            ExternalId = request.ExternalId,
            AccessToken = request.AccessToken,
            WhatsAppBusinessAccountId = request.WhatsAppBusinessAccountId,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Channels.Add(channel);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Created {ChannelType} channel '{DisplayName}' for tenant {TenantId}",
            channel.Type, channel.DisplayName, tenantId);

        return CreatedAtAction(
            nameof(GetById),
            new { tenantId, channelId = channel.Id },
            new ChannelDto
            {
                Id = channel.Id,
                Type = channel.Type,
                DisplayName = channel.DisplayName,
                ExternalId = channel.ExternalId,
                PhoneNumber = channel.PhoneNumber,
                IsActive = channel.IsActive,
                CreatedAt = channel.CreatedAt
            });
    }

    /// <summary>
    /// Update channel settings
    /// </summary>
    [HttpPut("{channelId:guid}")]
    public async Task<ActionResult> Update(
        Guid tenantId, Guid channelId,
        [FromBody] UpdateChannelRequest request)
    {
        var channel = await _db.Channels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.TenantId == tenantId);

        if (channel == null)
            return NotFound();

        if (request.DisplayName != null)
            channel.DisplayName = request.DisplayName;
        if (request.AccessToken != null)
            channel.AccessToken = request.AccessToken;
        if (request.IsActive.HasValue)
            channel.IsActive = request.IsActive.Value;
        if (request.PhoneNumber != null)
            channel.PhoneNumber = request.PhoneNumber;

        channel.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated channel {ChannelId}", channelId);

        return NoContent();
    }

    /// <summary>
    /// Delete a channel
    /// </summary>
    [HttpDelete("{channelId:guid}")]
    public async Task<ActionResult> Delete(Guid tenantId, Guid channelId)
    {
        var channel = await _db.Channels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.TenantId == tenantId);

        if (channel == null)
            return NotFound();

        _db.Channels.Remove(channel);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted channel {ChannelId}", channelId);

        return NoContent();
    }
}

// ---- DTOs ----

public class ChannelDto
{
    public Guid Id { get; set; }
    public ChannelType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? WhatsAppBusinessAccountId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AccessTokenMasked { get; set; }
}

public class CreateChannelRequest
{
    public required ChannelType Type { get; set; }
    public required string DisplayName { get; set; }
    
    /// <summary>
    /// Instagram: Page ID, WhatsApp: Phone Number ID
    /// </summary>
    public required string ExternalId { get; set; }
    public required string AccessToken { get; set; }
    
    /// <summary>
    /// WhatsApp only: WABA ID
    /// </summary>
    public string? WhatsAppBusinessAccountId { get; set; }
    
    /// <summary>
    /// WhatsApp only: phone number in international format
    /// </summary>
    public string? PhoneNumber { get; set; }
}

public class UpdateChannelRequest
{
    public string? DisplayName { get; set; }
    public string? AccessToken { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsActive { get; set; }
}

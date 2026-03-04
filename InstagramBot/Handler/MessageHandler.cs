using InstagramBot.Data;
using InstagramBot.Interfaces;
using InstagramBot.Models;
using InstagramBot.Services;
using Microsoft.EntityFrameworkCore;

namespace InstagramBot.Handler;

public interface IMessageHandler
{
    Task ProcessAsync(WebhookPayload payload);
}

public class MessageHandler : IMessageHandler
{
    private readonly IInstagramApi _instagram;
    private readonly IOpenAiService _openAi;
    private readonly AppDbContext _db;
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(
        IInstagramApi instagram,
        IOpenAiService openAi,
        AppDbContext db,
        ILogger<MessageHandler> logger)
    {
        _instagram = instagram;
        _openAi = openAi;
        _db = db;
        _logger = logger;
    }

    public async Task ProcessAsync(WebhookPayload payload)
    {
        if (payload.Object != "instagram")
        {
            _logger.LogWarning("Received non-instagram webhook: {Object}", payload.Object);
            return;
        }

        foreach (var entry in payload.Entry)
        {
            await ProcessEntryAsync(entry);
        }
    }

    private async Task ProcessEntryAsync(Entry entry)
    {
        // Find tenant by Instagram Page ID
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.InstagramPageId == entry.Id && t.IsActive);

        if (tenant == null)
        {
            _logger.LogWarning("No active tenant found for Instagram Page ID: {PageId}", entry.Id);
            return;
        }

        // Check message limit
        if (tenant.CurrentMonthMessages >= tenant.MonthlyMessageLimit)
        {
            _logger.LogWarning("Tenant {TenantId} exceeded monthly message limit", tenant.Id);
            return;
        }

        foreach (var messaging in entry.Messaging ?? [])
        {
            await ProcessMessageAsync(tenant, messaging);
        }
    }

    private async Task ProcessMessageAsync(Tenant tenant, MessagingEvent messaging)
    {
        // Skip if no text message
        var userText = messaging.Message?.Text;
        if (string.IsNullOrWhiteSpace(userText))
        {
            _logger.LogDebug("Skipping non-text message");
            return;
        }

        var senderId = messaging.Sender.Id;
        
        _logger.LogInformation(
            "Processing message from {SenderId} to tenant {TenantName}: {Text}",
            senderId, tenant.BusinessName, userText.Length > 50 ? userText[..50] + "..." : userText);

        try
        {
            // Get or create conversation
            var conversation = await GetOrCreateConversationAsync(tenant.Id, senderId);

            // Save incoming message
            await SaveMessageAsync(conversation.Id, userText, isFromUser: true, messaging.Message?.Mid);

            // Get conversation history for context
            var history = await GetConversationHistoryAsync(conversation.Id);

            // Generate AI response
            var aiResponse = await _openAi.GetResponseAsync(
                tenant.SystemPrompt,
                tenant.KnowledgeBase,
                userText,
                history);

            // Send response to Instagram
            var sendResult = await SendInstagramMessageAsync(tenant.AccessToken, senderId, aiResponse);

            if (sendResult.Error != null)
            {
                _logger.LogError(
                    "Failed to send message: {Error} (Code: {Code})",
                    sendResult.Error.Message, sendResult.Error.Code);
                return;
            }

            // Save bot response
            await SaveMessageAsync(conversation.Id, aiResponse, isFromUser: false, sendResult.MessageId);

            // Update message count
            await IncrementMessageCountAsync(tenant.Id);

            _logger.LogInformation("Successfully processed message for tenant {TenantName}", tenant.BusinessName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for tenant {TenantId}", tenant.Id);
        }
    }

    private async Task<Conversation> GetOrCreateConversationAsync(Guid tenantId, string instagramUserId)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.InstagramUserId == instagramUserId);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InstagramUserId = instagramUserId,
                StartedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();
        }
        else
        {
            conversation.LastMessageAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return conversation;
    }

    private async Task SaveMessageAsync(Guid conversationId, string content, bool isFromUser, string? messageId)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = content,
            IsFromUser = isFromUser,
            InstagramMessageId = messageId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
    }

    private async Task<List<ChatMessage>> GetConversationHistoryAsync(Guid conversationId)
    {
        return await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(10)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage
            {
                IsFromUser = m.IsFromUser,
                Content = m.Content
            })
            .ToListAsync();
    }

    private async Task<SendMessageResponse> SendInstagramMessageAsync(string accessToken, string recipientId, string text)
    {
        var request = new SendMessageRequest
        {
            Recipient = new MessageRecipient { Id = recipientId },
            Message = new OutgoingMessage { Text = text },
            MessagingType = "RESPONSE"
        };

        return await _instagram.SendMessageAsync(accessToken, request);
    }

    private async Task IncrementMessageCountAsync(Guid tenantId)
    {
        await _db.Tenants
            .Where(t => t.Id == tenantId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.CurrentMonthMessages, t => t.CurrentMonthMessages + 1));
    }
}

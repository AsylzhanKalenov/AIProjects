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

/// <summary>
/// Handles incoming Instagram messages.
/// 
/// Updated for Instagram Login approach:
///   - Looks up Channel by ExternalId (IG User ID), not Tenant.InstagramPageId
///   - Sends messages via graph.instagram.com with Bearer token
///   - Entry.Id in webhook = Instagram professional account ID = Channel.ExternalId
/// </summary>
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
        // entry.Id = Instagram professional account ID (IG User ID)
        // This matches Channel.ExternalId for Instagram channels
        var channel = await _db.Channels
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c =>
                c.ExternalId == entry.Id &&
                c.Type == ChannelType.Instagram &&
                c.IsActive);

        if (channel == null)
        {
            _logger.LogWarning(
                "No active Instagram channel found for IG User ID: {IgUserId}", entry.Id);
            return;
        }

        var tenant = channel.Tenant;

        if (!tenant.IsActive)
        {
            _logger.LogWarning("Tenant {TenantId} is inactive", tenant.Id);
            return;
        }

        // Check message limit
        if (tenant.CurrentMonthMessages >= tenant.MonthlyMessageLimit)
        {
            _logger.LogWarning("Tenant {TenantId} exceeded monthly message limit", tenant.Id);
            return;
        }

        // Check token expiry
        if (channel.TokenExpiresAt.HasValue && channel.TokenExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogError(
                "Instagram token expired for channel {ChannelId} (expired {ExpiredAt}). " +
                "Client needs to re-authorize.",
                channel.Id, channel.TokenExpiresAt);
            return;
        }

        foreach (var messaging in entry.Messaging ?? [])
        {
            // Skip non-message events
            if (messaging.Read != null)
            {
                _logger.LogDebug("Skipping read receipt: {Mid}", messaging.Read.Mid);
                continue;
            }

            if (messaging.Reaction != null)
            {
                _logger.LogDebug("Skipping reaction: {Action} on {Mid}",
                    messaging.Reaction.Action, messaging.Reaction.Mid);
                continue;
            }

            // Handle postback (Icebreaker / Generic Template button)
            if (messaging.Postback != null)
            {
                await ProcessPostbackAsync(tenant, channel, messaging);
                continue;
            }

            // Handle regular message
            if (messaging.Message != null)
            {
                await ProcessMessageAsync(tenant, channel, messaging);
            }
        }
    }

    private async Task ProcessMessageAsync(Tenant tenant, Channel channel, MessagingEvent messaging)
    {
        var message = messaging.Message!;

        if (message.IsEcho == true || message.IsDeleted == true)
        {
            _logger.LogDebug("Skipping echo/deleted message: {Mid}", message.Mid);
            return;
        }

        if (message.IsUnsupported == true)
        {
            await SendInstagramMessageAsync(channel, messaging.Sender.Id,
                "К сожалению, этот тип сообщения не поддерживается. Пожалуйста, отправьте текст.");
            return;
        }

        var userText = ExtractTextContent(message);
        if (string.IsNullOrWhiteSpace(userText))
            return;

        var senderId = messaging.Sender.Id;

        _logger.LogInformation(
            "Processing message from {SenderId} to tenant {TenantName}: {Text}",
            senderId, tenant.BusinessName,
            userText.Length > 50 ? userText[..50] + "..." : userText);

        try
        {
            var conversation = await GetOrCreateConversationAsync(tenant.Id, channel.Id, senderId);
            await SaveMessageAsync(conversation.Id, userText, isFromUser: true, message.Mid);

            var history = await GetConversationHistoryAsync(conversation.Id);

            var aiResponse = await _openAi.GetResponseAsync(
                tenant.SystemPrompt, tenant.KnowledgeBase, userText, history);

            var sendResult = await SendInstagramMessageAsync(channel, senderId, aiResponse);

            if (sendResult.Error != null)
            {
                _logger.LogError("Failed to send message: {Error} (Code: {Code})",
                    sendResult.Error.Message, sendResult.Error.Code);
                return;
            }

            await SaveMessageAsync(conversation.Id, aiResponse, isFromUser: false, sendResult.MessageId);
            await IncrementMessageCountAsync(tenant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for tenant {TenantId}", tenant.Id);
        }
    }

    private async Task ProcessPostbackAsync(Tenant tenant, Channel channel, MessagingEvent messaging)
    {
        var postback = messaging.Postback!;
        var senderId = messaging.Sender.Id;

        _logger.LogInformation("Processing postback from {SenderId}: '{Title}'", senderId, postback.Title);

        try
        {
            var conversation = await GetOrCreateConversationAsync(tenant.Id, channel.Id, senderId);
            await SaveMessageAsync(conversation.Id, postback.Title, isFromUser: true, postback.Mid);

            var history = await GetConversationHistoryAsync(conversation.Id);

            var aiResponse = await _openAi.GetResponseAsync(
                tenant.SystemPrompt, tenant.KnowledgeBase, postback.Title, history);

            var sendResult = await SendInstagramMessageAsync(channel, senderId, aiResponse);

            if (sendResult.Error != null)
            {
                _logger.LogError("Failed to send postback response: {Error}", sendResult.Error.Message);
                return;
            }

            await SaveMessageAsync(conversation.Id, aiResponse, isFromUser: false, sendResult.MessageId);
            await IncrementMessageCountAsync(tenant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing postback for tenant {TenantId}", tenant.Id);
        }
    }

    private string? ExtractTextContent(IncomingMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.Text))
            return message.Text;

        if (message.QuickReply != null)
            return message.QuickReply.Payload;

        if (message.ReplyTo?.Story != null)
            return message.Text ?? "[Ответ на историю]";

        if (message.Attachments is { Count: > 0 })
        {
            var first = message.Attachments[0];
            return first.Type switch
            {
                "image" => "[Изображение]",
                "video" => "[Видео]",
                "audio" => "[Аудио]",
                "file" => "[Файл]",
                "share" => "[Поделился публикацией]",
                "story_mention" => "[Упоминание в истории]",
                "ig_reel" or "reel" => "[Reels]",
                "ephemeral" => null,
                _ => $"[Вложение: {first.Type}]"
            };
        }

        return null;
    }

    // ============================================================
    // API & DB METHODS
    // ============================================================

    /// <summary>
    /// Send message via Instagram API (graph.instagram.com).
    /// Uses Bearer token in Authorization header.
    /// igUserId = Channel.ExternalId (IG professional account ID).
    /// </summary>
    private async Task<SendMessageResponse> SendInstagramMessageAsync(
        Channel channel, string recipientId, string text)
    {
        var request = new SendMessageRequest
        {
            Recipient = new MessageRecipient { Id = recipientId },
            Message = new OutgoingMessage { Text = text },
            MessagingType = "RESPONSE"
        };

        // Bearer token format for graph.instagram.com
        var bearerToken = $"Bearer {channel.AccessToken}";

        return await _instagram.SendMessageAsync(
            channel.ExternalId,  // IG User ID (the business account)
            bearerToken,
            request);
    }

    private async Task<Conversation> GetOrCreateConversationAsync(
        Guid tenantId, Guid channelId, string instagramUserId)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenantId &&
                c.ChannelId == channelId &&
                c.InstagramUserId == instagramUserId);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ChannelId = channelId,
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

    private async Task SaveMessageAsync(
        Guid conversationId, string content, bool isFromUser, string? messageId)
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

    private async Task IncrementMessageCountAsync(Guid tenantId)
    {
        await _db.Tenants
            .Where(t => t.Id == tenantId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.CurrentMonthMessages, t => t.CurrentMonthMessages + 1));
    }
}
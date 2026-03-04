using Microsoft.EntityFrameworkCore;
using InstagramBot.Data;
using InstagramBot.Interfaces;
using InstagramBot.Models;
using InstagramBot.Models.WhatsApp;

namespace InstagramBot.Services;

public interface IWhatsAppMessageHandler
{
    Task ProcessAsync(WhatsAppWebhookPayload payload);
}

public class WhatsAppMessageHandler : IWhatsAppMessageHandler
{
    private readonly IWhatsAppApi _whatsApp;
    private readonly IOpenAiService _openAi;
    private readonly AppDbContext _db;
    private readonly ILogger<WhatsAppMessageHandler> _logger;

    public WhatsAppMessageHandler(
        IWhatsAppApi whatsApp,
        IOpenAiService openAi,
        AppDbContext db,
        ILogger<WhatsAppMessageHandler> logger)
    {
        _whatsApp = whatsApp;
        _openAi = openAi;
        _db = db;
        _logger = logger;
    }

    public async Task ProcessAsync(WhatsAppWebhookPayload payload)
    {
        if (payload.Object != "whatsapp_business_account")
        {
            _logger.LogWarning("Received non-whatsapp webhook: {Object}", payload.Object);
            return;
        }

        foreach (var entry in payload.Entry)
        {
            foreach (var change in entry.Changes)
            {
                if (change.Field != "messages")
                    continue;

                await ProcessChangeAsync(entry.Id, change.Value);
            }
        }
    }

    private async Task ProcessChangeAsync(string wabaId, WhatsAppValue value)
    {
        var phoneNumberId = value.Metadata.PhoneNumberId;

        // Find channel by Phone Number ID
        var channel = await _db.Channels
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c =>
                c.ExternalId == phoneNumberId &&
                c.Type == ChannelType.WhatsApp &&
                c.IsActive);

        if (channel == null)
        {
            _logger.LogWarning(
                "No active WhatsApp channel found for Phone Number ID: {PhoneNumberId}",
                phoneNumberId);
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

        // Process status updates (delivery receipts)
        if (value.Statuses?.Any() == true)
        {
            foreach (var status in value.Statuses)
            {
                _logger.LogDebug(
                    "WhatsApp status update: {MessageId} -> {Status}",
                    status.Id, status.Status);
            }
            return; // Status updates don't require a response
        }

        // Process incoming messages
        if (value.Messages == null || value.Messages.Count == 0)
            return;

        // Build contacts lookup
        var contactsMap = value.Contacts?
            .ToDictionary(c => c.WaId, c => c.Profile.Name) 
            ?? new Dictionary<string, string>();

        foreach (var message in value.Messages)
        {
            await ProcessMessageAsync(tenant, channel, message, contactsMap);
        }
    }

    private async Task ProcessMessageAsync(
        Tenant tenant,
        Channel channel,
        WhatsAppMessage message,
        Dictionary<string, string> contacts)
    {
        // Extract text content from different message types
        var userText = ExtractTextContent(message);

        if (string.IsNullOrWhiteSpace(userText))
        {
            _logger.LogDebug("Skipping unsupported message type: {Type}", message.Type);
            return;
        }

        var senderPhone = message.From;
        contacts.TryGetValue(senderPhone, out var senderName);

        _logger.LogInformation(
            "Processing WhatsApp message from {Sender} ({Name}) to tenant {TenantName}: {Text}",
            senderPhone,
            senderName ?? "unknown",
            tenant.BusinessName,
            userText.Length > 50 ? userText[..50] + "..." : userText);

        try
        {
            // Mark message as read (blue ticks)
            await MarkAsReadAsync(channel, message.Id);

            // Get or create conversation
            var conversation = await GetOrCreateConversationAsync(
                tenant.Id, channel.Id, senderPhone, senderName);

            // Save incoming message
            await SaveMessageAsync(conversation.Id, userText, isFromUser: true, message.Id);

            // Get conversation history
            var history = await GetConversationHistoryAsync(conversation.Id);

            // Generate AI response
            var aiResponse = await _openAi.GetResponseAsync(
                tenant.SystemPrompt,
                tenant.KnowledgeBase,
                userText,
                history);

            // Send text response
            var sendResult = await SendTextMessageAsync(channel, senderPhone, aiResponse);

            if (sendResult.Error != null)
            {
                _logger.LogError(
                    "Failed to send WhatsApp message: {Error} (Code: {Code})",
                    sendResult.Error.Message, sendResult.Error.Code);
                return;
            }

            var sentMessageId = sendResult.Messages?.FirstOrDefault()?.Id;

            // Save bot response
            await SaveMessageAsync(conversation.Id, aiResponse, isFromUser: false, sentMessageId);

            // Update message count
            await IncrementMessageCountAsync(tenant.Id);

            _logger.LogInformation(
                "Successfully processed WhatsApp message for tenant {TenantName}",
                tenant.BusinessName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing WhatsApp message for tenant {TenantId}",
                tenant.Id);
        }
    }

    /// <summary>
    /// Extract text from various WhatsApp message types
    /// </summary>
    private string? ExtractTextContent(WhatsAppMessage message)
    {
        return message.Type switch
        {
            "text" => message.Text?.Body,
            "image" => message.Image?.Caption ?? "[Изображение]",
            "document" => message.Document?.Caption ?? $"[Документ: {message.Document?.Filename ?? "файл"}]",
            "audio" => "[Голосовое сообщение]",
            "video" => message.Video?.Caption ?? "[Видео]",
            "sticker" => "[Стикер]",
            "location" => FormatLocation(message.Location),
            "interactive" => ExtractInteractiveReply(message.Interactive),
            "button" => message.Button?.Text,
            _ => null
        };
    }

    private string? FormatLocation(WhatsAppLocation? location)
    {
        if (location == null) return "[Геолокация]";
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(location.Name)) parts.Add(location.Name);
        if (!string.IsNullOrEmpty(location.Address)) parts.Add(location.Address);
        parts.Add($"({location.Latitude}, {location.Longitude})");
        return $"[Геолокация: {string.Join(", ", parts)}]";
    }

    private string? ExtractInteractiveReply(WhatsAppInteractiveReply? interactive)
    {
        if (interactive == null) return null;
        return interactive.Type switch
        {
            "button_reply" => interactive.ButtonReply?.Title,
            "list_reply" => interactive.ListReply?.Title,
            _ => null
        };
    }

    // ============================================================
    // SENDING METHODS
    // ============================================================

    public async Task<WhatsAppSendResponse> SendTextMessageAsync(
        Channel channel, string recipientPhone, string text)
    {
        var request = new WhatsAppSendRequest
        {
            To = recipientPhone,
            Type = "text",
            Text = new WhatsAppSendText { Body = text }
        };

        return await _whatsApp.SendMessageAsync(
            channel.ExternalId, channel.AccessToken, request);
    }

    public async Task<WhatsAppSendResponse> SendInteractiveButtonsAsync(
        Channel channel, string recipientPhone,
        string bodyText, List<(string id, string title)> buttons,
        string? headerText = null, string? footerText = null)
    {
        var request = new WhatsAppSendRequest
        {
            To = recipientPhone,
            Type = "interactive",
            Interactive = new WhatsAppSendInteractive
            {
                Type = "button",
                Header = headerText != null
                    ? new WhatsAppInteractiveHeader { Type = "text", Text = headerText }
                    : null,
                Body = new WhatsAppInteractiveBody { Text = bodyText },
                Footer = footerText != null
                    ? new WhatsAppInteractiveFooter { Text = footerText }
                    : null,
                Action = new WhatsAppInteractiveAction
                {
                    Buttons = buttons.Select(b => new WhatsAppInteractiveButton
                    {
                        Reply = new WhatsAppButtonContent { Id = b.id, Title = b.title }
                    }).ToList()
                }
            }
        };

        return await _whatsApp.SendMessageAsync(
            channel.ExternalId, channel.AccessToken, request);
    }

    public async Task<WhatsAppSendResponse> SendInteractiveListAsync(
        Channel channel, string recipientPhone,
        string bodyText, string buttonText,
        List<WhatsAppInteractiveSection> sections,
        string? headerText = null, string? footerText = null)
    {
        var request = new WhatsAppSendRequest
        {
            To = recipientPhone,
            Type = "interactive",
            Interactive = new WhatsAppSendInteractive
            {
                Type = "list",
                Header = headerText != null
                    ? new WhatsAppInteractiveHeader { Type = "text", Text = headerText }
                    : null,
                Body = new WhatsAppInteractiveBody { Text = bodyText },
                Footer = footerText != null
                    ? new WhatsAppInteractiveFooter { Text = footerText }
                    : null,
                Action = new WhatsAppInteractiveAction
                {
                    Button = buttonText,
                    Sections = sections
                }
            }
        };

        return await _whatsApp.SendMessageAsync(
            channel.ExternalId, channel.AccessToken, request);
    }

    public async Task<WhatsAppSendResponse> SendTemplateAsync(
        Channel channel, string recipientPhone,
        string templateName, string languageCode,
        List<WhatsAppTemplateComponent>? components = null)
    {
        var request = new WhatsAppSendRequest
        {
            To = recipientPhone,
            Type = "template",
            Template = new WhatsAppSendTemplate
            {
                Name = templateName,
                Language = new WhatsAppTemplateLanguage { Code = languageCode },
                Components = components
            }
        };

        return await _whatsApp.SendMessageAsync(
            channel.ExternalId, channel.AccessToken, request);
    }

    public async Task<WhatsAppSendResponse> SendImageAsync(
        Channel channel, string recipientPhone,
        string imageUrl, string? caption = null)
    {
        var request = new WhatsAppSendRequest
        {
            To = recipientPhone,
            Type = "image",
            Image = new WhatsAppSendMedia { Link = imageUrl, Caption = caption }
        };

        return await _whatsApp.SendMessageAsync(
            channel.ExternalId, channel.AccessToken, request);
    }

    public async Task<WhatsAppSendResponse> SendDocumentAsync(
        Channel channel, string recipientPhone,
        string documentUrl, string? filename = null, string? caption = null)
    {
        var request = new WhatsAppSendRequest
        {
            To = recipientPhone,
            Type = "document",
            Document = new WhatsAppSendDocument
            {
                Link = documentUrl,
                Filename = filename,
                Caption = caption
            }
        };

        return await _whatsApp.SendMessageAsync(
            channel.ExternalId, channel.AccessToken, request);
    }

    // ============================================================
    // DATABASE METHODS
    // ============================================================

    private async Task MarkAsReadAsync(Channel channel, string messageId)
    {
        try
        {
            await _whatsApp.MarkAsReadAsync(
                channel.ExternalId,
                channel.AccessToken,
                new WhatsAppMarkReadRequest { MessageId = messageId });
        }
        catch (Exception ex)
        {
            // Non-critical, don't fail the whole flow
            _logger.LogWarning(ex, "Failed to mark message as read: {MessageId}", messageId);
        }
    }

    private async Task<Conversation> GetOrCreateConversationAsync(
        Guid tenantId, Guid channelId, string senderPhone, string? senderName)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenantId &&
                c.ChannelId == channelId &&
                c.InstagramUserId == senderPhone); // Reused field for phone number

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ChannelId = channelId,
                InstagramUserId = senderPhone, // Stores phone for WhatsApp
                UserName = senderName,
                StartedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();
        }
        else
        {
            conversation.LastMessageAt = DateTime.UtcNow;
            if (senderName != null && conversation.UserName != senderName)
                conversation.UserName = senderName;
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
            InstagramMessageId = messageId, // Stores wamid for WhatsApp
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

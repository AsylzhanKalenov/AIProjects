using AIAgent.Models;
using AIAgent.Services;
using Hangfire;

namespace AIAgent.Jobs;

// Services/BackgroundJobs/MessageProcessingJob.cs
public class MessageProcessingJob
{
    private readonly IMetaMessagingService _messagingService;
    private readonly ILogger<MessageProcessingJob> _logger;
    
    public MessageProcessingJob(
        IMetaMessagingService messagingService,
        ILogger<MessageProcessingJob> logger)
    {
        _messagingService = messagingService;
        _logger = logger;
    }
    
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessMessage(MetaWebhookDto webhook)
    {
        try
        {
            await _messagingService.ProcessIncomingMessageAsync(webhook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from webhook");
            throw;
        }
    }
}
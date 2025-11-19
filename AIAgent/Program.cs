// Program.cs

using System.Text.Json;
using AIAgent.Interfaces;
using AIAgent.Jobs;
using AIAgent.Services;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Redis для контекста
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "ChatBot:";
});

// Hangfire для фоновых задач
builder.Services.AddHangfire(config =>
    config.UseRedisStorage(builder.Configuration["Redis:ConnectionString"]));
builder.Services.AddHangfireServer();

// Refit клиенты
builder.Services.AddRefitClient<IMetaApiClient>()
    .ConfigureHttpClient(c => 
        c.BaseAddress = new Uri("https://graph.facebook.com"));

builder.Services.AddRefitClient<IOpenAIClient>()
    .ConfigureHttpClient(c => 
        c.BaseAddress = new Uri("https://api.openai.com"));

// Сервисы
builder.Services.AddScoped<IAIService, OpenAIService>();
builder.Services.AddScoped<IConversationContextService, ConversationContextService>();
builder.Services.AddScoped<IMetaMessagingService, MetaMessagingService>();
builder.Services.AddScoped<MessageProcessingJob>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard("/hangfire");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
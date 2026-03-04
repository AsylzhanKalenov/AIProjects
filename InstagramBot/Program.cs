using Microsoft.EntityFrameworkCore;
using Refit;
using InstagramBot.Data;
using InstagramBot.Handler;
using InstagramBot.Interfaces;
using InstagramBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Instagram & WhatsApp Bot API", 
        Version = "v1",
        Description = "Multi-channel chatbot platform for managing Instagram and WhatsApp bots for multiple businesses"
    });
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Instagram API (Refit)
builder.Services
    .AddRefitClient<IInstagramApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://graph.facebook.com"));

// WhatsApp API (Refit) — same base URL as Instagram (Meta Graph API)
builder.Services
    .AddRefitClient<IWhatsAppApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://graph.facebook.com"));

// OpenAI
builder.Services.AddHttpClient<IOpenAiService, OpenAiService>();

// Message Handlers
builder.Services.AddScoped<IMessageHandler, MessageHandler>();
builder.Services.AddScoped<IWhatsAppMessageHandler, WhatsAppMessageHandler>();

// CORS (for admin panel if needed later)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        
        int retries = 5;
        while (retries > 0)
        {
            try
            {
                db.Database.Migrate();
                logger.LogInformation("Database migrated successfully.");
                break;
            }
            catch (Exception ex) when (retries > 0)
            {
                retries--;
                logger.LogWarning($"Database not ready, retrying... ({retries} attempts left)");
                Thread.Sleep(2000);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bot Platform API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    time = DateTime.UtcNow,
    channels = new[] { "instagram", "whatsapp" }
}));

Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║         Multi-Channel Bot Platform Started!                    ║
╠═══════════════════════════════════════════════════════════════╣
║  Swagger UI:          http://localhost:5000                    ║
║  Instagram Webhook:   /api/webhooks/instagram                  ║
║  WhatsApp Webhook:    /api/webhooks/whatsapp                   ║
║  Channels API:        /api/tenants/{id}/channels               ║
║  Health Check:        /health                                  ║
╚═══════════════════════════════════════════════════════════════╝
");

app.Run();

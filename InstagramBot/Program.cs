using Microsoft.EntityFrameworkCore;
using Refit;
using InstagramBot.Data;
using InstagramBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Instagram Bot API", 
        Version = "v1",
        Description = "API for managing Instagram chatbots for multiple businesses"
    });
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Instagram API (Refit)
builder.Services
    .AddRefitClient<IInstagramApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://graph.facebook.com"));

// OpenAI
builder.Services.AddHttpClient<IOpenAiService, OpenAiService>();

// Message Handler
builder.Services.AddScoped<IMessageHandler, MessageHandler>();

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
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger (always enabled for easy API access)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Instagram Bot API v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

app.UseCors("AllowAll");
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    time = DateTime.UtcNow 
}));

Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════╗
║           Instagram Bot Platform Started!                  ║
╠═══════════════════════════════════════════════════════════╣
║  Swagger UI:     http://localhost:5000                    ║
║  Webhook URL:    http://localhost:5000/api/webhooks/instagram  ║
║  Health Check:   http://localhost:5000/health             ║
╚═══════════════════════════════════════════════════════════╝
");

app.Run();

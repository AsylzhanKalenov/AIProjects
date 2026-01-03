using System.Text.Json;
using System.Threading.RateLimiting;
using AIWebAPI.Interfaces;
using AIWebAPI.Persistence.Contexts;
using AIWebAPI.Services;
using Betalgo.Ranul.OpenAI.Extensions;
using Betalgo.Ranul.OpenAI.Interfaces;
using Betalgo.Ranul.OpenAI.Managers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Tool Instructor API", 
        Version = "v1",
        Description = "API for intelligent tool instruction system"
    });
        
    // Add Bearer token authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Context with connection resiliency
builder.Services.AddDbContext<ToolDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
});

// Vector Database Client
builder.Services.AddSingleton<QdrantClient>(sp =>
{
    var config = builder.Configuration.GetSection("Qdrant");
    return new QdrantClient(
        host: config["Host"],
        port: int.Parse(config["Port"]),
        https: bool.Parse(config["UseHttps"] ?? "false"),
        apiKey: config["ApiKey"]
    );
});

// OpenAI Service with Polly retry policy
builder.Services.AddHttpClient<IOpenAIService>();

builder.Services.AddOpenAIService(options =>
{
    options.ApiKey = builder.Configuration["OpenAI:ApiKey"] ?? "<OPENAI_API_KEY>";
});
// Custom Services
builder.Services.AddScoped<IToolService, ToolService>();
builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<IVectorService, VectorService>();
builder.Services.AddScoped<IFunctionToolService, FunctionToolService>();

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ToolInstructor";
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? new[] { "http://localhost:3000" };
                
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", cancellationToken: token);
    };
});

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowSpecificOrigins");
app.UseRateLimiter();

app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


public class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ToolDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseInitializer>>();

        try
        {
            // Apply migrations
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Seed data if needed
            if (!await context.Tools.AnyAsync())
            {
                await SeedDataAsync(context);
                logger.LogInformation("Database seeded successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    private static async Task SeedDataAsync(ToolDbContext context)
    {
        // Seed will be handled by OnModelCreating HasData
        await context.SaveChangesAsync();
    }
}
using Microsoft.EntityFrameworkCore;
using InstagramBot.Models;

namespace InstagramBot.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InstagramPageId).IsUnique();
            entity.Property(e => e.BusinessName).HasMaxLength(200);
            entity.Property(e => e.InstagramPageId).HasMaxLength(100);
            entity.Property(e => e.AccessToken).HasMaxLength(500);
            entity.Property(e => e.SystemPrompt).HasMaxLength(4000);
            entity.Property(e => e.KnowledgeBase);
            entity.Property(e => e.WelcomeMessage).HasMaxLength(1000);
            entity.Property(e => e.FallbackMessage).HasMaxLength(1000);
        });

        // Channel
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Unique: one ExternalId per channel type
            entity.HasIndex(e => new { e.Type, e.ExternalId }).IsUnique();
            
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.ExternalId).HasMaxLength(100);
            entity.Property(e => e.AccessToken).HasMaxLength(500);
            entity.Property(e => e.WhatsAppBusinessAccountId).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            
            // Enum stored as string for readability
            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Channels)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Conversation
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.InstagramUserId });
            entity.HasIndex(e => new { e.ChannelId, e.InstagramUserId });
            entity.Property(e => e.InstagramUserId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(200);
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Conversations)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Channel)
                .WithMany(c => c.Conversations)
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.Property(e => e.InstagramMessageId).HasMaxLength(200);
            
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed demo tenant
        var demoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        modelBuilder.Entity<Tenant>().HasData(new Tenant
        {
            Id = demoTenantId,
            BusinessName = "Demo Shop",
            InstagramPageId = "REPLACE_WITH_YOUR_PAGE_ID",
            AccessToken = "REPLACE_WITH_YOUR_ACCESS_TOKEN",
            SystemPrompt = @"Ты дружелюбный консультант магазина. 
Отвечай кратко и по делу. 
Если не знаешь ответ - предложи связаться с менеджером.
Всегда будь вежливым.",
            KnowledgeBase = @"
Информация о магазине:
- Доставка: 2-3 рабочих дня по городу
- Бесплатная доставка от 20000 тг
- Возврат: 14 дней с момента покупки
- Режим работы: Пн-Сб 10:00-20:00
- Контакт менеджера: +7 777 123 4567
",
            WelcomeMessage = "Здравствуйте! Я бот-консультант. Чем могу помочь?",
            FallbackMessage = "Извините, я не понял ваш вопрос. Попробуйте переформулировать или свяжитесь с менеджером.",
            IsActive = true,
            MonthlyMessageLimit = 1000,
            CreatedAt = DateTime.UtcNow
        });

        // Seed demo WhatsApp channel
        modelBuilder.Entity<Channel>().HasData(new Channel
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            TenantId = demoTenantId,
            Type = ChannelType.WhatsApp,
            DisplayName = "Demo WhatsApp",
            ExternalId = "REPLACE_WITH_PHONE_NUMBER_ID",
            AccessToken = "REPLACE_WITH_WHATSAPP_TOKEN",
            WhatsAppBusinessAccountId = "REPLACE_WITH_WABA_ID",
            PhoneNumber = "+7 777 000 0000",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }
}

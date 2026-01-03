using System.Text.Json;
using AIWebAPI.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIWebAPI.Persistence.Contexts;

public class ToolDbContext : DbContext
    {
        public ToolDbContext(DbContextOptions<ToolDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tool> Tools { get; set; }
        public DbSet<ToolCategory> ToolCategories { get; set; }
        public DbSet<ToolUsageInstruction> ToolUsageInstructions { get; set; }
        public DbSet<UserQuery> UserQueries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tool entity configuration
            modelBuilder.Entity<Tool>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Category).HasMaxLength(255);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Manufacturer).HasMaxLength(255);
                entity.Property(e => e.ModelNumber).HasMaxLength(255);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
                
                // JSON column for specifications
                entity.Property(e => e.Specifications)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Availability);
            });

            // ToolCategory entity configuration
            modelBuilder.Entity<ToolCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasColumnType("text");
                
                // Self-referencing relationship for parent category
                entity.HasOne(e => e.ParentCategory)
                    .WithMany(e => e.SubCategories)
                    .HasForeignKey(e => e.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ToolUsageInstruction entity configuration
            modelBuilder.Entity<ToolUsageInstruction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Instruction).IsRequired().HasColumnType("text");
                entity.Property(e => e.SafetyNotes).HasColumnType("text");
                
                // Relationship with Tool
                entity.HasOne(e => e.Tool)
                    .WithMany(e => e.Instructions)
                    .HasForeignKey(e => e.ToolId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => e.ToolId);
            });

            // UserQuery entity configuration
            modelBuilder.Entity<UserQuery>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.QueryText).IsRequired().HasColumnType("text");
                entity.Property(e => e.Response).HasColumnType("text");
                
                // JSON column for context
                entity.Property(e => e.Context)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                    .HasColumnType("jsonb");
                
                entity.HasIndex(e => e.Timestamp);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var toolIds = new[]
            {
                "183e34f1-1a42-4445-ae48-55d910b9e103", 
                "63f49afd-4383-4062-ac6d-d98633b19fc7", 
                "79660627-c0af-439b-b64c-329c7f96fcbd"
            };

            var seedCreatedAtUtc = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            var seedUpdatedAtUtc = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Tool>().HasData(
                new Tool
                {
                    Id = Guid.Parse(toolIds[0]),
                    Name = "Cordless Drill",
                    Category = "Power Tools",
                    Description = "High-performance 20V cordless drill with variable speed",
                    Specifications = new Dictionary<string, object>(),
                    Manufacturer = "DeWalt",
                    ModelNumber = "DCD777C2",
                    Price = 159.99m,
                    Availability = true,
                    CreatedAt = seedCreatedAtUtc,
                    UpdatedAt = seedUpdatedAtUtc
                },
                new Tool
                {
                    Id = Guid.Parse(toolIds[1]),
                    Name = "Circular Saw",
                    Category = "Power Tools",
                    Description = "7-1/4 inch circular saw with laser guide",
                    Specifications = new Dictionary<string, object>(),
                    Manufacturer = "Makita",
                    ModelNumber = "5007F",
                    Price = 129.99m,
                    Availability = true,
                    CreatedAt = seedCreatedAtUtc,
                    UpdatedAt = seedUpdatedAtUtc
                },
                new Tool
                {
                    Id = Guid.Parse(toolIds[2]),
                    Name = "Digital Multimeter",
                    Category = "Measuring Tools",
                    Description = "Professional digital multimeter with auto-ranging",
                    Specifications = new Dictionary<string, object>(),
                    Manufacturer = "Fluke",
                    ModelNumber = "115",
                    Price = 179.99m,
                    Availability = true,
                    CreatedAt = seedCreatedAtUtc,
                    UpdatedAt = seedUpdatedAtUtc
                }
            );
        }
    }
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AIWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToolCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolCategories_ToolCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ToolCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Specifications = table.Column<string>(type: "jsonb", nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ModelNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Availability = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QueryText = table.Column<string>(type: "text", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: false),
                    Context = table.Column<string>(type: "jsonb", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolUsageInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    SafetyNotes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolUsageInstructions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolUsageInstructions_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Tools",
                columns: new[] { "Id", "Availability", "Category", "CreatedAt", "Description", "Manufacturer", "ModelNumber", "Name", "Price", "Specifications", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("183e34f1-1a42-4445-ae48-55d910b9e103"), true, "Power Tools", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "High-performance 20V cordless drill with variable speed", "DeWalt", "DCD777C2", "Cordless Drill", 159.99m, "{}", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("63f49afd-4383-4062-ac6d-d98633b19fc7"), true, "Power Tools", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "7-1/4 inch circular saw with laser guide", "Makita", "5007F", "Circular Saw", 129.99m, "{}", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("79660627-c0af-439b-b64c-329c7f96fcbd"), true, "Measuring Tools", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Professional digital multimeter with auto-ranging", "Fluke", "115", "Digital Multimeter", 179.99m, "{}", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToolCategories_ParentCategoryId",
                table: "ToolCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_Availability",
                table: "Tools",
                column: "Availability");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_Category",
                table: "Tools",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_Name",
                table: "Tools",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ToolUsageInstructions_ToolId",
                table: "ToolUsageInstructions",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_UserQueries_Timestamp",
                table: "UserQueries",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToolCategories");

            migrationBuilder.DropTable(
                name: "ToolUsageInstructions");

            migrationBuilder.DropTable(
                name: "UserQueries");

            migrationBuilder.DropTable(
                name: "Tools");
        }
    }
}

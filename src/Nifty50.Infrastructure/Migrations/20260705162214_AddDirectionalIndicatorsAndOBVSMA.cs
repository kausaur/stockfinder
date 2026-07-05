using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nifty50.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectionalIndicatorsAndOBVSMA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexMemberships");

            migrationBuilder.AddColumn<decimal>(
                name: "MinusDI",
                table: "TechnicalIndicators",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OBVSMA20",
                table: "TechnicalIndicators",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlusDI",
                table: "TechnicalIndicators",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinusDI",
                table: "TechnicalIndicators");

            migrationBuilder.DropColumn(
                name: "OBVSMA20",
                table: "TechnicalIndicators");

            migrationBuilder.DropColumn(
                name: "PlusDI",
                table: "TechnicalIndicators");

            migrationBuilder.CreateTable(
                name: "IndexMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IndexName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndexMemberships_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndexMemberships_IndexName_StockId",
                table: "IndexMemberships",
                columns: new[] { "IndexName", "StockId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_IndexMemberships_StockId",
                table: "IndexMemberships",
                column: "StockId");
        }
    }
}

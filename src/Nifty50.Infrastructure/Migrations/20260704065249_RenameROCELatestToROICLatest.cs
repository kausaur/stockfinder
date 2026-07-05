using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nifty50.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameROCELatestToROICLatest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ROCELatest",
                table: "QualityMetrics",
                newName: "ROICLatest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ROICLatest",
                table: "QualityMetrics",
                newName: "ROCELatest");
        }
    }
}

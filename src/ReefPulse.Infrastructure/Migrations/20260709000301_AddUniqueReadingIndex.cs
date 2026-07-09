using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReefPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueReadingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_readings_ReefSiteId_Metric_ObservedAt_Source",
                table: "readings",
                columns: new[] { "ReefSiteId", "Metric", "ObservedAt", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_readings_ReefSiteId_Metric_ObservedAt_Source",
                table: "readings");
        }
    }
}

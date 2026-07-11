using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReefPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReefSiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Metric = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false),
                    TriggeredValue = table.Column<double>(type: "double precision", nullable: false),
                    TriggeredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alerts_reef_sites_ReefSiteId",
                        column: x => x.ReefSiteId,
                        principalTable: "reef_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_ReefSiteId_Status",
                table: "alerts",
                columns: new[] { "ReefSiteId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");
        }
    }
}

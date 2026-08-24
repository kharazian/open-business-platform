using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DashboardPublishingRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "published_menu_icon",
                table: "dashboards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "published_menu_label",
                table: "dashboards",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "published_menu_order",
                table: "dashboards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "published_show_in_navigation",
                table: "dashboards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "published_slug",
                table: "dashboards",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "published_snapshot_json",
                table: "dashboards",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "published_view_permission",
                table: "dashboards",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dashboard_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dashboard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    snapshot_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_dashboard_revisions_dashboards_dashboard_id",
                        column: x => x.dashboard_id,
                        principalTable: "dashboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dashboard_revisions_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_workspace_id_published_slug",
                table: "dashboards",
                columns: new[] { "workspace_id", "published_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_revisions_dashboard_id",
                table: "dashboard_revisions",
                column: "dashboard_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_revisions_workspace_id",
                table: "dashboard_revisions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_revisions_workspace_id_dashboard_id_revision_numb~",
                table: "dashboard_revisions",
                columns: new[] { "workspace_id", "dashboard_id", "revision_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dashboard_revisions");

            migrationBuilder.DropIndex(
                name: "IX_dashboards_workspace_id_published_slug",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_menu_icon",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_menu_label",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_menu_order",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_show_in_navigation",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_slug",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_snapshot_json",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_view_permission",
                table: "dashboards");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DashboardPublishingAndNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "menu_icon",
                table: "dashboards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "menu_label",
                table: "dashboards",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "menu_order",
                table: "dashboards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                table: "dashboards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "published_by_id",
                table: "dashboards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_in_navigation",
                table: "dashboards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "dashboards",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "dashboards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "draft");

            migrationBuilder.AddColumn<string>(
                name: "view_permission",
                table: "dashboards",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_workspace_id_slug",
                table: "dashboards",
                columns: new[] { "workspace_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_workspace_id_status_show_in_navigation",
                table: "dashboards",
                columns: new[] { "workspace_id", "status", "show_in_navigation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dashboards_workspace_id_slug",
                table: "dashboards");

            migrationBuilder.DropIndex(
                name: "IX_dashboards_workspace_id_status_show_in_navigation",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "menu_icon",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "menu_label",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "menu_order",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "published_by_id",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "show_in_navigation",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "status",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "view_permission",
                table: "dashboards");
        }
    }
}

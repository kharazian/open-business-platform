using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workspace_custom_domains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hostname = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    verification_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_failure = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_custom_domains", x => x.id);
                    table.ForeignKey(
                        name: "FK_workspace_custom_domains_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_custom_domains_hostname",
                table: "workspace_custom_domains",
                column: "hostname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_custom_domains_workspace_id",
                table: "workspace_custom_domains",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_custom_domains_workspace_id_status_is_enabled",
                table: "workspace_custom_domains",
                columns: new[] { "workspace_id", "status", "is_enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workspace_custom_domains");
        }
    }
}

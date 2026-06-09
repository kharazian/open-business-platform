using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrationApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    integration_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    scopes_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_used_ip = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    last_used_user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_api_keys_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_integration_api_keys_users_revoked_by_id",
                        column: x => x.revoked_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_created_by_id",
                table: "integration_api_keys",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_integration_key",
                table: "integration_api_keys",
                column: "integration_key");

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_is_active",
                table: "integration_api_keys",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_key_hash",
                table: "integration_api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_key_prefix",
                table: "integration_api_keys",
                column: "key_prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_last_used_at",
                table: "integration_api_keys",
                column: "last_used_at");

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_revoked_at",
                table: "integration_api_keys",
                column: "revoked_at");

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_revoked_by_id",
                table: "integration_api_keys",
                column: "revoked_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_api_keys");
        }
    }
}

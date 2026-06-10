using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IncomingWebhookListeners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incoming_webhook_listeners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    listener_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    target_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    auth_mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    secret_prefix = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    secret_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    safe_lookup_field_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    mapping_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incoming_webhook_listeners", x => x.id);
                    table.ForeignKey(
                        name: "FK_incoming_webhook_listeners_forms_target_form_id",
                        column: x => x.target_form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incoming_webhook_listeners_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incoming_webhook_listeners_created_by_id",
                table: "incoming_webhook_listeners",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_webhook_listeners_is_active",
                table: "incoming_webhook_listeners",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_webhook_listeners_listener_key",
                table: "incoming_webhook_listeners",
                column: "listener_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incoming_webhook_listeners_secret_prefix",
                table: "incoming_webhook_listeners",
                column: "secret_prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incoming_webhook_listeners_target_form_id",
                table: "incoming_webhook_listeners",
                column: "target_form_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incoming_webhook_listeners");
        }
    }
}

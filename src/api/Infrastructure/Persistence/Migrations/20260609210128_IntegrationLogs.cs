using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    integration_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    integration_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    target_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    is_retryable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    retry_next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_exhausted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_requested_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    response_metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_logs_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_integration_logs_users_retry_requested_by_id",
                        column: x => x.retry_requested_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_created_at",
                table: "integration_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_created_by_id",
                table: "integration_logs",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_direction",
                table: "integration_logs",
                column: "direction");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_integration_key",
                table: "integration_logs",
                column: "integration_key");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_integration_type",
                table: "integration_logs",
                column: "integration_type");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_retry_next_attempt_at",
                table: "integration_logs",
                column: "retry_next_attempt_at");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_retry_requested_by_id",
                table: "integration_logs",
                column: "retry_requested_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_source_type_source_id",
                table: "integration_logs",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_status",
                table: "integration_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_target_entity_type_target_entity_id",
                table: "integration_logs",
                columns: new[] { "target_entity_type", "target_entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_logs");
        }
    }
}

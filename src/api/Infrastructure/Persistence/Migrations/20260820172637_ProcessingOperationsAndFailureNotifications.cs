using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProcessingOperationsAndFailureNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JsonDocument>(
                name: "failure_notification_policy_json",
                table: "processing_job_definitions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{\"isEnabled\":false,\"includeOwner\":false,\"recipientUserIds\":[]}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "deduplication_key",
                table: "notifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "processing_operational_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    event_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    event_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    attempt = table.Column<int>(type: "integer", nullable: true),
                    max_attempts = table.Column<int>(type: "integer", nullable: true),
                    error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    duration_milliseconds = table.Column<long>(type: "bigint", nullable: true),
                    record_import_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_export_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_operational_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_processing_operational_logs_external_export_jobs_external_e~",
                        column: x => x.external_export_job_id,
                        principalTable: "external_export_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_processing_operational_logs_processing_job_definitions_defi~",
                        column: x => x.definition_id,
                        principalTable: "processing_job_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_processing_operational_logs_processing_job_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "processing_job_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_processing_operational_logs_record_import_jobs_record_impor~",
                        column: x => x.record_import_job_id,
                        principalTable: "record_import_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_processing_operational_logs_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_workspace_id_user_id_deduplication_key",
                table: "notifications",
                columns: new[] { "workspace_id", "user_id", "deduplication_key" },
                unique: true,
                filter: "\"deduplication_key\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_processing_operational_logs_definition_id_occurred_at",
                table: "processing_operational_logs",
                columns: new[] { "definition_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_processing_operational_logs_external_export_job_id",
                table: "processing_operational_logs",
                column: "external_export_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_operational_logs_record_import_job_id",
                table: "processing_operational_logs",
                column: "record_import_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_operational_logs_run_id_event_code",
                table: "processing_operational_logs",
                columns: new[] { "run_id", "event_code" });

            migrationBuilder.CreateIndex(
                name: "IX_processing_operational_logs_workspace_id",
                table: "processing_operational_logs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_operational_logs_workspace_id_event_key",
                table: "processing_operational_logs",
                columns: new[] { "workspace_id", "event_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processing_operational_logs_workspace_id_occurred_at",
                table: "processing_operational_logs",
                columns: new[] { "workspace_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_processing_operational_logs_workspace_id_severity_occurred_~",
                table: "processing_operational_logs",
                columns: new[] { "workspace_id", "severity", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processing_operational_logs");

            migrationBuilder.DropIndex(
                name: "IX_notifications_workspace_id_user_id_deduplication_key",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "failure_notification_policy_json",
                table: "processing_job_definitions");

            migrationBuilder.DropColumn(
                name: "deduplication_key",
                table: "notifications");
        }
    }
}

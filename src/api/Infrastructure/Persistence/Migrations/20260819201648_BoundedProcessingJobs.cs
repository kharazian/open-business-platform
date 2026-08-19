using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BoundedProcessingJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processing_job_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    config_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    schedule_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    retry_policy_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    next_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    schedule_locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    schedule_claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_job_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_processing_job_definitions_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_processing_job_definitions_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_processing_job_definitions_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_processing_job_definitions_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "processing_job_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    attempt = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    input_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    input_size_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    input_checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    input_content = table.Column<string>(type: "text", nullable: true),
                    record_import_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_export_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retry_source_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_job_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_processing_job_runs_external_export_jobs_external_export_jo~",
                        column: x => x.external_export_job_id,
                        principalTable: "external_export_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_processing_job_runs_processing_job_definitions_definition_id",
                        column: x => x.definition_id,
                        principalTable: "processing_job_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_processing_job_runs_processing_job_runs_retry_source_run_id",
                        column: x => x.retry_source_run_id,
                        principalTable: "processing_job_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_processing_job_runs_record_import_jobs_record_import_job_id",
                        column: x => x.record_import_job_id,
                        principalTable: "record_import_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_processing_job_runs_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_definitions_form_id",
                table: "processing_job_definitions",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_definitions_is_enabled_next_run_at",
                table: "processing_job_definitions",
                columns: new[] { "is_enabled", "next_run_at" });

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_definitions_kind",
                table: "processing_job_definitions",
                column: "kind");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_definitions_owner_user_id",
                table: "processing_job_definitions",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_definitions_report_id",
                table: "processing_job_definitions",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_definitions_workspace_id",
                table: "processing_job_definitions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_runs_created_at",
                table: "processing_job_runs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_runs_definition_id",
                table: "processing_job_runs",
                column: "definition_id",
                unique: true,
                filter: "\"status\" IN ('pending', 'running')");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_runs_external_export_job_id",
                table: "processing_job_runs",
                column: "external_export_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_runs_record_import_job_id",
                table: "processing_job_runs",
                column: "record_import_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_runs_retry_source_run_id",
                table: "processing_job_runs",
                column: "retry_source_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_runs_status_next_attempt_at",
                table: "processing_job_runs",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_processing_job_runs_workspace_id",
                table: "processing_job_runs",
                column: "workspace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processing_job_runs");

            migrationBuilder.DropTable(
                name: "processing_job_definitions");
        }
    }
}

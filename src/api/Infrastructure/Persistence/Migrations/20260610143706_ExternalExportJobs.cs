using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExternalExportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_export_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    format = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    integration_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: true),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    artifact_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    artifact_content_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    artifact_size_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    artifact_content = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    request_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    artifact_metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_export_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_export_jobs_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_external_export_jobs_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_external_export_jobs_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_external_export_jobs_created_at",
                table: "external_export_jobs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_external_export_jobs_created_by_id",
                table: "external_export_jobs",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_export_jobs_form_id",
                table: "external_export_jobs",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_export_jobs_report_id",
                table: "external_export_jobs",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_export_jobs_source_type",
                table: "external_export_jobs",
                column: "source_type");

            migrationBuilder.CreateIndex(
                name: "IX_external_export_jobs_status",
                table: "external_export_jobs",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_export_jobs");
        }
    }
}

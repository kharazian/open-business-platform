using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecordImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "record_import_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    succeeded_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    mapping_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record_import_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_record_import_jobs_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_import_jobs_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "record_import_job_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    errors_json = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record_import_job_rows", x => x.id);
                    table.ForeignKey(
                        name: "FK_record_import_job_rows_record_import_jobs_import_job_id",
                        column: x => x.import_job_id,
                        principalTable: "record_import_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_record_import_job_rows_records_record_id",
                        column: x => x.record_id,
                        principalTable: "records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_record_import_job_rows_import_job_id",
                table: "record_import_job_rows",
                column: "import_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_import_job_rows_import_job_id_row_number",
                table: "record_import_job_rows",
                columns: new[] { "import_job_id", "row_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_record_import_job_rows_record_id",
                table: "record_import_job_rows",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_import_job_rows_status",
                table: "record_import_job_rows",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_record_import_jobs_created_at",
                table: "record_import_jobs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_record_import_jobs_created_by_id",
                table: "record_import_jobs",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_import_jobs_form_id",
                table: "record_import_jobs",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_import_jobs_status",
                table: "record_import_jobs",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "record_import_job_rows");

            migrationBuilder.DropTable(
                name: "record_import_jobs");
        }
    }
}

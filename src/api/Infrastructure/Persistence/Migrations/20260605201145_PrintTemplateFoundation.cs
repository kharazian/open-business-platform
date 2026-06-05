using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrintTemplateFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "print_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    config_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_print_templates_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_templates_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_print_templates_created_by_id",
                table: "print_templates",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_print_templates_form_id",
                table: "print_templates",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_print_templates_report_id",
                table: "print_templates",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_print_templates_type",
                table: "print_templates",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "print_templates");
        }
    }
}

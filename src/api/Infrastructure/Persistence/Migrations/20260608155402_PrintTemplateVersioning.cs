using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrintTemplateVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "current_version_id",
                table: "print_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "print_template_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    print_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    config_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    published_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_template_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_print_template_versions_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_template_versions_print_templates_print_template_id",
                        column: x => x.print_template_id,
                        principalTable: "print_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_template_versions_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_print_templates_current_version_id",
                table: "print_templates",
                column: "current_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_print_template_versions_form_id",
                table: "print_template_versions",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_print_template_versions_print_template_id",
                table: "print_template_versions",
                column: "print_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_print_template_versions_print_template_id_version_number",
                table: "print_template_versions",
                columns: new[] { "print_template_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_print_template_versions_published_at",
                table: "print_template_versions",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "IX_print_template_versions_report_id",
                table: "print_template_versions",
                column: "report_id");

            migrationBuilder.AddForeignKey(
                name: "FK_print_templates_print_template_versions_current_version_id",
                table: "print_templates",
                column: "current_version_id",
                principalTable: "print_template_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_print_templates_print_template_versions_current_version_id",
                table: "print_templates");

            migrationBuilder.DropTable(
                name: "print_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_print_templates_current_version_id",
                table: "print_templates");

            migrationBuilder.DropColumn(
                name: "current_version_id",
                table: "print_templates");
        }
    }
}

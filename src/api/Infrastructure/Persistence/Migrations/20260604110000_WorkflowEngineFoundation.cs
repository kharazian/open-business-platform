using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OpenBusinessPlatformDbContext))]
    [Migration("20260604110000_WorkflowEngineFoundation")]
    public partial class WorkflowEngineFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    has_unpublished_changes = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    draft_config_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_definitions_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definition_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    config_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    published_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definition_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_definition_versions_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_definition_versions_workflow_definitions_workflow_definition_id",
                        column: x => x.workflow_definition_id,
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_state_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    to_state_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    transition_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_history_form_records_record_id",
                        column: x => x.record_id,
                        principalTable: "records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_history_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_history_workflow_definition_versions_workflow_definition_version_id",
                        column: x => x.workflow_definition_version_id,
                        principalTable: "workflow_definition_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_history_workflow_definitions_workflow_definition_id",
                        column: x => x.workflow_definition_id,
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_versions_form_id",
                table: "workflow_definition_versions",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_versions_workflow_definition_id",
                table: "workflow_definition_versions",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_versions_workflow_definition_id_version_number",
                table: "workflow_definition_versions",
                columns: new[] { "workflow_definition_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_current_version_id",
                table: "workflow_definitions",
                column: "current_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_form_id",
                table: "workflow_definitions",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_is_enabled",
                table: "workflow_definitions",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_status",
                table: "workflow_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_created_at",
                table: "workflow_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_form_id",
                table: "workflow_history",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_record_id",
                table: "workflow_history",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_workflow_definition_id",
                table: "workflow_history",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_workflow_definition_version_id",
                table: "workflow_history",
                column: "workflow_definition_version_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_definitions_workflow_definition_versions_current_version_id",
                table: "workflow_definitions",
                column: "current_version_id",
                principalTable: "workflow_definition_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_definitions_workflow_definition_versions_current_version_id",
                table: "workflow_definitions");

            migrationBuilder.DropTable(
                name: "workflow_history");

            migrationBuilder.DropTable(
                name: "workflow_definition_versions");

            migrationBuilder.DropTable(
                name: "workflow_definitions");
        }
    }
}

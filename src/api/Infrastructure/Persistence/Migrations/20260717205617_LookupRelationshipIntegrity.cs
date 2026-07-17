using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LookupRelationshipIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "record_relationships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_field_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    target_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record_relationships", x => x.id);
                    table.ForeignKey(
                        name: "FK_record_relationships_form_versions_source_form_version_id",
                        column: x => x.source_form_version_id,
                        principalTable: "form_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_relationships_forms_source_form_id",
                        column: x => x.source_form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_relationships_forms_target_form_id",
                        column: x => x.target_form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_relationships_records_source_record_id",
                        column: x => x.source_record_id,
                        principalTable: "records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_relationships_records_target_record_id",
                        column: x => x.target_record_id,
                        principalTable: "records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_relationships_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_record_relationships_source_form_id",
                table: "record_relationships",
                column: "source_form_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_relationships_source_form_version_id",
                table: "record_relationships",
                column: "source_form_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_relationships_source_record_id",
                table: "record_relationships",
                column: "source_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_relationships_target_form_id",
                table: "record_relationships",
                column: "target_form_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_relationships_target_record_id_source_record_id",
                table: "record_relationships",
                columns: new[] { "target_record_id", "source_record_id" });

            migrationBuilder.CreateIndex(
                name: "IX_record_relationships_workspace_id",
                table: "record_relationships",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_relationships_workspace_id_source_record_id_source_f~",
                table: "record_relationships",
                columns: new[] { "workspace_id", "source_record_id", "source_field_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "record_relationships");
        }
    }
}

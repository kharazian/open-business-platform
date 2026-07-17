using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackendGeneratedAutonumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "form_autonumber_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    next_value = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_autonumber_sequences", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_autonumber_sequences_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_form_autonumber_sequences_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_form_autonumber_sequences_form_id",
                table: "form_autonumber_sequences",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_autonumber_sequences_workspace_id",
                table: "form_autonumber_sequences",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_autonumber_sequences_workspace_id_form_id_field_id",
                table: "form_autonumber_sequences",
                columns: new[] { "workspace_id", "form_id", "field_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_autonumber_sequences");
        }
    }
}

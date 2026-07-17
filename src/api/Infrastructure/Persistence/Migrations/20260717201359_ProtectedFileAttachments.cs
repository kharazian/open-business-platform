using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectedFileAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    file_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    content_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    scan_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    attached_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_attachments_form_versions_form_version_id",
                        column: x => x.form_version_id,
                        principalTable: "form_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_attachments_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_attachments_records_record_id",
                        column: x => x.record_id,
                        principalTable: "records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_attachments_users_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_attachments_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_file_attachments_form_id_field_id_status",
                table: "file_attachments",
                columns: new[] { "form_id", "field_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_file_attachments_form_version_id",
                table: "file_attachments",
                column: "form_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_file_attachments_record_id",
                table: "file_attachments",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "IX_file_attachments_uploaded_by_id_status_created_at",
                table: "file_attachments",
                columns: new[] { "uploaded_by_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_file_attachments_workspace_id",
                table: "file_attachments",
                column: "workspace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_attachments");
        }
    }
}

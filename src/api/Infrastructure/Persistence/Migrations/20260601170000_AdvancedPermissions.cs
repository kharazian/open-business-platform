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
    [Migration("20260601170000_AdvancedPermissions")]
    public partial class AdvancedPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assigned_group_id",
                table: "records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_to_user_id",
                table: "records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "role_form_permissions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "all");

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_field_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    access = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_field_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_field_permissions_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_field_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_report_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_report_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_report_permissions_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_report_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_groups_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_groups_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_records_assigned_group_id",
                table: "records",
                column: "assigned_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_records_assigned_to_user_id",
                table: "records",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_name",
                table: "groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_field_permissions_form_id",
                table: "role_field_permissions",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_field_permissions_role_id",
                table: "role_field_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_field_permissions_role_id_form_id_field_id",
                table: "role_field_permissions",
                columns: new[] { "role_id", "form_id", "field_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_report_permissions_report_id",
                table: "role_report_permissions",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_report_permissions_role_id",
                table: "role_report_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_report_permissions_role_id_report_id_action",
                table: "role_report_permissions",
                columns: new[] { "role_id", "report_id", "action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_groups_group_id",
                table: "user_groups",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_groups_user_id",
                table: "user_groups",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_groups_user_id_group_id",
                table: "user_groups",
                columns: new[] { "user_id", "group_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_records_groups_assigned_group_id",
                table: "records",
                column: "assigned_group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_records_users_assigned_to_user_id",
                table: "records",
                column: "assigned_to_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_records_groups_assigned_group_id",
                table: "records");

            migrationBuilder.DropForeignKey(
                name: "FK_records_users_assigned_to_user_id",
                table: "records");

            migrationBuilder.DropTable(name: "role_field_permissions");

            migrationBuilder.DropTable(name: "role_report_permissions");

            migrationBuilder.DropTable(name: "user_groups");

            migrationBuilder.DropTable(name: "groups");

            migrationBuilder.DropIndex(name: "IX_records_assigned_group_id", table: "records");

            migrationBuilder.DropIndex(name: "IX_records_assigned_to_user_id", table: "records");

            migrationBuilder.DropColumn(name: "assigned_group_id", table: "records");

            migrationBuilder.DropColumn(name: "assigned_to_user_id", table: "records");

            migrationBuilder.DropColumn(name: "scope", table: "role_form_permissions");
        }
    }
}

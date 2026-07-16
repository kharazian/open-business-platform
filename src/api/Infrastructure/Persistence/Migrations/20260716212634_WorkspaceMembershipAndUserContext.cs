using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkspaceMembershipAndUserContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workspace_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    invited_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    extra_properties_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_workspace_memberships_users_invited_by_id",
                        column: x => x.invited_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_workspace_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workspace_memberships_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO workspace_memberships (
                    id,
                    workspace_id,
                    user_id,
                    role,
                    status,
                    is_default,
                    invited_at,
                    activated_at,
                    suspended_at,
                    concurrency_stamp,
                    created_at)
                SELECT
                    md5(user_account.id::text || ':default-workspace-membership')::uuid,
                    '00000000-0000-0000-0000-000000000002'::uuid,
                    user_account.id,
                    CASE WHEN EXISTS (
                        SELECT 1
                        FROM user_roles user_role
                        INNER JOIN roles role ON role.id = user_role.role_id
                        WHERE user_role.user_id = user_account.id
                          AND user_role.workspace_id = '00000000-0000-0000-0000-000000000002'::uuid
                          AND lower(role.name) = 'admin'
                    ) THEN 'admin' ELSE 'member' END,
                    CASE WHEN user_account.is_active THEN 'active' ELSE 'suspended' END,
                    user_account.is_active,
                    CURRENT_TIMESTAMP,
                    CASE WHEN user_account.is_active THEN CURRENT_TIMESTAMP ELSE NULL END,
                    CASE WHEN user_account.is_active THEN NULL ELSE CURRENT_TIMESTAMP END,
                    md5(user_account.id::text || ':workspace-membership-stamp'),
                    CURRENT_TIMESTAMP
                FROM users user_account
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM workspace_memberships membership
                    WHERE membership.workspace_id = '00000000-0000-0000-0000-000000000002'::uuid
                      AND membership.user_id = user_account.id
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_memberships_invited_by_id",
                table: "workspace_memberships",
                column: "invited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_memberships_user_id",
                table: "workspace_memberships",
                column: "user_id",
                unique: true,
                filter: "\"is_default\" = TRUE AND \"status\" = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_memberships_user_id_status",
                table: "workspace_memberships",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_memberships_workspace_id_status",
                table: "workspace_memberships",
                columns: new[] { "workspace_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_memberships_workspace_id_user_id",
                table: "workspace_memberships",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workspace_memberships");
        }
    }
}

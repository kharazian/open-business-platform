using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleDefaultWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspaces_tenant_id_is_default",
                table: "workspaces");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_tenant_id",
                table: "workspaces",
                column: "tenant_id",
                unique: true,
                filter: "\"is_default\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspaces_tenant_id",
                table: "workspaces");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_tenant_id_is_default",
                table: "workspaces",
                columns: new[] { "tenant_id", "is_default" });
        }
    }
}

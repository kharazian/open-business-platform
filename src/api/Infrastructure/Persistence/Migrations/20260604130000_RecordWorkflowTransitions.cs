using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecordWorkflowTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "records",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_definition_id",
                table: "records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workflow_definition_version_id",
                table: "records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "workflow_state_key",
                table: "records",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_records_workflow_definition_id",
                table: "records",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_records_workflow_definition_version_id",
                table: "records",
                column: "workflow_definition_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_records_workflow_state_key",
                table: "records",
                column: "workflow_state_key");

            migrationBuilder.AddForeignKey(
                name: "FK_records_workflow_definition_versions_workflow_definition_version_id",
                table: "records",
                column: "workflow_definition_version_id",
                principalTable: "workflow_definition_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_records_workflow_definitions_workflow_definition_id",
                table: "records",
                column: "workflow_definition_id",
                principalTable: "workflow_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_records_workflow_definition_versions_workflow_definition_version_id",
                table: "records");

            migrationBuilder.DropForeignKey(
                name: "FK_records_workflow_definitions_workflow_definition_id",
                table: "records");

            migrationBuilder.DropIndex(
                name: "IX_records_workflow_definition_id",
                table: "records");

            migrationBuilder.DropIndex(
                name: "IX_records_workflow_definition_version_id",
                table: "records");

            migrationBuilder.DropIndex(
                name: "IX_records_workflow_state_key",
                table: "records");

            migrationBuilder.DropColumn(
                name: "workflow_definition_id",
                table: "records");

            migrationBuilder.DropColumn(
                name: "workflow_definition_version_id",
                table: "records");

            migrationBuilder.DropColumn(
                name: "workflow_state_key",
                table: "records");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);
        }
    }
}

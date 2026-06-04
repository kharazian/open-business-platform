using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowApprovalInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_approval_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_step_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    approval_step_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    transition_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    transition_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_state_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    to_state_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_approval_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_approval_tasks_form_records_record_id",
                        column: x => x.record_id,
                        principalTable: "records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_approval_tasks_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_approval_tasks_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_approval_tasks_users_requested_by_id",
                        column: x => x.requested_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_workflow_approval_tasks_users_responded_by_id",
                        column: x => x.responded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_workflow_approval_tasks_workflow_definition_versions_workflow_definition_version_id",
                        column: x => x.workflow_definition_version_id,
                        principalTable: "workflow_definition_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_approval_tasks_workflow_definitions_workflow_definition_id",
                        column: x => x.workflow_definition_id,
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_approval_group_id",
                table: "workflow_approval_tasks",
                column: "approval_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_assigned_to_user_id_status",
                table: "workflow_approval_tasks",
                columns: new[] { "assigned_to_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_form_id",
                table: "workflow_approval_tasks",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_record_id_transition_key_status",
                table: "workflow_approval_tasks",
                columns: new[] { "record_id", "transition_key", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_requested_by_id",
                table: "workflow_approval_tasks",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_responded_by_id",
                table: "workflow_approval_tasks",
                column: "responded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_workflow_definition_id",
                table: "workflow_approval_tasks",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_workflow_definition_version_id",
                table: "workflow_approval_tasks",
                column: "workflow_definition_version_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_approval_tasks");
        }
    }
}

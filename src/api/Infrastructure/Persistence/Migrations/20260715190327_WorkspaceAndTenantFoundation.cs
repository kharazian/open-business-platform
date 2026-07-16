using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkspaceAndTenantFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_roles_name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_integration_connectors_connector_key",
                table: "integration_connectors");

            migrationBuilder.DropIndex(
                name: "IX_groups_name",
                table: "groups");

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "workflow_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "workflow_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "workflow_definition_versions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "workflow_approval_tasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "user_roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "user_groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "user_departments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "triggers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "trigger_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "trigger_event_outbox",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "role_report_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "role_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "role_form_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "role_field_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "records",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "record_import_jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "record_import_job_rows",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "print_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "print_template_versions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "notifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "integration_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "integration_connectors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "integration_api_keys",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "incoming_webhook_listeners",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "forms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "form_versions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "external_export_jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "departments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "dashboards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "audit_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_workspaces", x => x.id);
                    table.ForeignKey(
                        name: "FK_workspaces_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO tenants (
                    id, name, slug, is_active, concurrency_stamp, created_at)
                VALUES (
                    '00000000-0000-0000-0000-000000000001'::uuid,
                    'Open Business Platform',
                    'default-tenant',
                    TRUE,
                    'v9-default-tenant',
                    CURRENT_TIMESTAMP);

                INSERT INTO workspaces (
                    id, tenant_id, name, slug, is_default, is_active, concurrency_stamp, created_at)
                VALUES (
                    '00000000-0000-0000-0000-000000000002'::uuid,
                    '00000000-0000-0000-0000-000000000001'::uuid,
                    'Default Workspace',
                    'default-workspace',
                    TRUE,
                    TRUE,
                    'v9-default-workspace',
                    CURRENT_TIMESTAMP);

                DO $workspace_backfill$
                DECLARE
                    workspace_table text;
                BEGIN
                    FOREACH workspace_table IN ARRAY ARRAY[
                        'audit_logs',
                        'dashboards',
                        'departments',
                        'external_export_jobs',
                        'form_versions',
                        'forms',
                        'groups',
                        'incoming_webhook_listeners',
                        'integration_api_keys',
                        'integration_connectors',
                        'integration_logs',
                        'notifications',
                        'print_template_versions',
                        'print_templates',
                        'record_import_job_rows',
                        'record_import_jobs',
                        'records',
                        'reports',
                        'role_field_permissions',
                        'role_form_permissions',
                        'role_permissions',
                        'role_report_permissions',
                        'roles',
                        'trigger_event_outbox',
                        'trigger_logs',
                        'triggers',
                        'user_departments',
                        'user_groups',
                        'user_roles',
                        'workflow_approval_tasks',
                        'workflow_definition_versions',
                        'workflow_definitions',
                        'workflow_history'
                    ]
                    LOOP
                        EXECUTE format(
                            'UPDATE %I SET workspace_id = %L::uuid',
                            workspace_table,
                            '00000000-0000-0000-0000-000000000002');
                        EXECUTE format(
                            'ALTER TABLE %I ALTER COLUMN workspace_id DROP DEFAULT',
                            workspace_table);
                    END LOOP;
                END
                $workspace_backfill$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_workspace_id",
                table: "workflow_history",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_workspace_id",
                table: "workflow_definitions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_versions_workspace_id",
                table: "workflow_definition_versions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approval_tasks_workspace_id",
                table: "workflow_approval_tasks",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_workspace_id",
                table: "user_roles",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_groups_workspace_id",
                table: "user_groups",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_departments_workspace_id",
                table: "user_departments",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_triggers_workspace_id",
                table: "triggers",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_trigger_logs_workspace_id",
                table: "trigger_logs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_trigger_event_outbox_workspace_id",
                table: "trigger_event_outbox",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_workspace_id",
                table: "roles",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_workspace_id_name",
                table: "roles",
                columns: new[] { "workspace_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_report_permissions_workspace_id",
                table: "role_report_permissions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_workspace_id",
                table: "role_permissions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_form_permissions_workspace_id",
                table: "role_form_permissions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_field_permissions_workspace_id",
                table: "role_field_permissions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_workspace_id",
                table: "reports",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_records_workspace_id",
                table: "records",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_import_jobs_workspace_id",
                table: "record_import_jobs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_record_import_job_rows_workspace_id",
                table: "record_import_job_rows",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_print_templates_workspace_id",
                table: "print_templates",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_print_template_versions_workspace_id",
                table: "print_template_versions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_workspace_id",
                table: "notifications",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_logs_workspace_id",
                table: "integration_logs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_connectors_workspace_id",
                table: "integration_connectors",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_integration_connectors_workspace_id_connector_key",
                table: "integration_connectors",
                columns: new[] { "workspace_id", "connector_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_api_keys_workspace_id",
                table: "integration_api_keys",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_incoming_webhook_listeners_workspace_id",
                table: "incoming_webhook_listeners",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_workspace_id",
                table: "groups",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_workspace_id_name",
                table: "groups",
                columns: new[] { "workspace_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_forms_workspace_id",
                table: "forms",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_versions_workspace_id",
                table: "form_versions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_export_jobs_workspace_id",
                table: "external_export_jobs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_workspace_id",
                table: "departments",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_workspace_id",
                table: "dashboards",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_workspace_id",
                table: "audit_logs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_is_active",
                table: "tenants",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_slug",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_is_active",
                table: "workspaces",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_tenant_id_is_default",
                table: "workspaces",
                columns: new[] { "tenant_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_tenant_id_slug",
                table: "workspaces",
                columns: new[] { "tenant_id", "slug" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_workspaces_workspace_id",
                table: "audit_logs",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dashboards_workspaces_workspace_id",
                table: "dashboards",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_departments_workspaces_workspace_id",
                table: "departments",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_external_export_jobs_workspaces_workspace_id",
                table: "external_export_jobs",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_form_versions_workspaces_workspace_id",
                table: "form_versions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_forms_workspaces_workspace_id",
                table: "forms",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_groups_workspaces_workspace_id",
                table: "groups",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_incoming_webhook_listeners_workspaces_workspace_id",
                table: "incoming_webhook_listeners",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_integration_api_keys_workspaces_workspace_id",
                table: "integration_api_keys",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_integration_connectors_workspaces_workspace_id",
                table: "integration_connectors",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_integration_logs_workspaces_workspace_id",
                table: "integration_logs",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_workspaces_workspace_id",
                table: "notifications",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_print_template_versions_workspaces_workspace_id",
                table: "print_template_versions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_print_templates_workspaces_workspace_id",
                table: "print_templates",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_record_import_job_rows_workspaces_workspace_id",
                table: "record_import_job_rows",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_record_import_jobs_workspaces_workspace_id",
                table: "record_import_jobs",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_records_workspaces_workspace_id",
                table: "records",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reports_workspaces_workspace_id",
                table: "reports",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_role_field_permissions_workspaces_workspace_id",
                table: "role_field_permissions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_role_form_permissions_workspaces_workspace_id",
                table: "role_form_permissions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_role_permissions_workspaces_workspace_id",
                table: "role_permissions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_role_report_permissions_workspaces_workspace_id",
                table: "role_report_permissions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_roles_workspaces_workspace_id",
                table: "roles",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_trigger_event_outbox_workspaces_workspace_id",
                table: "trigger_event_outbox",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_trigger_logs_workspaces_workspace_id",
                table: "trigger_logs",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_triggers_workspaces_workspace_id",
                table: "triggers",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_departments_workspaces_workspace_id",
                table: "user_departments",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_groups_workspaces_workspace_id",
                table: "user_groups",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_workspaces_workspace_id",
                table: "user_roles",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_approval_tasks_workspaces_workspace_id",
                table: "workflow_approval_tasks",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_definition_versions_workspaces_workspace_id",
                table: "workflow_definition_versions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_definitions_workspaces_workspace_id",
                table: "workflow_definitions",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_history_workspaces_workspace_id",
                table: "workflow_history",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_workspaces_workspace_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_dashboards_workspaces_workspace_id",
                table: "dashboards");

            migrationBuilder.DropForeignKey(
                name: "FK_departments_workspaces_workspace_id",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "FK_external_export_jobs_workspaces_workspace_id",
                table: "external_export_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_form_versions_workspaces_workspace_id",
                table: "form_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_forms_workspaces_workspace_id",
                table: "forms");

            migrationBuilder.DropForeignKey(
                name: "FK_groups_workspaces_workspace_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "FK_incoming_webhook_listeners_workspaces_workspace_id",
                table: "incoming_webhook_listeners");

            migrationBuilder.DropForeignKey(
                name: "FK_integration_api_keys_workspaces_workspace_id",
                table: "integration_api_keys");

            migrationBuilder.DropForeignKey(
                name: "FK_integration_connectors_workspaces_workspace_id",
                table: "integration_connectors");

            migrationBuilder.DropForeignKey(
                name: "FK_integration_logs_workspaces_workspace_id",
                table: "integration_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_workspaces_workspace_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_print_template_versions_workspaces_workspace_id",
                table: "print_template_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_print_templates_workspaces_workspace_id",
                table: "print_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_record_import_job_rows_workspaces_workspace_id",
                table: "record_import_job_rows");

            migrationBuilder.DropForeignKey(
                name: "FK_record_import_jobs_workspaces_workspace_id",
                table: "record_import_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_records_workspaces_workspace_id",
                table: "records");

            migrationBuilder.DropForeignKey(
                name: "FK_reports_workspaces_workspace_id",
                table: "reports");

            migrationBuilder.DropForeignKey(
                name: "FK_role_field_permissions_workspaces_workspace_id",
                table: "role_field_permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_role_form_permissions_workspaces_workspace_id",
                table: "role_form_permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_role_permissions_workspaces_workspace_id",
                table: "role_permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_role_report_permissions_workspaces_workspace_id",
                table: "role_report_permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_roles_workspaces_workspace_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "FK_trigger_event_outbox_workspaces_workspace_id",
                table: "trigger_event_outbox");

            migrationBuilder.DropForeignKey(
                name: "FK_trigger_logs_workspaces_workspace_id",
                table: "trigger_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_triggers_workspaces_workspace_id",
                table: "triggers");

            migrationBuilder.DropForeignKey(
                name: "FK_user_departments_workspaces_workspace_id",
                table: "user_departments");

            migrationBuilder.DropForeignKey(
                name: "FK_user_groups_workspaces_workspace_id",
                table: "user_groups");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_workspaces_workspace_id",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_approval_tasks_workspaces_workspace_id",
                table: "workflow_approval_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_definition_versions_workspaces_workspace_id",
                table: "workflow_definition_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_definitions_workspaces_workspace_id",
                table: "workflow_definitions");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_history_workspaces_workspace_id",
                table: "workflow_history");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_workflow_history_workspace_id",
                table: "workflow_history");

            migrationBuilder.DropIndex(
                name: "IX_workflow_definitions_workspace_id",
                table: "workflow_definitions");

            migrationBuilder.DropIndex(
                name: "IX_workflow_definition_versions_workspace_id",
                table: "workflow_definition_versions");

            migrationBuilder.DropIndex(
                name: "IX_workflow_approval_tasks_workspace_id",
                table: "workflow_approval_tasks");

            migrationBuilder.DropIndex(
                name: "IX_user_roles_workspace_id",
                table: "user_roles");

            migrationBuilder.DropIndex(
                name: "IX_user_groups_workspace_id",
                table: "user_groups");

            migrationBuilder.DropIndex(
                name: "IX_user_departments_workspace_id",
                table: "user_departments");

            migrationBuilder.DropIndex(
                name: "IX_triggers_workspace_id",
                table: "triggers");

            migrationBuilder.DropIndex(
                name: "IX_trigger_logs_workspace_id",
                table: "trigger_logs");

            migrationBuilder.DropIndex(
                name: "IX_trigger_event_outbox_workspace_id",
                table: "trigger_event_outbox");

            migrationBuilder.DropIndex(
                name: "IX_roles_workspace_id",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_workspace_id_name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_role_report_permissions_workspace_id",
                table: "role_report_permissions");

            migrationBuilder.DropIndex(
                name: "IX_role_permissions_workspace_id",
                table: "role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_role_form_permissions_workspace_id",
                table: "role_form_permissions");

            migrationBuilder.DropIndex(
                name: "IX_role_field_permissions_workspace_id",
                table: "role_field_permissions");

            migrationBuilder.DropIndex(
                name: "IX_reports_workspace_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "IX_records_workspace_id",
                table: "records");

            migrationBuilder.DropIndex(
                name: "IX_record_import_jobs_workspace_id",
                table: "record_import_jobs");

            migrationBuilder.DropIndex(
                name: "IX_record_import_job_rows_workspace_id",
                table: "record_import_job_rows");

            migrationBuilder.DropIndex(
                name: "IX_print_templates_workspace_id",
                table: "print_templates");

            migrationBuilder.DropIndex(
                name: "IX_print_template_versions_workspace_id",
                table: "print_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_notifications_workspace_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_integration_logs_workspace_id",
                table: "integration_logs");

            migrationBuilder.DropIndex(
                name: "IX_integration_connectors_workspace_id",
                table: "integration_connectors");

            migrationBuilder.DropIndex(
                name: "IX_integration_connectors_workspace_id_connector_key",
                table: "integration_connectors");

            migrationBuilder.DropIndex(
                name: "IX_integration_api_keys_workspace_id",
                table: "integration_api_keys");

            migrationBuilder.DropIndex(
                name: "IX_incoming_webhook_listeners_workspace_id",
                table: "incoming_webhook_listeners");

            migrationBuilder.DropIndex(
                name: "IX_groups_workspace_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "IX_groups_workspace_id_name",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "IX_forms_workspace_id",
                table: "forms");

            migrationBuilder.DropIndex(
                name: "IX_form_versions_workspace_id",
                table: "form_versions");

            migrationBuilder.DropIndex(
                name: "IX_external_export_jobs_workspace_id",
                table: "external_export_jobs");

            migrationBuilder.DropIndex(
                name: "IX_departments_workspace_id",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_dashboards_workspace_id",
                table: "dashboards");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_workspace_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "workflow_history");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "workflow_definitions");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "workflow_definition_versions");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "workflow_approval_tasks");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "user_groups");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "user_departments");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "triggers");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "trigger_logs");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "trigger_event_outbox");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "role_report_permissions");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "role_permissions");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "role_form_permissions");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "role_field_permissions");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "records");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "record_import_jobs");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "record_import_job_rows");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "print_templates");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "print_template_versions");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "integration_logs");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "integration_connectors");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "integration_api_keys");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "incoming_webhook_listeners");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "forms");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "form_versions");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "external_export_jobs");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "dashboards");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_connectors_connector_key",
                table: "integration_connectors",
                column: "connector_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_groups_name",
                table: "groups",
                column: "name",
                unique: true);
        }
    }
}

using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TransactionalTriggerEventOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trigger_event_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    payload_json = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trigger_event_outbox", x => x.id);
                    table.ForeignKey(
                        name: "FK_trigger_event_outbox_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trigger_event_outbox_records_record_id",
                        column: x => x.record_id,
                        principalTable: "records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trigger_event_outbox_form_id",
                table: "trigger_event_outbox",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_trigger_event_outbox_locked_at",
                table: "trigger_event_outbox",
                column: "locked_at");

            migrationBuilder.CreateIndex(
                name: "IX_trigger_event_outbox_record_id",
                table: "trigger_event_outbox",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "IX_trigger_event_outbox_status_next_attempt_at",
                table: "trigger_event_outbox",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trigger_event_outbox");
        }
    }
}

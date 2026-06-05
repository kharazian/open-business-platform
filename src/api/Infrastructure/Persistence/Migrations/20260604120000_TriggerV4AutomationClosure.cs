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
    [Migration("20260604120000_TriggerV4AutomationClosure")]
    public partial class TriggerV4AutomationClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "auto_retry_enabled",
                table: "triggers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "auto_retry_max_attempts",
                table: "triggers",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "auto_retry_delay_seconds",
                table: "triggers",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "schedule_json",
                table: "triggers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "schedule_next_run_at",
                table: "triggers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "schedule_last_run_at",
                table: "triggers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_triggers_schedule_next_run_at",
                table: "triggers",
                column: "schedule_next_run_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_triggers_schedule_next_run_at",
                table: "triggers");

            migrationBuilder.DropColumn(
                name: "auto_retry_enabled",
                table: "triggers");

            migrationBuilder.DropColumn(
                name: "auto_retry_max_attempts",
                table: "triggers");

            migrationBuilder.DropColumn(
                name: "auto_retry_delay_seconds",
                table: "triggers");

            migrationBuilder.DropColumn(
                name: "schedule_json",
                table: "triggers");

            migrationBuilder.DropColumn(
                name: "schedule_next_run_at",
                table: "triggers");

            migrationBuilder.DropColumn(
                name: "schedule_last_run_at",
                table: "triggers");
        }
    }
}

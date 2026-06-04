using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBusinessPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TriggerAutomaticRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "auto_retry_attempt_count",
                table: "trigger_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "auto_retry_completed_at",
                table: "trigger_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "auto_retry_disabled_at",
                table: "trigger_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "auto_retry_exhausted_at",
                table: "trigger_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "auto_retry_locked_at",
                table: "trigger_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "auto_retry_max_attempts",
                table: "trigger_logs",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "auto_retry_next_attempt_at",
                table: "trigger_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_trigger_logs_auto_retry_next_attempt_at",
                table: "trigger_logs",
                column: "auto_retry_next_attempt_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trigger_logs_auto_retry_next_attempt_at",
                table: "trigger_logs");

            migrationBuilder.DropColumn(
                name: "auto_retry_attempt_count",
                table: "trigger_logs");

            migrationBuilder.DropColumn(
                name: "auto_retry_completed_at",
                table: "trigger_logs");

            migrationBuilder.DropColumn(
                name: "auto_retry_disabled_at",
                table: "trigger_logs");

            migrationBuilder.DropColumn(
                name: "auto_retry_exhausted_at",
                table: "trigger_logs");

            migrationBuilder.DropColumn(
                name: "auto_retry_locked_at",
                table: "trigger_logs");

            migrationBuilder.DropColumn(
                name: "auto_retry_max_attempts",
                table: "trigger_logs");

            migrationBuilder.DropColumn(
                name: "auto_retry_next_attempt_at",
                table: "trigger_logs");
        }
    }
}

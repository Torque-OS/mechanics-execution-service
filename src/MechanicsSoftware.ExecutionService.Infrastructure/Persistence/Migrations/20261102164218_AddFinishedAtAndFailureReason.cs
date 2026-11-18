using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicsSoftware.ExecutionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinishedAtAndFailureReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "execution_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "finished_at",
                table: "execution_orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "execution_orders");

            migrationBuilder.DropColumn(
                name: "finished_at",
                table: "execution_orders");
        }
    }
}

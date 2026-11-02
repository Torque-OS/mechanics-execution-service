using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicsSoftware.ExecutionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStartedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                table: "execution_orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "started_at",
                table: "execution_orders");
        }
    }
}

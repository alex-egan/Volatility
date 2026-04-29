using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class BeverageLastPurchasedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoUpdateDueAt",
                table: "Beverages");

            migrationBuilder.DropColumn(
                name: "PendingAutoUpdate",
                table: "Beverages");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "Beverages");

            migrationBuilder.DropColumn(
                name: "ProcessingToken",
                table: "Beverages");

            migrationBuilder.RenameColumn(
                name: "LastUpdateRequestedAt",
                table: "Beverages",
                newName: "LastPurchasedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastPurchasedOn",
                table: "Beverages",
                newName: "LastUpdateRequestedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "AutoUpdateDueAt",
                table: "Beverages",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "PendingAutoUpdate",
                table: "Beverages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                table: "Beverages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessingToken",
                table: "Beverages",
                type: "TEXT",
                nullable: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class BeverageEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeverageEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BeverageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    PerformedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeverageEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeverageEvents_Beverages_BeverageId",
                        column: x => x.BeverageId,
                        principalTable: "Beverages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeverageEvents_BeverageId",
                table: "BeverageEvents",
                column: "BeverageId");

            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS LogPriceDecrease
                AFTER UPDATE OF Price ON Beverages
                FOR EACH ROW
                WHEN NEW.Price < OLD.Price
                BEGIN
                    INSERT INTO BeverageEvents (
                        BeverageId,
                        Price,
                        Type,
                        PerformedOn
                    )
                    VALUES (
                        OLD.Id,
                        NEW.Price,
                        'PRICE_DECREASED',
                        CURRENT_TIMESTAMP
                    );
                END;
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeverageEvents");
        }
    }
}

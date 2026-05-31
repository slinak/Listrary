using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lists",
                columns: table => new
                {
                    ListID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ListType = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lists", x => x.ListID);
                });

            migrationBuilder.CreateTable(
                name: "ListItems",
                columns: table => new
                {
                    ListItemID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListID = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ItemDetails = table.Column<string>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListItems", x => x.ListItemID);
                    table.ForeignKey(
                        name: "FK_ListItems_Lists_ListID",
                        column: x => x.ListID,
                        principalTable: "Lists",
                        principalColumn: "ListID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListItems_ListID",
                table: "ListItems",
                column: "ListID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListItems");

            migrationBuilder.DropTable(
                name: "Lists");
        }
    }
}

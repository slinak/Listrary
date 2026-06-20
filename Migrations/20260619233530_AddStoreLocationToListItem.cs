using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListManager.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreLocationToListItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoreLocation",
                table: "ListItems",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreLocation",
                table: "ListItems");
        }
    }
}

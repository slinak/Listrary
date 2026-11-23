using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListManager.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeIngredientSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "RecipeIngredients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Section",
                table: "RecipeIngredients");
        }
    }
}

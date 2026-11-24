using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListManager.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipeTags",
                columns: table => new
                {
                    RecipeTagID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeTags", x => x.RecipeTagID);
                    table.ForeignKey(
                        name: "FK_RecipeTags_Recipes_RecipeID",
                        column: x => x.RecipeID,
                        principalTable: "Recipes",
                        principalColumn: "RecipeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeTags_Name",
                table: "RecipeTags",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeTags_RecipeID_Name",
                table: "RecipeTags",
                columns: new[] { "RecipeID", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeTags");
        }
    }
}

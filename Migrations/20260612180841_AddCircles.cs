using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListManager.Migrations
{
    /// <inheritdoc />
    public partial class AddCircles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CircleId",
                table: "Lists",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Circles",
                columns: table => new
                {
                    CircleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circles", x => x.CircleId);
                });

            migrationBuilder.CreateTable(
                name: "CircleInvitations",
                columns: table => new
                {
                    CircleInvitationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CircleId = table.Column<int>(type: "INTEGER", nullable: false),
                    InviterId = table.Column<string>(type: "TEXT", nullable: false),
                    RecipientEmail = table.Column<string>(type: "TEXT", nullable: true),
                    RecipientId = table.Column<string>(type: "TEXT", nullable: true),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    Accepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleInvitations", x => x.CircleInvitationId);
                    table.ForeignKey(
                        name: "FK_CircleInvitations_Circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "Circles",
                        principalColumn: "CircleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CircleMembers",
                columns: table => new
                {
                    CircleMemberId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CircleId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    JoinedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleMembers", x => x.CircleMemberId);
                    table.ForeignKey(
                        name: "FK_CircleMembers_Circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "Circles",
                        principalColumn: "CircleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lists_CircleId",
                table: "Lists",
                column: "CircleId");

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_CircleId",
                table: "CircleInvitations",
                column: "CircleId");

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_Token",
                table: "CircleInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_CircleId_UserId",
                table: "CircleMembers",
                columns: new[] { "CircleId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lists_Circles_CircleId",
                table: "Lists",
                column: "CircleId",
                principalTable: "Circles",
                principalColumn: "CircleId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lists_Circles_CircleId",
                table: "Lists");

            migrationBuilder.DropTable(
                name: "CircleInvitations");

            migrationBuilder.DropTable(
                name: "CircleMembers");

            migrationBuilder.DropTable(
                name: "Circles");

            migrationBuilder.DropIndex(
                name: "IX_Lists_CircleId",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "CircleId",
                table: "Lists");
        }
    }
}

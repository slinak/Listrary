using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderToListItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CircleInvitations_Circles_CircleId",
                table: "CircleInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_CircleMembers_Circles_CircleId",
                table: "CircleMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Lists_Circles_CircleId",
                table: "Lists");

            migrationBuilder.RenameColumn(
                name: "RecipientId",
                table: "ListShares",
                newName: "RecipientID");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "ListShares",
                newName: "OwnerID");

            migrationBuilder.RenameColumn(
                name: "ListShareId",
                table: "ListShares",
                newName: "ListShareID");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Lists",
                newName: "OwnerID");

            migrationBuilder.RenameColumn(
                name: "CircleId",
                table: "Lists",
                newName: "CircleID");

            migrationBuilder.RenameIndex(
                name: "IX_Lists_CircleId",
                table: "Lists",
                newName: "IX_Lists_CircleID");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Circles",
                newName: "OwnerID");

            migrationBuilder.RenameColumn(
                name: "CircleId",
                table: "Circles",
                newName: "CircleID");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "CircleMembers",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "CircleId",
                table: "CircleMembers",
                newName: "CircleID");

            migrationBuilder.RenameColumn(
                name: "CircleMemberId",
                table: "CircleMembers",
                newName: "CircleMemberID");

            migrationBuilder.RenameIndex(
                name: "IX_CircleMembers_CircleId_UserId",
                table: "CircleMembers",
                newName: "IX_CircleMembers_CircleID_UserID");

            migrationBuilder.RenameColumn(
                name: "RecipientId",
                table: "CircleInvitations",
                newName: "RecipientID");

            migrationBuilder.RenameColumn(
                name: "InviterId",
                table: "CircleInvitations",
                newName: "InviterID");

            migrationBuilder.RenameColumn(
                name: "CircleId",
                table: "CircleInvitations",
                newName: "CircleID");

            migrationBuilder.RenameColumn(
                name: "CircleInvitationId",
                table: "CircleInvitations",
                newName: "CircleInvitationID");

            migrationBuilder.RenameIndex(
                name: "IX_CircleInvitations_CircleId",
                table: "CircleInvitations",
                newName: "IX_CircleInvitations_CircleID");

            migrationBuilder.AlterColumn<int>(
                name: "ListID",
                table: "ListItems",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ListItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_CircleInvitations_Circles_CircleID",
                table: "CircleInvitations",
                column: "CircleID",
                principalTable: "Circles",
                principalColumn: "CircleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CircleMembers_Circles_CircleID",
                table: "CircleMembers",
                column: "CircleID",
                principalTable: "Circles",
                principalColumn: "CircleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lists_Circles_CircleID",
                table: "Lists",
                column: "CircleID",
                principalTable: "Circles",
                principalColumn: "CircleID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CircleInvitations_Circles_CircleID",
                table: "CircleInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_CircleMembers_Circles_CircleID",
                table: "CircleMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Lists_Circles_CircleID",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ListItems");

            migrationBuilder.RenameColumn(
                name: "RecipientID",
                table: "ListShares",
                newName: "RecipientId");

            migrationBuilder.RenameColumn(
                name: "OwnerID",
                table: "ListShares",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "ListShareID",
                table: "ListShares",
                newName: "ListShareId");

            migrationBuilder.RenameColumn(
                name: "OwnerID",
                table: "Lists",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "CircleID",
                table: "Lists",
                newName: "CircleId");

            migrationBuilder.RenameIndex(
                name: "IX_Lists_CircleID",
                table: "Lists",
                newName: "IX_Lists_CircleId");

            migrationBuilder.RenameColumn(
                name: "OwnerID",
                table: "Circles",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "CircleID",
                table: "Circles",
                newName: "CircleId");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "CircleMembers",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CircleID",
                table: "CircleMembers",
                newName: "CircleId");

            migrationBuilder.RenameColumn(
                name: "CircleMemberID",
                table: "CircleMembers",
                newName: "CircleMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_CircleMembers_CircleID_UserID",
                table: "CircleMembers",
                newName: "IX_CircleMembers_CircleId_UserId");

            migrationBuilder.RenameColumn(
                name: "RecipientID",
                table: "CircleInvitations",
                newName: "RecipientId");

            migrationBuilder.RenameColumn(
                name: "InviterID",
                table: "CircleInvitations",
                newName: "InviterId");

            migrationBuilder.RenameColumn(
                name: "CircleID",
                table: "CircleInvitations",
                newName: "CircleId");

            migrationBuilder.RenameColumn(
                name: "CircleInvitationID",
                table: "CircleInvitations",
                newName: "CircleInvitationId");

            migrationBuilder.RenameIndex(
                name: "IX_CircleInvitations_CircleID",
                table: "CircleInvitations",
                newName: "IX_CircleInvitations_CircleId");

            migrationBuilder.AlterColumn<int>(
                name: "ListID",
                table: "ListItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CircleInvitations_Circles_CircleId",
                table: "CircleInvitations",
                column: "CircleId",
                principalTable: "Circles",
                principalColumn: "CircleId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CircleMembers_Circles_CircleId",
                table: "CircleMembers",
                column: "CircleId",
                principalTable: "Circles",
                principalColumn: "CircleId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lists_Circles_CircleId",
                table: "Lists",
                column: "CircleId",
                principalTable: "Circles",
                principalColumn: "CircleId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

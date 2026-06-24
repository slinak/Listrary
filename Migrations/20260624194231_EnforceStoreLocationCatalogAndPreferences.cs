using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListManager.Migrations
{
    /// <inheritdoc />
    public partial class EnforceStoreLocationCatalogAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StrictStoreLocations",
                table: "Lists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StoreLocationOptionID",
                table: "ListItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreLocationOptions",
                columns: table => new
                {
                    StoreLocationOptionID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListID = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreLocationOptions", x => x.StoreLocationOptionID);
                    table.ForeignKey(
                        name: "FK_StoreLocationOptions_Lists_ListID",
                        column: x => x.ListID,
                        principalTable: "Lists",
                        principalColumn: "ListID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemLocationPreferences",
                columns: table => new
                {
                    ItemLocationPreferenceID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ListID = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemNameNormalized = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    StoreLocationOptionID = table.Column<int>(type: "INTEGER", nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemLocationPreferences", x => x.ItemLocationPreferenceID);
                    table.ForeignKey(
                        name: "FK_ItemLocationPreferences_Lists_ListID",
                        column: x => x.ListID,
                        principalTable: "Lists",
                        principalColumn: "ListID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemLocationPreferences_StoreLocationOptions_StoreLocationOptionID",
                        column: x => x.StoreLocationOptionID,
                        principalTable: "StoreLocationOptions",
                        principalColumn: "StoreLocationOptionID",
                        onDelete: ReferentialAction.Cascade);
                });

                        migrationBuilder.Sql(@"
                                INSERT INTO StoreLocationOptions (ListID, Name, NormalizedName, CreatedDate, LastUsedDate)
                                SELECT li.ListID,
                                             TRIM(li.StoreLocation) AS Name,
                                             LOWER(TRIM(li.StoreLocation)) AS NormalizedName,
                                             CURRENT_TIMESTAMP,
                                             CURRENT_TIMESTAMP
                                FROM ListItems li
                                WHERE li.ListID IS NOT NULL
                                    AND li.StoreLocation IS NOT NULL
                                    AND TRIM(li.StoreLocation) <> ''
                                GROUP BY li.ListID, LOWER(TRIM(li.StoreLocation));
                        ");

                        migrationBuilder.Sql(@"
                                UPDATE ListItems
                                     SET StoreLocationOptionID = (
                                             SELECT slo.StoreLocationOptionID
                                             FROM StoreLocationOptions slo
                                             WHERE slo.ListID = ListItems.ListID
                                                 AND slo.NormalizedName = LOWER(TRIM(ListItems.StoreLocation))
                                     )
                                 WHERE ListID IS NOT NULL
                                     AND StoreLocation IS NOT NULL
                                     AND TRIM(StoreLocation) <> '';
                        ");

                        migrationBuilder.Sql(@"
                                WITH RankedPreferences AS (
                                        SELECT li.ListID,
                                                     LOWER(TRIM(li.ItemName)) AS ItemNameNormalized,
                                                     li.StoreLocationOptionID,
                                                     COUNT(*) AS UsageCount,
                                                     MAX(li.LastUpdatedDate) AS LastUsedDate,
                                                     ROW_NUMBER() OVER (
                                                             PARTITION BY li.ListID, LOWER(TRIM(li.ItemName))
                                                             ORDER BY COUNT(*) DESC, MAX(li.LastUpdatedDate) DESC
                                                     ) AS PreferenceRank
                                        FROM ListItems li
                                        WHERE li.ListID IS NOT NULL
                                            AND li.StoreLocationOptionID IS NOT NULL
                                            AND li.ItemName IS NOT NULL
                                            AND TRIM(li.ItemName) <> ''
                                        GROUP BY li.ListID, LOWER(TRIM(li.ItemName)), li.StoreLocationOptionID
                                )
                                INSERT INTO ItemLocationPreferences (ListID, ItemNameNormalized, StoreLocationOptionID, UsageCount, LastUsedDate)
                                SELECT ListID, ItemNameNormalized, StoreLocationOptionID, UsageCount, LastUsedDate
                                FROM RankedPreferences
                                WHERE PreferenceRank = 1;
                        ");

            migrationBuilder.CreateIndex(
                name: "IX_ListItems_StoreLocationOptionID",
                table: "ListItems",
                column: "StoreLocationOptionID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocationPreferences_ListID_ItemNameNormalized",
                table: "ItemLocationPreferences",
                columns: new[] { "ListID", "ItemNameNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemLocationPreferences_StoreLocationOptionID",
                table: "ItemLocationPreferences",
                column: "StoreLocationOptionID");

            migrationBuilder.CreateIndex(
                name: "IX_StoreLocationOptions_ListID_NormalizedName",
                table: "StoreLocationOptions",
                columns: new[] { "ListID", "NormalizedName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ListItems_StoreLocationOptions_StoreLocationOptionID",
                table: "ListItems",
                column: "StoreLocationOptionID",
                principalTable: "StoreLocationOptions",
                principalColumn: "StoreLocationOptionID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListItems_StoreLocationOptions_StoreLocationOptionID",
                table: "ListItems");

            migrationBuilder.DropTable(
                name: "ItemLocationPreferences");

            migrationBuilder.DropTable(
                name: "StoreLocationOptions");

            migrationBuilder.DropIndex(
                name: "IX_ListItems_StoreLocationOptionID",
                table: "ListItems");

            migrationBuilder.DropColumn(
                name: "StrictStoreLocations",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "StoreLocationOptionID",
                table: "ListItems");
        }
    }
}

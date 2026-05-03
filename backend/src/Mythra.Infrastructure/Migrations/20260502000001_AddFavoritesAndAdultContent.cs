using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mythra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoritesAndAdultContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ShowAdultContent column to profiles
            migrationBuilder.AddColumn<bool>(
                name: "ShowAdultContent",
                table: "profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Create favorite_items table
            migrationBuilder.CreateTable(
                name: "favorite_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorite_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_favorite_items_ProfileId_MediaItemId",
                table: "favorite_items",
                columns: new[] { "ProfileId", "MediaItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_favorite_items_ProfileId",
                table: "favorite_items",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "favorite_items");

            migrationBuilder.DropColumn(
                name: "ShowAdultContent",
                table: "profiles");
        }
    }
}

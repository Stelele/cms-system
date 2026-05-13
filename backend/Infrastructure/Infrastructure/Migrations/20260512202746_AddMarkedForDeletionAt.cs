using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkedForDeletionAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MarkedForDeletionAt",
                table: "FileItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_MarkedForDeletionAt",
                table: "FileItems",
                column: "MarkedForDeletionAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileItems_MarkedForDeletionAt",
                table: "FileItems");

            migrationBuilder.DropColumn(
                name: "MarkedForDeletionAt",
                table: "FileItems");
        }
    }
}

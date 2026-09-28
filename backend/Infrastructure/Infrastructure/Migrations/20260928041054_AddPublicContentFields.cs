using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicContentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "Posts",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Blogs",
                type: "TEXT",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Blogs");
        }
    }
}

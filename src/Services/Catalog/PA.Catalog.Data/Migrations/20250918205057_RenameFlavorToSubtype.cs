using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PA.Catalog.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameFlavorToSubtype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Flavor",
                table: "product",
                newName: "Subtype");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Subtype",
                table: "product",
                newName: "Flavor");
        }
    }
}

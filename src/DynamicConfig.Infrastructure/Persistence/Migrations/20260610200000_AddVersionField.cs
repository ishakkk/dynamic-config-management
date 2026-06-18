using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DynamicConfig.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ConfigurationEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationEntries_ApplicationName_Version",
                table: "ConfigurationEntries",
                columns: new[] { "ApplicationName", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConfigurationEntries_ApplicationName_Version",
                table: "ConfigurationEntries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ConfigurationEntries");
        }
    }
}

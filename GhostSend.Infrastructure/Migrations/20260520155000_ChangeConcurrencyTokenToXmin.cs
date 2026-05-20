using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostSend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeConcurrencyTokenToXmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawVersion",
                table: "stored_files");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RawVersion",
                table: "stored_files",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}

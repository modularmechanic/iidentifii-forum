using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutstandingTokenIsUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_Outstanding",
                table: "UserTokens",
                columns: new[] { "UserId", "Purpose" },
                unique: true,
                filter: "\"ConsumedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTokens_Outstanding",
                table: "UserTokens");
        }
    }
}

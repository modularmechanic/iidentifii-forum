using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndexUserTokenSecretHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_SecretHash",
                table: "UserTokens",
                column: "SecretHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTokens_SecretHash",
                table: "UserTokens");
        }
    }
}

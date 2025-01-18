using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    /// <inheritdoc />
    public partial class Update_Third_Token_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppThirdUserTokenIssueInfo_Address_Symbol_OtherChainId",
                table: "AppThirdUserTokenIssueInfo");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "AppThirdUserTokenIssueInfo",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppThirdUserTokenIssueInfo_Symbol_OtherChainId",
                table: "AppThirdUserTokenIssueInfo",
                columns: new[] { "Symbol", "OtherChainId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppThirdUserTokenIssueInfo_Symbol_OtherChainId",
                table: "AppThirdUserTokenIssueInfo");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "AppThirdUserTokenIssueInfo",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppThirdUserTokenIssueInfo_Address_Symbol_OtherChainId",
                table: "AppThirdUserTokenIssueInfo",
                columns: new[] { "Address", "Symbol", "OtherChainId" },
                unique: true);
        }
    }
}

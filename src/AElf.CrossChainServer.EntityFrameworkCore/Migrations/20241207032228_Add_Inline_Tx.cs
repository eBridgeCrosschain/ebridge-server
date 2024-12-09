using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    /// <inheritdoc />
    public partial class Add_Inline_Tx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InlineTransferTransactionId",
                table: "AppCrossChainTransfers",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_InlineTransferT~",
                table: "AppCrossChainTransfers",
                columns: new[] { "FromChainId", "ToChainId", "InlineTransferTransactionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_InlineTransferT~",
                table: "AppCrossChainTransfers");

            migrationBuilder.DropColumn(
                name: "InlineTransferTransactionId",
                table: "AppCrossChainTransfers");
        }
    }
}

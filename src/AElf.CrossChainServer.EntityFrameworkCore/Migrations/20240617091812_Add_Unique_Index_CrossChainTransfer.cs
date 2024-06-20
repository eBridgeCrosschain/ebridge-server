using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    public partial class Add_Unique_Index_CrossChainTransfer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_TransferTransac~",
                table: "AppCrossChainTransfers");

            migrationBuilder.DropColumn(
                name: "TransferNeedToBeApproved",
                table: "AppCrossChainTransfers");

            migrationBuilder.CreateIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_TransferTransac~",
                table: "AppCrossChainTransfers",
                columns: new[] { "FromChainId", "ToChainId", "TransferTransactionId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_TransferTransac~",
                table: "AppCrossChainTransfers");

            migrationBuilder.AddColumn<bool>(
                name: "TransferNeedToBeApproved",
                table: "AppCrossChainTransfers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_TransferTransac~",
                table: "AppCrossChainTransfers",
                columns: new[] { "FromChainId", "ToChainId", "TransferTransactionId" });
        }
    }
}

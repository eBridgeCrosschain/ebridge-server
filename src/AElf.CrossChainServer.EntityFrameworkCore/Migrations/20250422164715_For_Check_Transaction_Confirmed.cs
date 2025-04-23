using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    /// <inheritdoc />
    public partial class For_Check_Transaction_Confirmed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransferNeedToBeApproved",
                table: "AppCrossChainTransfers");

            migrationBuilder.AddColumn<long>(
                name: "ReceiveBlockHeight",
                table: "AppCrossChainTransfers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ReceiveStatus",
                table: "AppCrossChainTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TransferStatus",
                table: "AppCrossChainTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiveBlockHeight",
                table: "AppCrossChainTransfers");

            migrationBuilder.DropColumn(
                name: "ReceiveStatus",
                table: "AppCrossChainTransfers");

            migrationBuilder.DropColumn(
                name: "TransferStatus",
                table: "AppCrossChainTransfers");

            migrationBuilder.AddColumn<bool>(
                name: "TransferNeedToBeApproved",
                table: "AppCrossChainTransfers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}

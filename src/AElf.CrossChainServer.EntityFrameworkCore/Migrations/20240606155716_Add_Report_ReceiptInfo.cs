using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    public partial class Add_Report_ReceiptInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransferNeedToBeApproved",
                table: "AppCrossChainTransfers");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptInfo",
                table: "AppReportInfos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptInfo",
                table: "AppReportInfos");

            migrationBuilder.AddColumn<bool>(
                name: "TransferNeedToBeApproved",
                table: "AppCrossChainTransfers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    public partial class Update_CrossChainTransfer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "AppReportInfos");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "AppOracleQueryInfos");

            migrationBuilder.AddColumn<long>(
                name: "LastUpdateHeight",
                table: "AppReportInfos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LastUpdateHeight",
                table: "AppOracleQueryInfos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<int>(
                name: "Progress",
                table: "AppCrossChainTransfers",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AddColumn<bool>(
                name: "TransferNeedToBeApproved",
                table: "AppCrossChainTransfers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdateHeight",
                table: "AppReportInfos");

            migrationBuilder.DropColumn(
                name: "LastUpdateHeight",
                table: "AppOracleQueryInfos");

            migrationBuilder.DropColumn(
                name: "TransferNeedToBeApproved",
                table: "AppCrossChainTransfers");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "AppReportInfos",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "AppOracleQueryInfos",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<double>(
                name: "Progress",
                table: "AppCrossChainTransfers",
                type: "double",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}

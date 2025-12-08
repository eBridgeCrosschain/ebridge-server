using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    public partial class Modify_ReportInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppReportInfos_ChainId_RoundId_Token",
                table: "AppReportInfos");

            migrationBuilder.AlterColumn<string>(
                name: "TargetChainId",
                table: "AppReportInfos",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "QueryTransactionId",
                table: "AppReportInfos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "AppReportInfos",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_AppReportInfos_ChainId_RoundId_Token_TargetChainId",
                table: "AppReportInfos",
                columns: new[] { "ChainId", "RoundId", "Token", "TargetChainId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppReportInfos_ChainId_RoundId_Token_TargetChainId",
                table: "AppReportInfos");

            migrationBuilder.DropColumn(
                name: "QueryTransactionId",
                table: "AppReportInfos");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "AppReportInfos");

            migrationBuilder.AlterColumn<string>(
                name: "TargetChainId",
                table: "AppReportInfos",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppReportInfos_ChainId_RoundId_Token",
                table: "AppReportInfos",
                columns: new[] { "ChainId", "RoundId", "Token" });
        }
    }
}

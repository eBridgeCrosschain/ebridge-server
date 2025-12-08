using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    public partial class Add_Index : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "AppReportInfos",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ChainId",
                table: "AppReportInfos",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "QueryId",
                table: "AppOracleQueryInfos",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ChainId",
                table: "AppOracleQueryInfos",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "TransferTransactionId",
                table: "AppCrossChainTransfers",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ToChainId",
                table: "AppCrossChainTransfers",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptId",
                table: "AppCrossChainTransfers",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "FromChainId",
                table: "AppCrossChainTransfers",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppReportInfos_ChainId_RoundId_Token",
                table: "AppReportInfos",
                columns: new[] { "ChainId", "RoundId", "Token" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOracleQueryInfos_ChainId_QueryId",
                table: "AppOracleQueryInfos",
                columns: new[] { "ChainId", "QueryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_ReceiptId",
                table: "AppCrossChainTransfers",
                columns: new[] { "FromChainId", "ToChainId", "ReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_TransferTransac~",
                table: "AppCrossChainTransfers",
                columns: new[] { "FromChainId", "ToChainId", "TransferTransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCrossChainTransfers_Status_ProgressUpdateTime",
                table: "AppCrossChainTransfers",
                columns: new[] { "Status", "ProgressUpdateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCrossChainIndexingInfos_BlockTime",
                table: "AppCrossChainIndexingInfos",
                column: "BlockTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppReportInfos_ChainId_RoundId_Token",
                table: "AppReportInfos");

            migrationBuilder.DropIndex(
                name: "IX_AppOracleQueryInfos_ChainId_QueryId",
                table: "AppOracleQueryInfos");

            migrationBuilder.DropIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_ReceiptId",
                table: "AppCrossChainTransfers");

            migrationBuilder.DropIndex(
                name: "IX_AppCrossChainTransfers_FromChainId_ToChainId_TransferTransac~",
                table: "AppCrossChainTransfers");

            migrationBuilder.DropIndex(
                name: "IX_AppCrossChainTransfers_Status_ProgressUpdateTime",
                table: "AppCrossChainTransfers");

            migrationBuilder.DropIndex(
                name: "IX_AppCrossChainIndexingInfos_BlockTime",
                table: "AppCrossChainIndexingInfos");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "AppReportInfos",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ChainId",
                table: "AppReportInfos",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "QueryId",
                table: "AppOracleQueryInfos",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ChainId",
                table: "AppOracleQueryInfos",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "TransferTransactionId",
                table: "AppCrossChainTransfers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ToChainId",
                table: "AppCrossChainTransfers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptId",
                table: "AppCrossChainTransfers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "FromChainId",
                table: "AppCrossChainTransfers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

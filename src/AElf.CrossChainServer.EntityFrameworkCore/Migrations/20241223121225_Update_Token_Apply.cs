using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    /// <inheritdoc />
    public partial class Update_Token_Apply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppApplyOrderChainTokenInfo_AppTokenApplyOrder_Id",
                table: "AppApplyOrderChainTokenInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_AppApplyOrderStatusChangedRecord_AppTokenApplyOrder_Id",
                table: "AppApplyOrderStatusChangedRecord");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "AppApplyOrderStatusChangedRecord",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "AppApplyOrderChainTokenInfo",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AppApplyOrderStatusChangedRecord_OrderId",
                table: "AppApplyOrderStatusChangedRecord",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppApplyOrderChainTokenInfo_OrderId",
                table: "AppApplyOrderChainTokenInfo",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppApplyOrderChainTokenInfo_AppTokenApplyOrder_OrderId",
                table: "AppApplyOrderChainTokenInfo",
                column: "OrderId",
                principalTable: "AppTokenApplyOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppApplyOrderStatusChangedRecord_AppTokenApplyOrder_OrderId",
                table: "AppApplyOrderStatusChangedRecord",
                column: "OrderId",
                principalTable: "AppTokenApplyOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppApplyOrderChainTokenInfo_AppTokenApplyOrder_OrderId",
                table: "AppApplyOrderChainTokenInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_AppApplyOrderStatusChangedRecord_AppTokenApplyOrder_OrderId",
                table: "AppApplyOrderStatusChangedRecord");

            migrationBuilder.DropIndex(
                name: "IX_AppApplyOrderStatusChangedRecord_OrderId",
                table: "AppApplyOrderStatusChangedRecord");

            migrationBuilder.DropIndex(
                name: "IX_AppApplyOrderChainTokenInfo_OrderId",
                table: "AppApplyOrderChainTokenInfo");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "AppApplyOrderStatusChangedRecord");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "AppApplyOrderChainTokenInfo");

            migrationBuilder.AddForeignKey(
                name: "FK_AppApplyOrderChainTokenInfo_AppTokenApplyOrder_Id",
                table: "AppApplyOrderChainTokenInfo",
                column: "Id",
                principalTable: "AppTokenApplyOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppApplyOrderStatusChangedRecord_AppTokenApplyOrder_Id",
                table: "AppApplyOrderStatusChangedRecord",
                column: "Id",
                principalTable: "AppTokenApplyOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

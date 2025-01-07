using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    /// <inheritdoc />
    public partial class Update_Token_Order : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppApplyOrderChainTokenInfo");

            migrationBuilder.AddColumn<bool>(
                name: "IsBurnable",
                table: "AppTokens",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ChainId",
                table: "AppTokenApplyOrder",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChainName",
                table: "AppTokenApplyOrder",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ContractAddress",
                table: "AppTokenApplyOrder",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Decimals",
                table: "AppTokenApplyOrder",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "AppTokenApplyOrder",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PoolAddress",
                table: "AppTokenApplyOrder",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TokenName",
                table: "AppTokenApplyOrder",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSupply",
                table: "AppTokenApplyOrder",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AddressInfoDto",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBurnable",
                table: "AppTokens");

            migrationBuilder.DropColumn(
                name: "ChainId",
                table: "AppTokenApplyOrder");

            migrationBuilder.DropColumn(
                name: "ChainName",
                table: "AppTokenApplyOrder");

            migrationBuilder.DropColumn(
                name: "ContractAddress",
                table: "AppTokenApplyOrder");

            migrationBuilder.DropColumn(
                name: "Decimals",
                table: "AppTokenApplyOrder");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "AppTokenApplyOrder");

            migrationBuilder.DropColumn(
                name: "PoolAddress",
                table: "AppTokenApplyOrder");

            migrationBuilder.DropColumn(
                name: "TokenName",
                table: "AppTokenApplyOrder");

            migrationBuilder.DropColumn(
                name: "TotalSupply",
                table: "AppTokenApplyOrder");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "AddressInfoDto",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "AppApplyOrderChainTokenInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChainId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChainName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContractAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Decimals = table.Column<int>(type: "int", nullable: false),
                    Icon = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PoolAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalSupply = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppApplyOrderChainTokenInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppApplyOrderChainTokenInfo_AppTokenApplyOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "AppTokenApplyOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppApplyOrderChainTokenInfo_OrderId",
                table: "AppApplyOrderChainTokenInfo",
                column: "OrderId");
        }
    }
}

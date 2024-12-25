using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    /// <inheritdoc />
    public partial class Update_Token_Access : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenOwnerDto");

            migrationBuilder.DropTable(
                name: "UserTokenIssues");

            migrationBuilder.DropTable(
                name: "AppUserTokenOwners");

            migrationBuilder.DropIndex(
                name: "IX_AppUserTokenAccessInfo_Address",
                table: "AppUserTokenAccessInfo");

            migrationBuilder.DropIndex(
                name: "IX_AppUserTokenAccessInfo_Symbol",
                table: "AppUserTokenAccessInfo");

            migrationBuilder.DropColumn(
                name: "ChainIds",
                table: "AppUserTokenAccessInfo");

            migrationBuilder.DropColumn(
                name: "OtherChainIds",
                table: "AppUserTokenAccessInfo");

            migrationBuilder.CreateTable(
                name: "AppThirdUserTokenIssueInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Address = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WalletAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChainId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<long>(type: "bigint", nullable: false),
                    UpdateTime = table.Column<long>(type: "bigint", nullable: false),
                    TokenName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenImage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OtherChainId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalSupply = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContractAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BindingId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThirdTokenId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppThirdUserTokenIssueInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserTokenAccessInfo_Address_Symbol",
                table: "AppUserTokenAccessInfo",
                columns: new[] { "Address", "Symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppThirdUserTokenIssueInfo_Address_Symbol_OtherChainId",
                table: "AppThirdUserTokenIssueInfo",
                columns: new[] { "Address", "Symbol", "OtherChainId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppThirdUserTokenIssueInfo");

            migrationBuilder.DropIndex(
                name: "IX_AppUserTokenAccessInfo_Address_Symbol",
                table: "AppUserTokenAccessInfo");

            migrationBuilder.AddColumn<string>(
                name: "ChainIds",
                table: "AppUserTokenAccessInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OtherChainIds",
                table: "AppUserTokenAccessInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AppUserTokenOwners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Address = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserTokenOwners", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserTokenIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Address = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BindingId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChainId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContractAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<long>(type: "bigint", nullable: false),
                    OtherChainId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThirdTokenId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenImage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalSupply = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdateTime = table.Column<long>(type: "bigint", nullable: false),
                    WalletAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokenIssues", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TokenOwnerDto",
                columns: table => new
                {
                    UserTokenOwnerDtoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChainIds = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContractAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Decimals = table.Column<int>(type: "int", nullable: false),
                    Holders = table.Column<int>(type: "int", nullable: false),
                    Icon = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LiquidityInUsd = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Owner = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PoolAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalSupply = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenOwnerDto", x => new { x.UserTokenOwnerDtoId, x.Id });
                    table.ForeignKey(
                        name: "FK_TokenOwnerDto_AppUserTokenOwners_UserTokenOwnerDtoId",
                        column: x => x.UserTokenOwnerDtoId,
                        principalTable: "AppUserTokenOwners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserTokenAccessInfo_Address",
                table: "AppUserTokenAccessInfo",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserTokenAccessInfo_Symbol",
                table: "AppUserTokenAccessInfo",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserTokenOwners_Address",
                table: "AppUserTokenOwners",
                column: "Address");
        }
    }
}

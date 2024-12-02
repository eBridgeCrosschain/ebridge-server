using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AElf.CrossChainServer.Migrations
{
    /// <inheritdoc />
    public partial class Add_Ton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "AppCrossChainTransfers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AppCrossChainDailyLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChainId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetChainId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RemainAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    RefreshTime = table.Column<long>(type: "bigint", nullable: false),
                    DailyLimit = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCrossChainDailyLimits", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AppCrossChainRateLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TokenId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChainId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetChainId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Capacity = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsEnable = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCrossChainRateLimits", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCrossChainDailyLimits");

            migrationBuilder.DropTable(
                name: "AppCrossChainRateLimits");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "AppCrossChainTransfers");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerWallWeb.Migrations
{
    /// <inheritdoc />
    public partial class addGroupByTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GroupBuyId",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderType",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GroupBuyInstances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RuleId = table.Column<long>(type: "bigint", nullable: false),
                    InitiatorId = table.Column<long>(type: "bigint", nullable: false),
                    GroupNo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentCount = table.Column<int>(type: "int", nullable: false),
                    TargetCount = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpireTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBuyInstances", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GroupBuyRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    RequiredPeople = table.Column<int>(type: "int", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DurationHours = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBuyRules", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 59, DateTimeKind.Local).AddTicks(7918));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 59, DateTimeKind.Local).AddTicks(8260));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 59, DateTimeKind.Local).AddTicks(8261));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 59, DateTimeKind.Local).AddTicks(8262));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 60, DateTimeKind.Local).AddTicks(3186));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 60, DateTimeKind.Local).AddTicks(3806));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 60, DateTimeKind.Local).AddTicks(3809));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 60, DateTimeKind.Local).AddTicks(2262));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 11, 6, 14, 60, DateTimeKind.Local).AddTicks(2942));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupBuyInstances");

            migrationBuilder.DropTable(
                name: "GroupBuyRules");

            migrationBuilder.DropColumn(
                name: "GroupBuyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 279, DateTimeKind.Local).AddTicks(6413));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 279, DateTimeKind.Local).AddTicks(6866));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 279, DateTimeKind.Local).AddTicks(6868));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 279, DateTimeKind.Local).AddTicks(6868));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 280, DateTimeKind.Local).AddTicks(1621));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 280, DateTimeKind.Local).AddTicks(2096));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 280, DateTimeKind.Local).AddTicks(2098));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 280, DateTimeKind.Local).AddTicks(623));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 48, 44, 280, DateTimeKind.Local).AddTicks(1329));
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerWallWeb.Migrations
{
    /// <inheritdoc />
    public partial class updateOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CouponAmount",
                table: "Orders",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 889, DateTimeKind.Local).AddTicks(6662));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 889, DateTimeKind.Local).AddTicks(7180));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 889, DateTimeKind.Local).AddTicks(7181));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 889, DateTimeKind.Local).AddTicks(7182));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 890, DateTimeKind.Local).AddTicks(1925));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 890, DateTimeKind.Local).AddTicks(2399));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 890, DateTimeKind.Local).AddTicks(2401));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 890, DateTimeKind.Local).AddTicks(915));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 39, 29, 890, DateTimeKind.Local).AddTicks(1650));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CouponAmount",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 9, DateTimeKind.Local).AddTicks(7040));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 9, DateTimeKind.Local).AddTicks(7401));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 9, DateTimeKind.Local).AddTicks(7402));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 9, DateTimeKind.Local).AddTicks(7403));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 10, DateTimeKind.Local).AddTicks(2257));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 10, DateTimeKind.Local).AddTicks(2723));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 10, DateTimeKind.Local).AddTicks(2724));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 10, DateTimeKind.Local).AddTicks(1285));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 10, 22, 26, 36, 10, DateTimeKind.Local).AddTicks(1962));
        }
    }
}

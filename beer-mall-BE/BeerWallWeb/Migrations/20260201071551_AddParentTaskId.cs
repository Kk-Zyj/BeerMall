using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerWallWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddParentTaskId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentTaskId",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 621, DateTimeKind.Local).AddTicks(9182));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 621, DateTimeKind.Local).AddTicks(9643));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 621, DateTimeKind.Local).AddTicks(9645));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 621, DateTimeKind.Local).AddTicks(9645));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 622, DateTimeKind.Local).AddTicks(4496));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 622, DateTimeKind.Local).AddTicks(4976));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 622, DateTimeKind.Local).AddTicks(4978));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 622, DateTimeKind.Local).AddTicks(3444));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 2, 1, 15, 15, 51, 622, DateTimeKind.Local).AddTicks(4126));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "Orders");

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
    }
}

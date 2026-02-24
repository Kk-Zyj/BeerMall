using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerWallWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RiskReason",
                table: "Orders",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ClientIp",
                table: "Orders",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "RiskReason",
                keyValue: null,
                column: "RiskReason",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "RiskReason",
                table: "Orders",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "ClientIp",
                keyValue: null,
                column: "ClientIp",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "ClientIp",
                table: "Orders",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 975, DateTimeKind.Local).AddTicks(9003));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 975, DateTimeKind.Local).AddTicks(9430));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 975, DateTimeKind.Local).AddTicks(9431));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 975, DateTimeKind.Local).AddTicks(9432));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 976, DateTimeKind.Local).AddTicks(4174));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 976, DateTimeKind.Local).AddTicks(4639));

            migrationBuilder.UpdateData(
                table: "ProductSkus",
                keyColumn: "Id",
                keyValue: 3L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 976, DateTimeKind.Local).AddTicks(4641));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 976, DateTimeKind.Local).AddTicks(3183));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2L,
                column: "CreateTime",
                value: new DateTime(2026, 1, 31, 20, 1, 40, 976, DateTimeKind.Local).AddTicks(3896));
        }
    }
}

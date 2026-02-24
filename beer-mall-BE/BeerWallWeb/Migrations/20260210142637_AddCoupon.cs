using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerWallWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCoupon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MinPoint = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    ReceivedCount = table.Column<int>(type: "int", nullable: false),
                    TimeType = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Days = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserCoupons",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CouponId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UsedTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    ExpireTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCoupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCoupons_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.CreateIndex(
                name: "IX_UserCoupons_CouponId",
                table: "UserCoupons",
                column: "CouponId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCoupons");

            migrationBuilder.DropTable(
                name: "Coupons");

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
    }
}

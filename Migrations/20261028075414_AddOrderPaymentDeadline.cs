using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaymentDueAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql("""UPDATE "Orders" SET "PaymentDueAt" = "CreatedAt" + interval '30 minutes';""");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_PaymentDueAt",
                table: "Orders",
                columns: new[] { "Status", "PaymentDueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_PaymentDueAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentDueAt",
                table: "Orders");
        }
    }
}

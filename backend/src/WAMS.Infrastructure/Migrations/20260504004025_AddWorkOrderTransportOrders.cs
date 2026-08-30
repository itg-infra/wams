using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderTransportOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_order_transport_orders",
                columns: table => new
                {
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    budget_plan_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_transport_orders", x => new { x.work_order_id, x.budget_plan_id });
                    table.ForeignKey(
                        name: "FK_work_order_transport_orders_budget_plans_budget_plan_id",
                        column: x => x.budget_plan_id,
                        principalTable: "budget_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_transport_orders_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_order_transport_orders_budget_plan_id",
                table: "work_order_transport_orders",
                column: "budget_plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_order_transport_orders");
        }
    }
}

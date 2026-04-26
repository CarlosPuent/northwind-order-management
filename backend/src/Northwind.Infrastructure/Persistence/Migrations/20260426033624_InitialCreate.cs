using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Northwind.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Initial migration. Creates only the new ShippingGeocodes table.
    ///
    /// The legacy Northwind tables (Customers, Employees, Orders, Order Details,
    /// Products, Shippers) are intentionally NOT included in this migration:
    /// they already exist in the production database and are owned by upstream
    /// consumers we don't control. The Up() and Down() methods touch only
    /// schema objects this project owns.
    /// </summary>
    /// <remarks>
    /// EF Core's scaffolder generated CREATE TABLE statements for every entity
    /// in the model. We removed the legacy ones manually because the scaffolder
    /// can't distinguish "tables I own" from "tables I only map for read access".
    /// This is the standard practice when bolting EF Core onto a brownfield database.
    /// </remarks>
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingGeocodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    PlaceType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    RawResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingGeocodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingGeocodes_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingGeocodes_OrderId",
                table: "ShippingGeocodes",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingGeocodes");
        }
    }
}
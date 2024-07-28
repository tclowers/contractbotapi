using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractBotApi.Migrations
{
    /// <inheritdoc />
    public partial class AddContractSubtypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForwardContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    FutureDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettlementTerms = table.Column<string>(type: "text", nullable: true),
                    ForwardPrice = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForwardContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForwardContracts_Contracts_Id",
                        column: x => x.Id,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptionContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StrikePrice = table.Column<string>(type: "text", nullable: true),
                    OptionType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionContracts_Contracts_Id",
                        column: x => x.Id,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpotContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    ImmediateDelivery = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotContracts_Contracts_Id",
                        column: x => x.Id,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SwapContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UnderlyingAsset = table.Column<string>(type: "text", nullable: true),
                    NotionalAmount = table.Column<string>(type: "text", nullable: true),
                    PaymentFrequency = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwapContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwapContracts_Contracts_Id",
                        column: x => x.Id,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForwardContracts");

            migrationBuilder.DropTable(
                name: "OptionContracts");

            migrationBuilder.DropTable(
                name: "SpotContracts");

            migrationBuilder.DropTable(
                name: "SwapContracts");
        }
    }
}

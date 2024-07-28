using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractBotApi.Migrations
{
    /// <inheritdoc />
    public partial class AddContractInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForwardContracts");

            migrationBuilder.DropTable(
                name: "OptionContracts");

            migrationBuilder.DropTable(
                name: "SpotContracts");

            migrationBuilder.DropTable(
                name: "SwapContracts");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Contracts",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Contracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForwardPrice",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FutureDeliveryDate",
                table: "Contracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ImmediateDelivery",
                table: "Contracts",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotionalAmount",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionType",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentFrequency",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettlementDate",
                table: "Contracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementTerms",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrikePrice",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnderlyingAsset",
                table: "Contracts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ForwardPrice",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "FutureDeliveryDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ImmediateDelivery",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "NotionalAmount",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "OptionType",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PaymentFrequency",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SettlementDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SettlementTerms",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "StrikePrice",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "UnderlyingAsset",
                table: "Contracts");

            migrationBuilder.CreateTable(
                name: "ForwardContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ForwardPrice = table.Column<string>(type: "text", nullable: true),
                    FutureDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettlementTerms = table.Column<string>(type: "text", nullable: true)
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
                    OptionType = table.Column<string>(type: "text", nullable: true),
                    StrikePrice = table.Column<string>(type: "text", nullable: true)
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
                    ImmediateDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    SettlementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    NotionalAmount = table.Column<string>(type: "text", nullable: true),
                    PaymentFrequency = table.Column<string>(type: "text", nullable: true),
                    UnderlyingAsset = table.Column<string>(type: "text", nullable: true)
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
    }
}

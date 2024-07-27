using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractBotApi.Migrations
{
    /// <inheritdoc />
    public partial class AddContractTextField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContractText",
                table: "Contracts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractText",
                table: "Contracts");
        }
    }
}

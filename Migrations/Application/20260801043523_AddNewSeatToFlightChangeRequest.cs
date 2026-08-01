using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U3_Examen_Airport.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNewSeatToFlightChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "new_seat",
                table: "flight_change_requests",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "new_seat",
                table: "flight_change_requests");
        }
    }
}

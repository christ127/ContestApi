using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContestApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDogFieldsAndDogPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DogName",
                table: "Submission",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DogStory",
                table: "Submission",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoPurchase",
                table: "Submission",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DogPhotoBlobName",
                table: "Submission",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DogName",         table: "Submission");
            migrationBuilder.DropColumn(name: "DogStory",        table: "Submission");
            migrationBuilder.DropColumn(name: "NoPurchase",      table: "Submission");
            migrationBuilder.DropColumn(name: "DogPhotoBlobName", table: "Submission");
        }
    }
}

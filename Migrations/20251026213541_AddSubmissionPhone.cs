using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContestApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Contests_ContestId",
                table: "Submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Submissions",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_ContestId",
                table: "Submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Contests",
                table: "Contests");

            migrationBuilder.RenameTable(
                name: "Submissions",
                newName: "Submission");

            migrationBuilder.RenameTable(
                name: "Contests",
                newName: "Contest");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Submission",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BlobName",
                table: "Submission",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Submission",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Submission",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "Submission",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Submission",
                table: "Submission",
                column: "SubmissionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contest",
                table: "Contest",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_Submission_ContestId_Email",
                table: "Submission",
                columns: new[] { "ContestId", "Email" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Submission_Contest_ContestId",
                table: "Submission",
                column: "ContestId",
                principalTable: "Contest",
                principalColumn: "ContestId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submission_Contest_ContestId",
                table: "Submission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Submission",
                table: "Submission");

            migrationBuilder.DropIndex(
                name: "IX_Submission_ContestId_Email",
                table: "Submission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Contest",
                table: "Contest");

            migrationBuilder.DropColumn(
                name: "BlobName",
                table: "Submission");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Submission");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Submission");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "Submission");

            migrationBuilder.RenameTable(
                name: "Submission",
                newName: "Submissions");

            migrationBuilder.RenameTable(
                name: "Contest",
                newName: "Contests");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Submissions",
                table: "Submissions",
                column: "SubmissionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contests",
                table: "Contests",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ContestId",
                table: "Submissions",
                column: "ContestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Contests_ContestId",
                table: "Submissions",
                column: "ContestId",
                principalTable: "Contests",
                principalColumn: "ContestId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

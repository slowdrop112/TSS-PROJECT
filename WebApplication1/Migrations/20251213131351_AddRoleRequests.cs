using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uniflow.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_Courses_AspNetUsers_ProfesorId",
            //     table: "Courses");

            // migrationBuilder.DropIndex(
            //     name: "IX_Courses_ProfesorId",
            //     table: "Courses");

            // migrationBuilder.DropColumn(
            //     name: "ProfesorId",
            //     table: "Courses");

            // migrationBuilder.RenameColumn(
            //     name: "CreatedDate",
            //     table: "Courses",
            //     newName: "DateCreated");

            // migrationBuilder.RenameColumn(
            //     name: "Id",
            //     table: "Courses",
            //     newName: "CourseID");

            // migrationBuilder.AlterColumn<string>(
            //     name: "Title",
            //     table: "Courses",
            //     type: "nvarchar(255)",
            //     maxLength: 255,
            //     nullable: false,
            //     oldClrType: typeof(string),
            //     oldType: "nvarchar(200)",
            //     oldMaxLength: 200);

            // migrationBuilder.AlterColumn<string>(
            //     name: "Description",
            //     table: "Courses",
            //     type: "nvarchar(max)",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "nvarchar(1000)",
            //     oldMaxLength: 1000,
            //     oldNullable: true);

            // migrationBuilder.AddColumn<string>(
            //     name: "Category",
            //     table: "Courses",
            //     type: "nvarchar(50)",
            //     maxLength: 50,
            //     nullable: true);

            // migrationBuilder.AddColumn<int>(
            //     name: "DurationHours",
            //     table: "Courses",
            //     type: "int",
            //     nullable: true);

            // migrationBuilder.AddColumn<bool>(
            //     name: "IsPublished",
            //     table: "Courses",
            //     type: "bit",
            //     nullable: false,
            //     defaultValue: true);

            migrationBuilder.CreateTable(
                name: "RoleRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestedRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleRequests_AspNetUsers_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleRequests_ProcessedByUserId",
                table: "RoleRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleRequests_UserId",
                table: "RoleRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleRequests");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "DateCreated",
                table: "Courses",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "CourseID",
                table: "Courses",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Courses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Courses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfesorId",
                table: "Courses",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ProfesorId",
                table: "Courses",
                column: "ProfesorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_ProfesorId",
                table: "Courses",
                column: "ProfesorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

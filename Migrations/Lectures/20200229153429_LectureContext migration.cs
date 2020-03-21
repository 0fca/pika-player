using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Claudia.Migrations.Lectures
{
    public partial class LectureContextmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    comment_id = table.Column<string>(nullable: false),
                    video_id = table.Column<string>(nullable: false),
                    user_id = table.Column<string>(nullable: false),
                    content = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.comment_id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    course_name = table.Column<string>(nullable: true),
                    lecturer_id = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Expiries",
                columns: table => new
                {
                    id = table.Column<string>(nullable: false),
                    expiry_date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expiries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Lectures",
                columns: table => new
                {
                    id = table.Column<string>(nullable: false),
                    display_name = table.Column<string>(nullable: false),
                    description = table.Column<string>(nullable: false),
                    video_path = table.Column<string>(nullable: false),
                    thumbnail = table.Column<byte[]>(nullable: true),
                    mime_type = table.Column<string>(nullable: true),
                    lecturer_id = table.Column<string>(nullable: false),
                    course_id = table.Column<int>(nullable: false),
                    date_added = table.Column<DateTime>(nullable: false),
                    is_locked = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lectures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SubComments",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<string>(nullable: false),
                    subcontent = table.Column<string>(nullable: false),
                    comment_id = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubComments", x => x.id);
                    table.ForeignKey(
                        name: "FK_SubComments_Comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "Comments",
                        principalColumn: "comment_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubComments_comment_id",
                table: "SubComments",
                column: "comment_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Expiries");

            migrationBuilder.DropTable(
                name: "Lectures");

            migrationBuilder.DropTable(
                name: "SubComments");

            migrationBuilder.DropTable(
                name: "Comments");
        }
    }
}

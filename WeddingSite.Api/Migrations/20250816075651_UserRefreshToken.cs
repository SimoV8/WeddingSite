using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WeddingSite.Api.Migrations
{
    /// <inheritdoc />
    public partial class UserRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeddingParticipation_AspNetUsers_UserId",
                table: "WeddingParticipation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeddingParticipation",
                table: "WeddingParticipation");

            migrationBuilder.RenameTable(
                name: "WeddingParticipation",
                newName: "WeddingParticipations");

            migrationBuilder.RenameIndex(
                name: "IX_WeddingParticipation_UserId",
                table: "WeddingParticipations",
                newName: "IX_WeddingParticipations_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeddingParticipations",
                table: "WeddingParticipations",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefreshToken = table.Column<string>(type: "VARCHAR", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId",
                table: "UserRefreshTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WeddingParticipations_AspNetUsers_UserId",
                table: "WeddingParticipations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeddingParticipations_AspNetUsers_UserId",
                table: "WeddingParticipations");

            migrationBuilder.DropTable(
                name: "UserRefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeddingParticipations",
                table: "WeddingParticipations");

            migrationBuilder.RenameTable(
                name: "WeddingParticipations",
                newName: "WeddingParticipation");

            migrationBuilder.RenameIndex(
                name: "IX_WeddingParticipations_UserId",
                table: "WeddingParticipation",
                newName: "IX_WeddingParticipation_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeddingParticipation",
                table: "WeddingParticipation",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WeddingParticipation_AspNetUsers_UserId",
                table: "WeddingParticipation",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

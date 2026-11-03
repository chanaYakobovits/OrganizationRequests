using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = 'IX_Requests_AssignedTo_CreatedAt'
              AND object_id = OBJECT_ID('dbo.Requests')
        )
        CREATE NONCLUSTERED INDEX IX_Requests_AssignedTo_CreatedAt
        ON dbo.Requests (AssignedTo ASC, CreatedAt DESC);
    ");

            migrationBuilder.Sql(@"
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = 'IX_Requests_CreatedAt_Id'
              AND object_id = OBJECT_ID('dbo.Requests')
        )
        CREATE NONCLUSTERED INDEX IX_Requests_CreatedAt_Id
        ON dbo.Requests (CreatedAt DESC, Id ASC);
    ");

            migrationBuilder.Sql(@"
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = 'IX_Requests_Status_CreatedAt'
              AND object_id = OBJECT_ID('dbo.Requests')
        )
        CREATE NONCLUSTERED INDEX IX_Requests_Status_CreatedAt
        ON dbo.Requests (Status ASC, CreatedAt DESC);
    ");
        }
     

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_AssignedTo_CreatedAt",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CreatedAt_Id",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_Status_CreatedAt",
                table: "Requests");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF NOT EXISTS (SELECT 1 FROM sys.indexes
                       WHERE name = 'IX_Requests_Title' AND object_id = OBJECT_ID('dbo.Requests'))
            CREATE NONCLUSTERED INDEX IX_Requests_Title ON dbo.Requests (Title);");

            migrationBuilder.Sql(@"
        IF NOT EXISTS (SELECT 1 FROM sys.indexes
                       WHERE name = 'IX_Requests_OrganizationName' AND object_id = OBJECT_ID('dbo.Requests'))
            CREATE NONCLUSTERED INDEX IX_Requests_OrganizationName ON dbo.Requests (OrganizationName);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Requests_Title ON dbo.Requests;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Requests_OrganizationName ON dbo.Requests;");

        }
    }
}

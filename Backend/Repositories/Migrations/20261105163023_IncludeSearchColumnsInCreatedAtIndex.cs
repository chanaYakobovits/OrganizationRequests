using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class IncludeSearchColumnsInCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.name = 'IX_Requests_CreatedAt_Id'
              AND i.object_id = OBJECT_ID('dbo.Requests')
              AND c.name = 'Title'
              AND ic.is_included_column = 1
        )
            CREATE NONCLUSTERED INDEX IX_Requests_CreatedAt_Id
            ON dbo.Requests (CreatedAt DESC, Id ASC)
            INCLUDE (Title, OrganizationName)
            WITH (DROP_EXISTING = ON);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        CREATE NONCLUSTERED INDEX IX_Requests_CreatedAt_Id
        ON dbo.Requests (CreatedAt DESC, Id ASC)
        WITH (DROP_EXISTING = ON);");
        }
    }
}

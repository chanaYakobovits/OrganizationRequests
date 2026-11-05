using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF NOT EXISTS (SELECT 1 FROM sys.indexes
                       WHERE name = 'IX_Requests_Priority' AND object_id = OBJECT_ID('dbo.Requests'))
            CREATE NONCLUSTERED INDEX IX_Requests_Priority ON dbo.Requests (Priority);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Requests_Priority ON dbo.Requests;");
        }
    }
}


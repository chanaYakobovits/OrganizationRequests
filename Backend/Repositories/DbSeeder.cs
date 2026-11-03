using System.Data;
using Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Repositories;

public class DbSeeder
{
    private readonly AppDbContext _db;
    public DbSeeder(AppDbContext db) => _db = db;

    private static readonly string[] OrgPrefixes =
        { "אלפא", "בטא", "גמא", "דלתא", "אורן", "נגב", "גליל", "שחר", "מרום", "כרמל" };
    private static readonly string[] OrgTypes =
        { "הנדסה", "שירותים", "סיעוד", "בנייה", "חינוך", "תעשיות", "לוגיסטיקה", "טכנולוגיות" };
    private static readonly string[] Subjects =
        { "בקשה לאישור", "עדכון פרטים", "בירור סטטוס", "הארכת תוקף", "תלונה על עיכוב", "בקשה להנפקת מסמך" };
    private static readonly string[] Assignees =
        Enumerable.Range(1, 40).Select(i => $"מטפל {i:00}").ToArray();

    public async Task<int> SeedAsync(int count = 100_000, bool reset = false, CancellationToken ct = default)
    {
        if (reset)
        {
            await _db.RequestStatusHistories.ExecuteDeleteAsync(ct);
            await _db.Requests.ExecuteDeleteAsync(ct);
            await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Requests', RESEED, 0)", ct);
        }
        else if (await _db.Requests.AnyAsync(ct))
        {
            return 0; // כבר קיימים נתונים
        }

        var table = BuildTable(count);

        var conn = (SqlConnection)_db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = "Requests",
            BatchSize = 10_000,
            BulkCopyTimeout = 120
        };
        foreach (DataColumn c in table.Columns)
            bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);

        await bulk.WriteToServerAsync(table, ct);
        return count;
    }

    private static DataTable BuildTable(int count)
    {
        var rnd = new Random(42);
        var now = DateTime.UtcNow;

        var table = new DataTable();
        table.Columns.Add("Title", typeof(string));
        table.Columns.Add("OrganizationName", typeof(string));
        table.Columns.Add("Status", typeof(byte));
        table.Columns.Add("Priority", typeof(byte));
        table.Columns.Add("AssignedTo", typeof(string));
        table.Columns.Add("CreatedAt", typeof(DateTime));
        table.Columns.Add("UpdatedAt", typeof(DateTime));

        for (int i = 0; i < count; i++)
        {
            var status = PickStatus(rnd);
            var priority = PickPriority(rnd);

            // פניות חדשות בדרך כלל עדיין לא משובצות
            object assignedTo = (status == RequestStatus.New && rnd.NextDouble() < 0.8)
                ? DBNull.Value
                : Assignees[rnd.Next(Assignees.Length)];

            var created = now.AddMinutes(-rnd.Next(0, 60 * 24 * 730)); // עד שנתיים אחורה
            var updated = created.AddMinutes(rnd.Next(0, (int)Math.Min(60 * 24 * 60, (now - created).TotalMinutes) + 1));

            var org = $"{OrgPrefixes[rnd.Next(OrgPrefixes.Length)]} {OrgTypes[rnd.Next(OrgTypes.Length)]} {rnd.Next(1, 60)}";
            var title = $"{Subjects[rnd.Next(Subjects.Length)]} #{i + 1}";

            table.Rows.Add(title, org, (byte)status, (byte)priority, assignedTo, created, updated);
        }
        return table;
    }

    private static RequestStatus PickStatus(Random r)
    {
        var x = r.NextDouble();
        return x < 0.25 ? RequestStatus.New
             : x < 0.55 ? RequestStatus.InProgress
             : x < 0.70 ? RequestStatus.Waiting
             : RequestStatus.Completed;
    }

    private static RequestPriority PickPriority(Random r)
    {
        var x = r.NextDouble();
        return x < 0.30 ? RequestPriority.Low
             : x < 0.80 ? RequestPriority.Medium
             : RequestPriority.High;
    }
}
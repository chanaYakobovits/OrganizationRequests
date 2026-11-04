using Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repositories;

namespace API.Tests;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Testing");

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // הגנה: לא מוחקים מסד שהשם שלו לא נראה כמו מסד בדיקות
        var dbName = db.Database.GetDbConnection().Database;
        if (!dbName.Contains("Test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Refusing to reset non-test database '{dbName}'.");

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        var now = DateTime.UtcNow;
        Request R(string title, string org, RequestStatus s, int daysAgo) => new()
        {
            Title = title,
            OrganizationName = org,
            Status = s,
            Priority = RequestPriority.Medium,
            CreatedAt = now.AddDays(-daysAgo),
            UpdatedAt = now.AddDays(-daysAgo)
        };

        db.Requests.AddRange(
            R("Alpha request 1", "Beta Engineering", RequestStatus.New, 1),
            R("Alpha request 2", "Gamma Services", RequestStatus.InProgress, 2),
            R("Gamma complaint", "Alpha Construction", RequestStatus.New, 3),
            R("Delta update", "Delta Education", RequestStatus.Completed, 4),
            R("100% done", "Omega", RequestStatus.Waiting, 5));
        await db.SaveChangesAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
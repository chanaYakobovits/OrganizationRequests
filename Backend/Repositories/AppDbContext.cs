using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Repositories;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, string connectionString)
    {
       
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<DbSeeder>();
        services.AddScoped<IRequestRepository, RequestRepository>();
        return services;
    }
}
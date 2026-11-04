using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Services.IServices;
using Services.Services;

namespace Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<ISummaryCache, SummaryCache>();
            services.AddScoped<IRequestService, RequestService>();
            return services;
        }
    }
}

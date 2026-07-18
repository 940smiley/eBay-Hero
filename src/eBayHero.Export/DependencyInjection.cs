using eBayHero.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace eBayHero.Export;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryExport(this IServiceCollection services)
    {
        services.AddSingleton<IEbayExportService, EbayExportService>();
        return services;
    }
}



using InventoryPhotoOps.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryPhotoOps.Export;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryExport(this IServiceCollection services)
    {
        services.AddSingleton<IEbayExportService, EbayExportService>();
        return services;
    }
}


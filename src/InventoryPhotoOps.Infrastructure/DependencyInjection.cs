using InventoryPhotoOps.Core.Configuration;
using InventoryPhotoOps.Core.Services;
using InventoryPhotoOps.Infrastructure.Data;
using InventoryPhotoOps.Infrastructure.Migration;
using InventoryPhotoOps.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryPhotoOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        InventoryOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.DatabasePath)!);
        services.AddSingleton(options);
        services.AddDbContextFactory<InventoryDbContext>(builder =>
            builder.UseSqlite($"Data Source={options.DatabasePath}"));
        services.AddScoped<IJsonMigrationService, LegacyJsonMigrationService>();
        services.AddScoped<ICardOpsImportService, CardOpsImportService>();
        services.AddScoped<DatabaseMaintenanceService>();
        services.AddSingleton<ISecretStore, DpapiFileSecretStore>();
        services.AddSingleton<ISecretRedactor, SecretRedactor>();
        services.AddSingleton<IRootManagementService, RootManagementService>();
        services.AddSingleton<IPricingService, PricingService>();
        services.AddSingleton<ILotBuilderService, LotBuilderService>();
        services.AddSingleton<IListingDraftService, ListingDraftService>();
        services.AddSingleton<IListingAuditService, ListingAuditService>();
        services.AddSingleton<IEbayConnectionService, EbayConnectionService>();
        return services;
    }
}

using eBayHero.Core.Configuration;
using eBayHero.Core.Ebay;
using eBayHero.Core.Entitlements;
using eBayHero.Core.Plugins;
using eBayHero.Core.Services;
using eBayHero.Infrastructure.Data;
using eBayHero.Infrastructure.Migrations;
using eBayHero.Infrastructure.Security;
using eBayHero.Infrastructure.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eBayHero.Infrastructure;

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
        services.AddSingleton<RedactingBundleWriter>();
        services.AddScoped<IDiagnosticsBundleService, DiagnosticsBundleService>();
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

    /// <summary>
    /// Registers the entitlement controller and the plugin runtime.
    ///
    /// Kept separate from AddInventoryInfrastructure because the ecosystem is optional: a
    /// host that only needs persistence can skip it. Plugin projects are intentionally NOT
    /// referenced here - callers register their own plugins at the composition root, which
    /// preserves the one-way dependency rule (plugins -> Core).
    /// </summary>
    public static IServiceCollection AddEcosystem(
        this IServiceCollection services,
        EntitlementOptions? entitlementOptions = null,
        ILicenseKeyValidator? licenseValidator = null,
        IReadOnlyDictionary<string, string>? pluginSettings = null)
    {
        var options = entitlementOptions ?? new EntitlementOptions();

        services.AddSingleton(options);
        services.AddSingleton<IEntitlementService>(_ => new EntitlementService(options, licenseValidator));
        services.AddSingleton<IPluginRegistry>(provider =>
            new PluginRegistry(provider.GetRequiredService<IEntitlementService>()));
        services.AddSingleton<IPluginHost>(provider =>
            new PluginHost(
                provider.GetRequiredService<IPluginRegistry>(),
                provider.GetRequiredService<IEntitlementService>(),
                pluginSettings,
                provider));
        services.AddSingleton(new EbayListingMapper());
        services.AddSingleton(provider => new BulkDraftGenerator(
            provider.GetRequiredService<EbayListingMapper>(),
            provider.GetRequiredService<IEntitlementService>()));
        return services;
    }
}


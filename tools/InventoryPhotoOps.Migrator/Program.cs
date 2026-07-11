using System.Text.Json;
using InventoryPhotoOps.Core.Configuration;
using InventoryPhotoOps.Core.Services;
using InventoryPhotoOps.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var parsed = ParseArgs(args);
var options = new InventoryOptions();
if (parsed.TryGetValue("ops-root", out var opsRoot)) options.OperationsRoot = opsRoot;
var jsonPath = parsed.GetValueOrDefault("json", options.LegacyJsonPath);
var databasePath = parsed.GetValueOrDefault("database", options.DatabasePath);
var apply = parsed.ContainsKey("apply");
var allowLive = parsed.ContainsKey("allow-live");

if (!apply && !parsed.ContainsKey("dry-run"))
{
    Console.Error.WriteLine("Specify --dry-run or --apply.");
    return 2;
}

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddInventoryInfrastructure(options))
    .Build();

try
{
    var migrator = host.Services.GetRequiredService<IJsonMigrationService>();
    var report = await migrator.MigrateAsync(
        new MigrationOptions(jsonPath, databasePath, apply, allowLive),
        CancellationToken.None);
    Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new
    {
        error = ex.Message,
        exception = ex.ToString()
    }, new JsonSerializerOptions { WriteIndented = true }));
    return 1;
}

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = arg[2..];
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            result[key] = args[++i];
        }
        else
        {
            result[key] = "true";
        }
    }

    return result;
}


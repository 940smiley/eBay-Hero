using InventoryPhotoOps.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryPhotoOps.FileSystem;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryFileSystem(this IServiceCollection services)
    {
        services.AddSingleton<IPathService, WindowsPathService>();
        services.AddSingleton<IHashService, HashService>();
        services.AddSingleton<IFileScanner, FileScanner>();
        services.AddSingleton<IFileOperationPlanner, FileOperationPlanner>();
        return services;
    }
}


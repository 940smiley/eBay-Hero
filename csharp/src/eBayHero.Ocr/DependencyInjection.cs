using eBayHero.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace eBayHero.Ocr;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryOcr(this IServiceCollection services)
    {
        services.AddSingleton<IImagePreprocessingService, ImagePreprocessingService>();
        services.AddSingleton<IImageEditService, ImageEditService>();
        services.AddSingleton<IOcrService, TesseractOcrService>();
        services.AddSingleton<IOcrLearningService, OcrLearningService>();
        return services;
    }
}


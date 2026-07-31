using Microsoft.Extensions.DependencyInjection;
using PagePdf.Application.Interfaces;
using PagePdf.Infrastructure.Services;

namespace PagePdf.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IComicArchiveReader, ZipComicArchiveReader>();
        services.AddSingleton<IPdfGenerator, PdfSharpPdfGenerator>();
        return services;
    }
}

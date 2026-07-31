using Microsoft.Extensions.DependencyInjection;
using PagePdf.Application.UseCases;
using PagePdf.Infrastructure.DependencyInjection;

namespace PagePdf.UI;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        services.AddScoped<ConvertComicUseCase>();
        await using var provider = services.BuildServiceProvider();

        var useCase = provider.GetRequiredService<ConvertComicUseCase>();
        await useCase.ExecuteAsync(new("input.cdz", "output.pdf"));
        return 0;
    }
}

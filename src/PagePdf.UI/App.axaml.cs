using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PagePdf.Application.UseCases;
using PagePdf.Infrastructure.DependencyInjection;

namespace PagePdf.UI;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            services.AddInfrastructure();
            services.AddSingleton<ConvertComicUseCase>();

            var provider = services.BuildServiceProvider();
            desktop.MainWindow = new MainWindow(provider.GetRequiredService<ConvertComicUseCase>());
        }

        base.OnFrameworkInitializationCompleted();
    }
}

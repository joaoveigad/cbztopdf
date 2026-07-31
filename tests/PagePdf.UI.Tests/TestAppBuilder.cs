using Avalonia;
using Avalonia.Headless;
using PagePdf.UI;

[assembly: AvaloniaTestApplication(typeof(PagePdf.UI.Tests.TestAppBuilder))]

namespace PagePdf.UI.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

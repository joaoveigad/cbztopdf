using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace PagePdf.UI.Tests;

public class MainWindowTests
{
    [AvaloniaFact]
    public void MainWindow_has_file_menu()
    {
        var window = new MainWindow();
        window.Show();

        var fileMenu = window.FindControl<MenuItem>("FileMenu");

        Assert.NotNull(fileMenu);
        Assert.Equal("File", fileMenu!.Header.ToString());
    }
}

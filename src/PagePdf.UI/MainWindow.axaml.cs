using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PagePdf.Application.DTOs;
using PagePdf.Application.UseCases;
using PagePdf.Infrastructure.DependencyInjection;

namespace PagePdf.UI;

public partial class MainWindow : Window
{
    private readonly ConvertComicUseCase _useCase;
    private string? _selectedArchive;
    private bool _isConverting;

    internal Func<string, string, Task> ShowError { get; set; } = default!;

    public MainWindow()
        : this(CreateDefaultUseCase())
    {
    }

    public MainWindow(ConvertComicUseCase useCase)
    {
        _useCase = useCase;
        ShowError = ShowErrorAsync;
        InitializeComponent();
    }

    private static ConvertComicUseCase CreateDefaultUseCase()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        services.AddSingleton<ConvertComicUseCase>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ConvertComicUseCase>();
    }

    private async void OpenMenuItem_Click(object? sender, RoutedEventArgs e)
        => await OpenArchiveAsync();

    private async void OpenButton_Click(object? sender, RoutedEventArgs e)
        => await OpenArchiveAsync();

    private async void ExportMenuItem_Click(object? sender, RoutedEventArgs e)
        => await ExportAsync();

    private void ExitMenuItem_Click(object? sender, RoutedEventArgs e)
        => Close();

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var file = e.DataTransfer.TryGetFile();
        if (file is null)
        {
            return;
        }

        var path = file.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        await SelectArchiveAsync(path);
    }

    private async Task OpenArchiveAsync()
    {
        if (_isConverting)
        {
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a .cbz comic archive",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Comic archives") { Patterns = ["*.cbz"] },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            ],
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null)
        {
            await SelectArchiveAsync(path);
        }
    }

    internal async Task SelectArchiveAsync(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            await ShowError("File not found", $"The selected file was not found:\n{archivePath}");
            return;
        }

        _selectedArchive = archivePath;
        var fileName = Path.GetFileName(archivePath);
        DropTitleText.Text = fileName;
        DropSubtitleText.Text = "click Export PDF... to convert it";
        SetBusy(false);
        StatusText.Text = $"Selected: {fileName}";
        ProgressBar.Value = 0;
    }

    private async Task ExportAsync()
    {
        if (_isConverting)
        {
            return;
        }

        if (_selectedArchive is null)
        {
            await OpenArchiveAsync();
            if (_selectedArchive is null)
            {
                return;
            }
        }

        var topLevel = GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var defaultName = Path.GetFileNameWithoutExtension(_selectedArchive) + ".pdf";
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PDF as",
            SuggestedFileName = defaultName,
            DefaultExtension = "pdf",
            FileTypeChoices =
            [
                new FilePickerFileType("PDF documents") { Patterns = ["*.pdf"] },
            ],
        });

        var outputPath = file?.TryGetLocalPath();
        if (outputPath is null)
        {
            return;
        }

        await ConvertAsync(outputPath);
    }

    internal async Task ConvertAsync(string outputPath)
    {
        _isConverting = true;
        SetBusy(true);
        ProgressBar.Value = 0;

        try
        {
            var progress = new Progress<int>(value =>
            {
                ProgressBar.Value = value;
                StatusText.Text = $"Converting... {value}%";
            });

            var result = await Task.Run(() => _useCase.ExecuteAsync(
                new ConvertComicRequest(_selectedArchive!, outputPath),
                progress));

            ProgressBar.Value = result.PageCount > 0 ? 100 : 0;
            StatusText.Text = $"Done — {result.PageCount} pages in {result.Elapsed.TotalSeconds:F1}s";
        }
        catch (Exception ex)
        {
            await ShowError("Conversion failed", ex.Message);
            StatusText.Text = "Conversion failed";
        }
        finally
        {
            _isConverting = false;
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        OpenButton.IsEnabled = !busy;
        OpenMenuItem.IsEnabled = !busy;
        ExportMenuItem.IsEnabled = !busy && _selectedArchive is not null;
        ConvertButton.IsEnabled = !busy && _selectedArchive is not null;
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var ok = new Button { Content = "OK", MinWidth = 90, Classes = { "primary" } };
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400,
        };

        var panel = new StackPanel { Spacing = 16, Margin = new Avalonia.Thickness(24) };
        panel.Children.Add(text);
        ok.Click += (_, _) => Close();
        panel.Children.Add(ok);

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = Avalonia.Controls.SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = panel,
            CanResize = false,
        };

        await dialog.ShowDialog(this);
    }
}

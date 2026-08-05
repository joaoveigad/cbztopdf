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
    private readonly List<QueueItem> _queue = [];
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
        => await OpenArchivesAsync();

    private async void OpenButton_Click(object? sender, RoutedEventArgs e)
        => await OpenArchivesAsync();

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
        var paths = e.DataTransfer.TryGetFiles()
            ?.Select(f => f.TryGetLocalPath())
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        if (paths is { Count: > 0 })
        {
            await SelectArchivesAsync(paths);
        }
    }

    private async Task OpenArchivesAsync()
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
            Title = "Select .cbz comic archives",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Comic archives") { Patterns = ["*.cbz"] },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            ],
        });

        var paths = files.Select(f => f.TryGetLocalPath())
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        if (paths.Count > 0)
        {
            await SelectArchivesAsync(paths);
        }
    }

    internal async Task SelectArchiveAsync(string archivePath)
        => await SelectArchivesAsync([archivePath]);

    internal async Task SelectArchivesAsync(IEnumerable<string> archivePaths)
    {
        if (_isConverting)
        {
            return;
        }

        var added = false;
        foreach (var archivePath in archivePaths)
        {
            if (!File.Exists(archivePath))
            {
                await ShowError("File not found", $"The selected file was not found:\n{archivePath}");
                continue;
            }

            if (_queue.Any(item => item.ArchivePath == archivePath))
            {
                continue;
            }

            _queue.Add(new QueueItem(archivePath));
            added = true;
        }

        if (!added)
        {
            return;
        }

        RefreshQueueUi();
        SetBusy(false);
        ProgressBar.Value = 0;
        StatusText.Text = PendingCount() == 1 ? "1 file queued" : $"{PendingCount()} files queued";
    }

    private void RefreshQueueUi()
    {
        QueueList.ItemsSource = _queue
            .Select(item => $"{item.FileName} → {item.Status}")
            .ToList();
        QueueList.IsVisible = _queue.Count > 0;
    }

    private int PendingCount()
        => _queue.Count(item => item.Status != "done");

    private bool HasPendingWork()
        => PendingCount() > 0;

    private async Task ExportAsync()
    {
        if (_isConverting)
        {
            return;
        }

        if (!HasPendingWork())
        {
            await OpenArchivesAsync();
            if (!HasPendingWork())
            {
                return;
            }
        }

        var pending = _queue.Where(item => item.Status != "done").ToList();

        var topLevel = GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        string? folder = null;
        string? singleOutput = null;

        if (pending.Count == 1)
        {
            var defaultName = Path.GetFileNameWithoutExtension(pending[0].ArchivePath) + ".pdf";
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

            singleOutput = file?.TryGetLocalPath();
            if (singleOutput is null)
            {
                return;
            }
        }
        else
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select output folder",
                AllowMultiple = false,
            });

            folder = folders.FirstOrDefault()?.TryGetLocalPath();
            if (folder is null)
            {
                return;
            }
        }

        await ConvertAllAsync(folder, singleOutput);
    }

    internal async Task ConvertAsync(string outputPath)
    {
        if (_isConverting || !HasPendingWork())
        {
            return;
        }

        await ConvertAllAsync(folder: null, singleOutput: outputPath);
    }

    private async Task ConvertAllAsync(string? folder, string? singleOutput)
    {
        _isConverting = true;
        SetBusy(true);
        ProgressBar.Value = 0;

        var pending = _queue.Where(item => item.Status != "done").ToList();
        var total = pending.Count;
        var processed = 0;

        try
        {
            if (total == 0)
            {
                return;
            }

            for (var i = 0; i < total; i++)
            {
                var item = pending[i];
                var fileName = item.FileName;
                var outputPath = singleOutput
                    ?? Path.Combine(folder!, Path.GetFileNameWithoutExtension(item.ArchivePath) + ".pdf");

                item.Status = "converting";
                RefreshQueueUi();
                StatusText.Text = $"Converting {fileName} ({i + 1}/{total})...";
                ProgressBar.Value = 0;

                var progress = new Progress<int>(value =>
                    StatusText.Text = $"Converting {fileName} ({i + 1}/{total})... {value}%");

                try
                {
                    await Task.Run(() => _useCase.ExecuteAsync(
                        new ConvertComicRequest(item.ArchivePath, outputPath),
                        progress));

                    item.Status = "done";
                    processed++;
                }
                catch (Exception ex)
                {
                    item.Status = "failed";
                    await ShowError("Conversion failed", $"{fileName}:\n{ex.Message}");
                    StatusText.Text = $"Conversion failed at {fileName}";
                    ProgressBar.Value = 0;
                    break;
                }

                RefreshQueueUi();
            }

            if (processed == total)
            {
                StatusText.Text = $"Done — {processed} file(s) converted";
                ProgressBar.Value = 100;
            }
        }
        finally
        {
            _isConverting = false;
            SetBusy(false);
            RefreshQueueUi();
        }
    }

    private void SetBusy(bool busy)
    {
        OpenButton.IsEnabled = !busy;
        OpenMenuItem.IsEnabled = !busy;
        var hasPending = HasPendingWork();
        ExportMenuItem.IsEnabled = !busy && hasPending;
        ConvertButton.IsEnabled = !busy && hasPending;
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

    private sealed class QueueItem(string archivePath)
    {
        public string ArchivePath { get; } = archivePath;

        public string FileName => Path.GetFileName(ArchivePath);

        public string Status { get; set; } = "queued";
    }
}

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using PagePdf.Application.DTOs;
using PagePdf.Application.Interfaces;
using PagePdf.Application.UseCases;
using PagePdf.Domain.Entities;
using PagePdf.Domain.Exceptions;
using Xunit;

namespace PagePdf.UI.Tests;

public class MainWindowTests
{
    [AvaloniaFact]
    public void MainWindow_has_file_menu()
    {
        var window = new MainWindow(CreateUseCase(1));

        var fileMenu = window.FindControl<MenuItem>("FileMenu");

        Assert.NotNull(fileMenu);
        Assert.Equal("File", fileMenu!.Header?.ToString());
    }

    [AvaloniaFact]
    public void MainWindow_has_open_export_and_exit_items()
    {
        var window = new MainWindow(CreateUseCase(1));

        Assert.NotNull(window.FindControl<MenuItem>("OpenMenuItem"));
        Assert.NotNull(window.FindControl<MenuItem>("ExportMenuItem"));
        Assert.NotNull(window.FindControl<MenuItem>("ExitMenuItem"));
        Assert.NotNull(window.FindControl<Button>("OpenButton"));
    }

    [AvaloniaFact]
    public async Task MainWindow_select_archive_enables_export()
    {
        var window = new MainWindow(CreateUseCase(1));
        var tempCbz = CreateTempCbz();

        try
        {
            await window.SelectArchiveAsync(tempCbz);

            var export = window.FindControl<MenuItem>("ExportMenuItem");
            Assert.True(export!.IsEnabled);
            Assert.Equal($"Selected: {Path.GetFileName(tempCbz)}",
                window.FindControl<TextBlock>("StatusText")!.Text);
        }
        finally
        {
            File.Delete(tempCbz);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_select_missing_archive_shows_error()
    {
        var window = new MainWindow(CreateUseCase(1));
        var errors = new List<string>();
        window.ShowError = (title, message) =>
        {
            errors.Add($"{title}: {message}");
            return Task.CompletedTask;
        };

        await window.SelectArchiveAsync(Path.Combine(Path.GetTempPath(), "missing.cbz"));

        Assert.Contains(errors, e => e.StartsWith("File not found:"));
        Assert.False(window.FindControl<MenuItem>("ExportMenuItem")!.IsEnabled);
    }

    [AvaloniaFact]
    public async Task MainWindow_convert_updates_progress_and_status()
    {
        var tempCbz = CreateTempCbz();
        var outputPdf = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        try
        {
            var window = new MainWindow(CreateUseCase(3));
            await window.SelectArchiveAsync(tempCbz);
            await window.ConvertAsync(outputPdf);

            Assert.Equal(100, window.FindControl<ProgressBar>("ProgressBar")!.Value);
            Assert.Contains("Done", window.FindControl<TextBlock>("StatusText")!.Text);
            Assert.True(window.FindControl<Button>("OpenButton")!.IsEnabled);
        }
        finally
        {
            File.Delete(tempCbz);
            File.Delete(outputPdf);
        }
    }

    [AvaloniaFact]
    public async Task MainWindow_convert_failure_shows_error_and_reports_status()
    {
        var tempCbz = CreateTempCbz();
        var outputPdf = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        var errors = new List<string>();

        try
        {
            var window = new MainWindow(CreateUseCase(1, failGenerator: true));
            window.ShowError = (title, message) =>
            {
                errors.Add($"{title}: {message}");
                return Task.CompletedTask;
            };

            await window.SelectArchiveAsync(tempCbz);
            await window.ConvertAsync(outputPdf);

            Assert.Contains(errors, e => e.StartsWith("Conversion failed:"));
            Assert.Contains("failed", window.FindControl<TextBlock>("StatusText")!.Text);
            Assert.True(window.FindControl<Button>("OpenButton")!.IsEnabled);
        }
        finally
        {
            File.Delete(tempCbz);
            File.Delete(outputPdf);
        }
    }

    private static string CreateTempCbz()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cbz");
        File.WriteAllBytes(path, [0x50, 0x4B, 0x05, 0x06]);
        return path;
    }

    private static ConvertComicUseCase CreateUseCase(int pageCount, bool failGenerator = false)
    {
        var pages = new ComicPage[pageCount];
        for (var i = 0; i < pageCount; i++)
        {
            pages[i] = new ComicPage
            {
                Number = i + 1,
                FileName = $"p{i + 1}.png",
                ImageData = [1],
            };
        }

        var archive = new ComicArchive { FilePath = "fake.cbz", Pages = pages };

        return new ConvertComicUseCase(
            new FakeReader(archive),
            failGenerator ? new ThrowingGenerator() : new FakeGenerator());
    }

    private sealed class FakeReader : IComicArchiveReader
    {
        private readonly ComicArchive _archive;

        public FakeReader(ComicArchive archive) => _archive = archive;

        public Task<ComicArchive> ReadAsync(string archivePath, CancellationToken cancellationToken = default)
            => Task.FromResult(_archive);
    }

    private sealed class FakeGenerator : IPdfGenerator
    {
        public Task GenerateAsync(
            ComicArchive archive,
            string outputPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < archive.Pages.Count; i++)
            {
                progress?.Report((int)Math.Round((i + 1) * 100.0 / archive.Pages.Count));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingGenerator : IPdfGenerator
    {
        public Task GenerateAsync(
            ComicArchive archive,
            string outputPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
            => throw new ComicArchiveException("Falha na geração.");
    }
}

using PagePdf.Application.Interfaces;
using PagePdf.Domain.Entities;

namespace PagePdf.Application.Tests.Fakes;

public sealed class FakePdfGenerator : IPdfGenerator
{
    private int _reportCount;

    public int ReportCount => _reportCount;

    public Task GenerateAsync(
        ComicArchive archive,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _reportCount = 0;
        foreach (var _ in archive.Pages)
        {
            _reportCount++;
            progress?.Report(_reportCount);
        }

        return Task.CompletedTask;
    }
}

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
        for (var i = 0; i < archive.Pages.Count; i++)
        {
            _reportCount++;
            progress?.Report((int)Math.Round(_reportCount * 100.0 / archive.Pages.Count));
        }

        return Task.CompletedTask;
    }
}

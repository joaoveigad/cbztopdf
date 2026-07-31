using PagePdf.Application.Interfaces;
using PagePdf.Domain.Entities;

namespace PagePdf.Application.Tests.Fakes;

public sealed class FakeComicArchiveReader : IComicArchiveReader
{
    private readonly ComicArchive _archive;

    public FakeComicArchiveReader(ComicArchive archive)
    {
        _archive = archive;
    }

    public Task<ComicArchive> ReadAsync(string archivePath, CancellationToken cancellationToken = default)
        => Task.FromResult(_archive);
}

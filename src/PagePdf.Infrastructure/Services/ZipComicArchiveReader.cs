using PagePdf.Application.Interfaces;
using PagePdf.Domain.Entities;

namespace PagePdf.Infrastructure.Services;

public sealed class ZipComicArchiveReader : IComicArchiveReader
{
    public Task<ComicArchive> ReadAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

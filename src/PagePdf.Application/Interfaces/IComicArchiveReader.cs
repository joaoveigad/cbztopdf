using PagePdf.Domain.Entities;

namespace PagePdf.Application.Interfaces;

public interface IComicArchiveReader
{
    Task<ComicArchive> ReadAsync(string archivePath, CancellationToken cancellationToken = default);
}

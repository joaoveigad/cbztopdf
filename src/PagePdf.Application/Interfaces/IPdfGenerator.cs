    using PagePdf.Domain.Entities;

namespace PagePdf.Application.Interfaces;

public interface IPdfGenerator
{
    Task GenerateAsync(
        ComicArchive archive,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

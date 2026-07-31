using PagePdf.Domain.Entities;

namespace PagePdf.Application.Interfaces;

public interface IPdfGenerator
{
    Task GenerateAsync(ComicArchive archive, string outputPath, CancellationToken cancellationToken = default);
}

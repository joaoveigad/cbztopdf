namespace PagePdf.Domain.Entities;

public sealed class ComicArchive
{
    public required string FilePath { get; init; }
    public required IReadOnlyList<ComicPage> Pages { get; init; }

    public int PageCount => Pages.Count;
}

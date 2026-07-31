namespace PagePdf.Domain.Entities;

public sealed class ComicPage
{
    public required int Number { get; init; }
    public required string FileName { get; init; }
    public required byte[] ImageData { get; init; }
}

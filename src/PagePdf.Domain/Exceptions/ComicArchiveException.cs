namespace PagePdf.Domain.Exceptions;

public sealed class ComicArchiveException : Exception
{
    public ComicArchiveException(string message)
        : base(message)
    {
    }

    public ComicArchiveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

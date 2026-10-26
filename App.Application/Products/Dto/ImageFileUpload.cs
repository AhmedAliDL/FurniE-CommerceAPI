namespace App.Application.Products.Dto
{
    // Application-layer stand-in for an uploaded file so handlers stay free of ASP.NET types.
    // The handler disposes Content after saving.
    public record ImageFileUpload(string FileName, string ContentType, long Length, Stream Content);
}

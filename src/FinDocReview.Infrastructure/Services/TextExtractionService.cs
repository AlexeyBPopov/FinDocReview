using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FinDocReview.Infrastructure.Services;

public class TextExtractionService
{
    private readonly LocalStorageService _storage;

    public TextExtractionService(LocalStorageService storage)
    {
        _storage = storage;
    }

    public async Task<string> ExtractTextAsync(
        string? localPath,
        string? blobUri,
        string fileName,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        // Download to temp file if using Azure Blob
        string? tempFile = null;
        string filePath;

        if (!string.IsNullOrEmpty(blobUri))
        {
            tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{ext}");
            await using var stream = await _storage.GetFileAsync(null, blobUri);
            await using var fs = File.Create(tempFile);
            await stream.CopyToAsync(fs, ct);
            filePath = tempFile;
        }
        else
        {
            filePath = localPath ?? throw new InvalidOperationException("No file path provided");
        }

        try
        {
            return ext switch
            {
                ".pdf" => ExtractFromPdf(filePath),
                ".txt" => ExtractFromTxt(filePath),
                ".docx" => ExtractFromDocx(filePath),
                _ => throw new NotSupportedException($"File type '{ext}' is not supported.")
            };
        }
        finally
        {
            if (tempFile != null && File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private static string ExtractFromPdf(string filePath)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(filePath);
        foreach (Page page in pdf.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractFromTxt(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    private static string ExtractFromDocx(string filePath)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;
        foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            sb.AppendLine(para.InnerText);
        return sb.ToString();
    }
}
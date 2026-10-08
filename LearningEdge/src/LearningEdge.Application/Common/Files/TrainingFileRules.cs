namespace LearningEdge.Application.Common.Files;

public static class TrainingFileRules
{
    public const int MaxFilesPerUpload = 10;
    public const long MaxFileBytes = 20L * 1024 * 1024;
    public const long MaxTotalBytes = 100L * 1024 * 1024;
    public const int MaxFileNameLength = 255;

    // Content types are fixed per extension rather than trusted from the client.
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".txt"] = "text/plain"
    };

    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    public static string AllowedExtensionsText => string.Join(", ", ContentTypes.Keys);

    public static bool IsAllowedExtension(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName) && ContentTypes.ContainsKey(Path.GetExtension(fileName));

    public static string ContentTypeFor(string fileName) => ContentTypes[Path.GetExtension(fileName)];

    /// <summary>
    /// Checks the leading bytes so a renamed executable or archive can't pass as a document.
    /// Leaves the stream positioned at the start.
    /// </summary>
    public static async Task<bool> HasExpectedContentAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var read = await content.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;
        var head = buffer.AsSpan(0, read);

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => head.StartsWith(PdfSignature),
            ".docx" => head.StartsWith(ZipSignature),
            ".txt" => HasUtf16Bom(head) || !head.Contains((byte)0),
            _ => false
        };
    }

    private static bool HasUtf16Bom(ReadOnlySpan<byte> head) =>
        head.Length >= 2 && ((head[0] == 0xFF && head[1] == 0xFE) || (head[0] == 0xFE && head[1] == 0xFF));
}

namespace ContestApi.Services;

public class StoreOptions
{
    // Name of your Azure Blob Storage container (e.g. "contest-photos")
    public string ContainerName { get; set; } = "contest-photos";

    // Max upload size in bytes (default 5 MB)
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;

    // Allowed content types for uploads
    public HashSet<string> AllowedContentTypes { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/webp",
            "image/jpeg",
            "image/png"
        };
}

// curl -X PUT --upload-file ./photo.jpg \
//   -H "x-ms-blob-type: BlockBlob" \
//   -H "Content-Type: image/jpeg" \
//   "https://stcontestphotos1234.blob.core.windows.net/contest-photos/photos/2025/10/6ddb991013814f4294f41fe7ef8ea1cf.jpg?sv=2025-11-05&st=2025-10-21T20%3A45%3A34Z&se=2025-10-21T20%3A56%3A34Z&sr=b&sp=cw&rsct=image%2Fjpeg&sig=8ls3a4Z1CdFU9b%2FCoEgfe6USwD5v1izbx8oKf278AzA%3D"


// curl -X PUT --upload-file ./photo.jpg \
//   -H "x-ms-blob-type: BlockBlob" \
//   -H "Content-Type: image/jpeg" \
// "https://stcontestphotos1234.blob.core.windows.net/contest-photos/photos/2025/10/5bb1f51f4f224a2a87b586ac5b602e89.jpg?sv=2025-11-05&st=2025-10-21T20%3A47%3A59Z&se=2025-10-21T20%3A58%3A59Z&sr=b&sp=cw&rsct=image%2Fjpeg&sig=CzygGq1jGy6wXLklNV0bJH3vWj%2FyL75ondFdJpk%2BTEY%3D"
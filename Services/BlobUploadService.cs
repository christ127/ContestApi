using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ContestApi.Models;

namespace ContestApi.Services;
public class BlobUploadService
{
    private readonly BlobServiceClient _service;
    private readonly StoreOptions _options;

    public BlobUploadService(IConfiguration cfg, StoreOptions options)
    {
       var conn =
        // Azure App Setting env var: Storage__ConnectionString
        cfg["Storage__ConnectionString"]
        // Azure “Connection strings” blade (becomes CUSTOMCONNSTR_Storage)
        ?? cfg.GetConnectionString("Storage")
        // JSON / user-secrets: { "Storage": { "ConnectionString": "..." } }
        ?? cfg["Storage:ConnectionString"]
        ?? throw new InvalidOperationException("Storage__ConnectionString is required");
        _service = new BlobServiceClient(conn);   // <- uses connection string (shared key)
        _options = options;
    }

    public async Task<PresignResponse> GetWriteSasAsync(string fileName, string contentType, long bytes)
    {
        if (!_options.AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException("Unsupported content type.");
        if (bytes > _options.MaxBytes)
            throw new InvalidOperationException("File too large.");

        var container = _service.GetBlobContainerClient(_options.ContainerName);
        await container.CreateIfNotExistsAsync();

        var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var blobName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}-{safeName}";
        var blob = container.GetBlobClient(blobName);

        var starts  = DateTimeOffset.UtcNow.AddMinutes(-1);
        var expires = DateTimeOffset.UtcNow.AddMinutes(10);

        var sas = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName          = blob.Name,
            Resource          = "b",
            StartsOn          = starts,
            ExpiresOn         = expires,
            ContentType       = contentType
        };
        sas.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException("Blob client cannot generate SAS (missing shared key creds).");

        var uploadUrl = blob.GenerateSasUri(sas);
        return new PresignResponse(blobName,uploadUrl,expires);
    }
}
namespace ContestApi.Models;

public record PresignResponse(
    string BlobName,
    Uri UploadUrl,
    DateTimeOffset ExpiresAtUtc
);
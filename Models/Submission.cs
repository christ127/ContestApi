namespace ContestApi.Models;

public class Submission
{
    public Guid SubmissionId { get; set; } = Guid.NewGuid();

    public int ContestId { get; set; }
    public Contest Contest { get; set; } = default!;

    public string FirstName { get; set; } = default!;
    public string LastName  { get; set; } = default!;
    public string Email     { get; set; } = default!;
    public string Phone {get; set;} = default!;
    public bool ConsentGiven { get; set; }
    public string ConsentVersion { get; set; } = "v1";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

     // File metadata
    public string? BlobName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
}
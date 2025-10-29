using System.ComponentModel.DataAnnotations;

namespace ContestApi.Models;

public class SubmissionDto
{
    public string ContestSlug { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";     // NEW (optional)
    public bool ConsentGiven { get; set; }
    public string? ConsentVersion { get; set; } = "v1";

    public string? BlobName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
}



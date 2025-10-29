using System.ComponentModel.DataAnnotations.Schema;

namespace ContestApi.Models;

[Table("Contest")] // 👈 forces EF to use the exact SQL table name
public class Contest
{
    public int ContestId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
using Microsoft.EntityFrameworkCore;
using ContestApi.Models;

namespace ContestApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Contest> Contests => Set<Contest>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Contest>().ToTable("Contest");      // or "Contest"
    modelBuilder.Entity<Submission>().ToTable("Submission"); // be explicit

     modelBuilder.Entity<Submission>()
        .Property(s => s.Phone)
        .HasMaxLength(40);

    modelBuilder.Entity<Submission>()
        .HasIndex(s => new { s.ContestId, s.Email })
        .IsUnique();
}
}
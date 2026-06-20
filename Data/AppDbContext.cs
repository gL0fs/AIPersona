using Microsoft.EntityFrameworkCore;
using Persona.Models;

namespace Persona.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<QuizSession> Sessions => Set<QuizSession>();
    public DbSet<QuizMessage> Messages => Set<QuizMessage>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<QuizSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasMany(s => s.Messages)
             .WithOne(m => m.Session)
             .HasForeignKey(m => m.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Content).IsRequired();
        });
    }
}

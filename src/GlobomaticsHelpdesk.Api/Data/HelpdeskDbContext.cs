using GlobomaticsHelpdesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GlobomaticsHelpdesk.Api.Data;

public class HelpdeskDbContext : DbContext
{
    public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(320).IsRequired();
            entity.Property(u => u.Department).HasMaxLength(120);
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Email).HasMaxLength(320).IsRequired();
            entity.Property(a => a.Specialty).HasMaxLength(120);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).IsRequired();
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(t => t.Requester)
                .WithMany()
                .HasForeignKey(t => t.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedAgent)
                .WithMany()
                .HasForeignKey(t => t.AssignedAgentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(t => t.Comments)
                .WithOne(c => c.Ticket!)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Body).IsRequired();

            entity.HasOne(c => c.AuthorUser)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.AuthorAgent)
                .WithMany()
                .HasForeignKey(c => c.AuthorAgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KnowledgeArticle>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Title).HasMaxLength(200).IsRequired();
            entity.Property(k => k.Body).IsRequired();
            entity.Property(k => k.Tags)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                        c => c.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                        c => c.ToList()));
        });
    }
}

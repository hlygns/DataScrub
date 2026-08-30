using DataScrub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataScrub.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Dataset> Datasets => Set<Dataset>();
        public DbSet<DetectedIssue> DetectedIssues => Set<DetectedIssue>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dataset>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.FileName).IsRequired().HasMaxLength(255);
                entity.Property(d => d.Status).HasConversion<string>();
            });

            modelBuilder.Entity<DetectedIssue>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Type).HasConversion<string>();
                entity.Property(i => i.Resolution).HasConversion<string>();

                // Bir Dataset'in birçok DetectedIssue'su olabilir - N+1 problemine düşmemek için
                // GetByIdWithIssuesAsync içinde Include() kullanacağız.
                entity.HasOne(i => i.Dataset)
                      .WithMany(d => d.Issues)
                      .HasForeignKey(i => i.DatasetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Sık sorgulanan alanlar için index - performans
                entity.HasIndex(i => i.DatasetId);
                entity.HasIndex(i => i.Resolution);
            });
        }
    }
}

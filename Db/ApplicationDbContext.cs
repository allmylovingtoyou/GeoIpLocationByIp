using Domain;
using Microsoft.EntityFrameworkCore;

namespace Db
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=location_test;Username=location_test;Password=location_test_pass");
            }
        }

        public DbSet<IpRecord> IpRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IpRecord>()
                .ToTable(nameof(IpRecords)
                    .ToLowerInvariant());

            modelBuilder.Entity<IpRecord>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<IpRecord>()
                .HasIndex(x => x.Network);
        }
    }
}
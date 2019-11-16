using System.Numerics;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
                .HasKey(x => x.Network);

            modelBuilder.Entity<IpRecord>()
                .HasIndex(x => x.Network);
            
//            var converter = new ValueConverter<BigInteger, long>(    
//                model => (long)model,
//                provider => new BigInteger(provider));
//
//            modelBuilder
//                .Entity<IpRecord>()
//                .Property(e => e.Hash)
//                .HasConversion(converter);
        }
    }
}
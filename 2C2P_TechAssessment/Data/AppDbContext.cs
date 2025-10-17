using Microsoft.EntityFrameworkCore;
using _2C2P_TechAssessment.Models;

namespace _2C2P_TechAssessment.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TransactionEntity> Transactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionEntity>()
                .HasIndex(t => t.TransactionId)
                .IsUnique(false);
        }
    }
}

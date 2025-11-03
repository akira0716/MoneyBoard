using Microsoft.EntityFrameworkCore;
using MoneyBoard.Models;

namespace MoneyBoard.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Mapping> Mappings { get; set; }
        public DbSet<ImportHistory> ImportHistories { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ImportHistoryのユニークインデックスを追加
            modelBuilder.Entity<ImportHistory>()
                .HasIndex(h => new { h.Year, h.Month })
                .IsUnique();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using MoneyBoard.Models;

namespace MoneyBoard.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Mapping> Mappings { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}

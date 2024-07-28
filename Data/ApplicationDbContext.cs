using Microsoft.EntityFrameworkCore;
using ContractBotApi.Models;

namespace ContractBotApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ConversationHistory> ConversationHistories { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<SpotContract> SpotContracts { get; set; }
        public DbSet<ForwardContract> ForwardContracts { get; set; }
        public DbSet<OptionContract> OptionContracts { get; set; }
        public DbSet<SwapContract> SwapContracts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Contract>().ToTable("Contracts");
            modelBuilder.Entity<SpotContract>().ToTable("SpotContracts");
            modelBuilder.Entity<ForwardContract>().ToTable("ForwardContracts");
            modelBuilder.Entity<OptionContract>().ToTable("OptionContracts");
            modelBuilder.Entity<SwapContract>().ToTable("SwapContracts");
        }
    }
}
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
        public DbSet<UploadedFile> UploadedFiles { get; set; }
    }
}
using Microsoft.EntityFrameworkCore;

namespace Youtube_API
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<ResponseLog> ResponseLogs { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
    }
}

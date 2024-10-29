using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fall2024_Assignment3_msingh9.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Fall2024_Assignment3_msingh9.Models.Actor> Actor { get; set; } = default!;
        public DbSet<Fall2024_Assignment3_msingh9.Models.Movie> Movie { get; set; } = default!;

        public DbSet<Fall2024_Assignment3_msingh9.Models.ActorMovie> ActorMovie { get; set; } = default!;
    }
}

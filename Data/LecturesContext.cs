using Microsoft.EntityFrameworkCore;

namespace Claudia.Data
{
    public class LecturesContext : DbContext
    {
        public LecturesContext(DbContextOptions<LecturesContext> options) : base(options)
        {
        }
         
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<Expiry> Expiries { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<SubComment> SubComments { get; set; }
    }
}
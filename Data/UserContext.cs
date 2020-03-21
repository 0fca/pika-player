using Claudia.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Claudia.Data
{
    public class UserContext : IdentityDbContext
    {
        public UserContext(DbContextOptions<UserContext> options) :base(options)
        {
        }
         
        public new DbSet<User> Users { get; set; }
    }
}
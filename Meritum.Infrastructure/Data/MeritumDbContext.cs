using Meritum.Core.Entities;
using Microsoft.EntityFrameworkCore;

public class MeritumDbContext : DbContext
{
    public MeritumDbContext(DbContextOptions<MeritumDbContext> options) : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Vote> Votes { get; set; }
}
using Microsoft.EntityFrameworkCore;
using WarningSystems.Core.DataAccess.SOPDataAccess.Context.Entity;

public class SopDbContext : DbContext
{
    public SopDbContext(DbContextOptions<SopDbContext> options)
        : base(options)
    {
    }

    public DbSet<SopDocument> SopDocuments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SopDocument>().ToTable("SOPDocument", "dbo");
    }
}
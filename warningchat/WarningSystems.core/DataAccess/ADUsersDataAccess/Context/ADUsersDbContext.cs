using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarningSystems.core.DataAccess.ADUsersDataAccess.Context.Entities;

namespace WarningSystems.core.DataAccess.ADUsersDataAccess.Context;

public class ADUsersDbContext : DbContext
{
    public ADUsersDbContext(DbContextOptions<ADUsersDbContext> options)
        : base(options)
    {
    }

    public DbSet<ADUser> ADUsers => Set<ADUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ADUser>(entity =>
        {
            entity.ToTable("ADUser", "dbo");

            entity.HasKey(user => user.ADUserID);
        });
    }
}

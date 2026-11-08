using Microsoft.EntityFrameworkCore;

namespace {{DBPROJECT}};

public class {{DBNAME}}Context(DbContextOptions<{{DBNAME}}Context> options) : DbContext(options)
{
  public DbSet<Dummy> Dummies => Set<Dummy>();

  protected override void OnModelCreating(ModelBuilder modelBuilder) =>
    modelBuilder.Entity<Dummy>().HasData(
      new Dummy { Id = 1, Name = "Erster Eintrag", CreatedAt = new DateTime(2026, 1, 1) },
      new Dummy { Id = 2, Name = "Zweiter Eintrag", CreatedAt = new DateTime(2026, 1, 2) });
}

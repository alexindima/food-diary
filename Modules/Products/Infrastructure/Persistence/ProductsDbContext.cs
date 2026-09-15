using FoodDiary.Modules.Products.PersistenceModel;
using FoodDiary.Modules.Products.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Products.Infrastructure.Persistence;

public sealed class ProductsDbContext(DbContextOptions<ProductsDbContext> options) : DbContext(options) {
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyProductsPersistenceModel();
}

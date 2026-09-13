using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Products.Infrastructure.Persistence;

public sealed class ProductsDbContext(DbContextOptions<ProductsDbContext> options) : DbContext(options) {
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyProductsPersistenceModel();
}

using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Products.Infrastructure.Persistence;

internal sealed class ProductsUserContentRetentionParticipant(
    ProductsDbContext context,
    IModuleTransactionCoordinator coordinator,
    IProductUserPurgeRetentionReadService retentionReadService) : IUserContentRetentionParticipant {
    public int Order => 10;

    public async Task LockOwnedContentAsync(UserId sourceUserId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        FormattableString sql = $"""
            SELECT "Id" AS "Value"
            FROM "Products"
            WHERE "UserId" = {sourceUserId.Value}
            ORDER BY "Id"
            FOR UPDATE
            """;
        _ = await context.Database.SqlQuery<Guid>(sql).ToArrayAsync(cancellationToken);
    }

    public async Task<bool> HasReferencedContentAsync(UserId sourceUserId, CancellationToken cancellationToken) {
        IReadOnlyCollection<ProductId> productIds = await retentionReadService
            .GetRetainedProductIdsAsync(sourceUserId, cancellationToken);
        return productIds.Count > 0;
    }

    public async Task RetainReferencedContentAsync(UserId sourceUserId, UserId targetUserId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        IReadOnlyCollection<ProductId> productIds = await retentionReadService
            .GetRetainedProductIdsAsync(sourceUserId, cancellationToken);
        if (productIds.Count == 0) {
            return;
        }

        IQueryable<Product> products = context.Products.Where(product => product.UserId == sourceUserId && productIds.Contains(product.Id));
        Guid[] retainedIds = [.. productIds.Select(id => id.Value)];
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "ProductImages" WHERE "ProductId" IN
            (SELECT "Id" FROM "Products" WHERE "UserId" = {sourceUserId.Value} AND "Id" = ANY({retainedIds}))
            """, cancellationToken);
        await products.ExecuteUpdateAsync(setters => setters
            .SetProperty(product => product.Name, "Retained product")
            .SetProperty(product => product.Brand, (string?)null)
            .SetProperty(product => product.Category, (string?)null)
            .SetProperty(product => product.Barcode, (string?)null)
            .SetProperty(product => product.Description, (string?)null)
            .SetProperty(product => product.Comment, (string?)null)
            .SetProperty(product => product.ImageUrl, (string?)null)
            .SetProperty(product => product.ImageAssetId, (ImageAssetId?)null)
            .SetProperty(product => product.UserId, targetUserId), cancellationToken);
    }
}

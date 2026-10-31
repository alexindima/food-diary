using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
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
    IProductUserPurgeRetentionReadService retentionReadService,
    IImageAssetOwnershipService imageAssetOwnershipService) : IUserContentRetentionParticipant {
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
        List<ImageAssetId> imageIds = await products.Where(product => product.ImageAssetId != null)
            .Select(product => product.ImageAssetId!.Value).ToListAsync(cancellationToken);
        imageIds.AddRange(await products.SelectMany(product => product.Images).Select(image => image.ImageAssetId)
            .ToListAsync(cancellationToken));

        await imageAssetOwnershipService.ReassignAsync([.. imageIds.Distinct()], sourceUserId, targetUserId, cancellationToken);
        await products.ExecuteUpdateAsync(setters => setters.SetProperty(product => product.UserId, targetUserId), cancellationToken);
    }
}

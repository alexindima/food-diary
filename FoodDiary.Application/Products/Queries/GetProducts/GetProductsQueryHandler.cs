using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler(
    IProductRepository productRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetProductsQuery, Result<PagedResponse<ProductModel>>> {
    public async Task<Result<PagedResponse<ProductModel>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<ProductModel>>(Errors.Authentication.InvalidToken);
        }

        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);
        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<PagedResponse<ProductModel>>(accessError);
        }
        ProductType[]? productTypes = query.ProductTypes?
            .Select(type => Enum.TryParse<ProductType>(type, true, out ProductType parsed) ? parsed : (ProductType?)null)
            .OfType<ProductType>()
            .Distinct()
            .ToArray();

        (IReadOnlyList<(Domain.Entities.Products.Product Product, int UsageCount)>? items, int totalItems) = await productRepository.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            query.Search,
            productTypes is { Length: > 0 } ? productTypes : null,
            cancellationToken).ConfigureAwait(false);

        var productsWithUsage = items.Select(item => new {
            item.Product,
            item.UsageCount,
            IsOwner = item.Product.UserId == userId,
        }).ToList();

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<ProductModel>(
            productsWithUsage.Select(p => p.Product.ToModel(p.UsageCount, p.IsOwner)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}

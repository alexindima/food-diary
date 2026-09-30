using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Queries.GetPublicProduct;

public sealed record GetPublicProductQuery(ProductId ProductId) : IQuery<Result<PublicProductModel>>;

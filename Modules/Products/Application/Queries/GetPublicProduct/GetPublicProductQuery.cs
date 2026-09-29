using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Queries.GetPublicProduct;

public sealed record GetPublicProductQuery(Guid ProductId) : IQuery<Result<PublicProductModel>>;

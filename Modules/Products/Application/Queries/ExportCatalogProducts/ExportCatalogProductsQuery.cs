using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Queries.ExportCatalogProducts;

public sealed record ExportCatalogProductsQuery : IQuery<Result<IReadOnlyList<CatalogProductModel>>>;

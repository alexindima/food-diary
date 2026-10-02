using FoodDiary.Mediator;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Commands.ImportCatalogProduct;

public sealed record ImportCatalogProductCommand(Guid UserId, CatalogProductModel Product, bool Preview)
    : IRequest<Result<CatalogProductImportResult>>;

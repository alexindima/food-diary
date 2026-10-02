using FluentValidation;
using FoodDiary.Mediator;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Application.Commands.CreateProduct;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Commands.ImportCatalogProduct;

public sealed class ImportCatalogProductCommandHandler(
    IProductCatalogIdReadService repository,
    IValidator<CreateProductCommand> validator,
    ISender sender,
    IProductMutationTransactionRunner transactionRunner)
    : IRequestHandler<ImportCatalogProductCommand, Result<CatalogProductImportResult>> {
    public Task<Result<CatalogProductImportResult>> Handle(ImportCatalogProductCommand request, CancellationToken cancellationToken) =>
        request.Preview ? HandleCoreAsync(request, cancellationToken) : transactionRunner.ExecuteAsync(token => HandleCoreAsync(request, token), cancellationToken);

    private async Task<Result<CatalogProductImportResult>> HandleCoreAsync(ImportCatalogProductCommand request, CancellationToken cancellationToken) {
        CatalogProductModel item = request.Product;
        if (item.Id == Guid.Empty) {
            return Report(item.Id, "invalid", ["Id must not be empty."]);
        }
        bool? isPublic = await repository.CatalogIdIsPublicAsync(item.Id, cancellationToken).ConfigureAwait(false);
        if (isPublic is false) {
            return Report(item.Id, "invalid", ["This ID cannot be imported."]);
        }
        if (isPublic is true) {
            return Report(item.Id, "skipped", []);
        }
        var create = new CreateProductCommand(request.UserId, item.Barcode, item.Name, item.Brand, item.ProductType,
            item.Category, item.Description, Comment: null, item.ImageUrl, ImageAssetId: null, item.BaseUnit, item.BaseAmount,
            item.DefaultPortionAmount, item.CaloriesPerBase, item.ProteinsPerBase, item.FatsPerBase,
            item.CarbsPerBase, item.FiberPerBase, item.AlcoholPerBase, "Public") { CatalogImportId = item.Id };
        FluentValidation.Results.ValidationResult validation = await validator.ValidateAsync(create, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid) {
            return Report(item.Id, "invalid", validation.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}").ToArray());
        }
        if (request.Preview) {
            return Report(item.Id, "ready", []);
        }
        Result<ProductModel> created = await sender.Send(create, cancellationToken).ConfigureAwait(false);
        return created.IsSuccess ? Report(item.Id, "imported", []) : Result.Failure<CatalogProductImportResult>(created.Error);
    }

    private static Result<CatalogProductImportResult> Report(Guid id, string status, IReadOnlyList<string> errors) =>
        Result.Success(new CatalogProductImportResult(id, status, errors));
}

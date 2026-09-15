using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;

namespace FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchByBarcode;

public record SearchByBarcodeQuery(string Barcode) : IQuery<Result<OpenFoodFactsProductModel?>>;

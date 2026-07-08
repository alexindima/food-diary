using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;

public record SearchByBarcodeQuery(string Barcode) : IQuery<Result<OpenFoodFactsProductModel?>>;

using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;

public record SearchByBarcodeQuery(string Barcode) : IQuery<Result<OpenFoodFactsProductModel?>>;

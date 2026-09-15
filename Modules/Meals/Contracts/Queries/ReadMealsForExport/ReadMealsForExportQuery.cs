using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Queries.ReadMealsForExport;

public sealed record ReadMealsForExportQuery(UserId UserId, DateTime DateFrom, DateTime DateTo, int Limit) : IRequest<IReadOnlyList<MealProjectionReadModel>>;

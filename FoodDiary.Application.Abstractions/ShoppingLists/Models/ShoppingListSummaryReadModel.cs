namespace FoodDiary.Application.Abstractions.ShoppingLists.Models;

public sealed record ShoppingListSummaryReadModel(Guid Id, string Name, DateTime CreatedAt, int ItemsCount);

using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.ImportRecipe;

public sealed record ImportRecipeCommand(Guid? UserId, string? SourceUrl, string? Text, string RequestId)
    : ICommand<Result<RecipeImportDraftModel>>, IUserRequest;

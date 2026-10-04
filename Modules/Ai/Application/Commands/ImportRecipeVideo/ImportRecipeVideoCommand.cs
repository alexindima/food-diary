using System.Text.Json.Serialization;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.ImportRecipeVideo;

public sealed record ImportRecipeVideoCommand(Guid? UserId, [property: JsonIgnore] Stream? Video, string? SourceUrl, string? Text, string RequestId)
    : ICommand<Result<RecipeImportDraftModel>>, IUserRequest;

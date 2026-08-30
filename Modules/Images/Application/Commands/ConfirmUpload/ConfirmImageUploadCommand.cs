using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Images.Commands.ConfirmUpload;

public sealed record ConfirmImageUploadCommand(Guid UserId, Guid AssetId) : ICommand<Result<ConfirmImageUploadResult>>;

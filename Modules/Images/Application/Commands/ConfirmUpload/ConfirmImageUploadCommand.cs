using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Application.Commands.ConfirmUpload;

public sealed record ConfirmImageUploadCommand(Guid UserId, Guid AssetId) : ICommand<Result<ConfirmImageUploadResult>>;

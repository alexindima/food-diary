using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Images.Application.Commands.GetUploadUrl;

public sealed record GetImageUploadUrlCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSizeBytes) : ICommand<Result<GetImageUploadUrlResult>>;

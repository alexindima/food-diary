using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed record GetImageUploadUrlCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSizeBytes) : ICommand<Result<GetImageUploadUrlResult>>;

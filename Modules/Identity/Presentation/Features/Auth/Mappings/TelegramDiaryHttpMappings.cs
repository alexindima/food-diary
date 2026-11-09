using FoodDiary.Modules.Identity.Application.Authentication.Commands.TransferTelegramDiary;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.VerifyTelegramDiaryEmail;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;

internal static class TelegramDiaryHttpMappings {
    public static VerifyTelegramDiaryEmailCommand ToDiaryVerificationCommand(this VerifyEmailHttpRequest request, string binding) =>
        new(request.UserId, request.Token, binding);

    public static TransferTelegramDiaryCommand ToDiaryTransferCommand(this TransferTelegramDiaryHttpRequest request, Guid userId, string binding) =>
        new(userId, request.Ticket, binding, request.UseTelegramGoals);

    public static TelegramDiaryTransferHttpResponse ToDiaryTransferResponse(this TelegramDiaryPreview preview) =>
        new(new TelegramDiaryCountsHttpResponse(preview.Counts.Meals, preview.Counts.Recipes, preview.Counts.Products,
                preview.Counts.WaterEntries, preview.Counts.Measurements),
            ToGoalsResponse(preview.EmailGoals), ToGoalsResponse(preview.TelegramGoals), preview.CanUseTelegramGoals);

    private static TelegramDiaryGoalsHttpResponse ToGoalsResponse(TelegramDiaryGoals goals) =>
        new(goals.Calories, goals.Protein, goals.Fat, goals.Carbs, goals.Fiber, goals.Water, goals.Steps, goals.Weight, goals.Waist);
}

namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminUserCreationHttpResponse(
    AdminUserHttpResponse User,
    string TemporaryPassword,
    bool CredentialsEmailQueued);

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminUserCreationHttpResponse(
    AdminUserHttpResponse User,
    string TemporaryPassword,
    bool CredentialsEmailQueued);

namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminUserCreationHttpResponse(
    AdminUserHttpResponse User,
    string TemporaryPassword,
    bool CredentialsEmailQueued);

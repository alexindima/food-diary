using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Admin.Application.Models;

[ExcludeFromCodeCoverage]
public sealed record AdminUserCreationModel(
    AdminUserModel User,
    string TemporaryPassword,
    bool CredentialsEmailQueued);

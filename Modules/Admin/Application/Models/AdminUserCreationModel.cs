using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Application.Admin.Models;

[ExcludeFromCodeCoverage]
public sealed record AdminUserCreationModel(
    AdminUserModel User,
    string TemporaryPassword,
    bool CredentialsEmailQueued);

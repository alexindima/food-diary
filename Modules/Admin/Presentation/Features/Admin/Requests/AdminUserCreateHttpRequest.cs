using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record AdminUserCreateHttpRequest(
    string Email,
    string? FirstName,
    string? LastName,
    string? Language,
    string[] Roles,
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string? TemporaryPassword,
    bool GeneratePassword = true,
    bool IsEmailConfirmed = true,
    bool SendCredentialsEmail = true,
    bool RequirePasswordChange = true);

using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Identity.Contracts.Admin.Models;

[ExcludeFromCodeCoverage]
public sealed record EmailTemplateReadModel(
    Guid Id,
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);

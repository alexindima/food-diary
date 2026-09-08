using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminTemplateRevisions;

public sealed record GetAdminTemplateRevisionsQuery(string Key, string Locale, bool IsAiPrompt)
    : IQuery<Result<IReadOnlyList<AdminTemplateRevisionModel>>>;

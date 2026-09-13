using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminTemplateRevisions;

public sealed record GetAdminTemplateRevisionsQuery(string Key, string Locale, bool IsAiPrompt)
    : IQuery<Result<IReadOnlyList<AdminTemplateRevisionModel>>>;

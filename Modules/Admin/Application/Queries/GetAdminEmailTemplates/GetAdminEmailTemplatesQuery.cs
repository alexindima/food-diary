using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminEmailTemplates;

public sealed record GetAdminEmailTemplatesQuery
    : IQuery<Result<IReadOnlyList<AdminEmailTemplateModel>>>;

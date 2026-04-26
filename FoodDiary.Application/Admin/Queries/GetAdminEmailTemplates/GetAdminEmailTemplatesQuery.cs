using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;

public sealed record GetAdminEmailTemplatesQuery()
    : IQuery<Result<IReadOnlyList<AdminEmailTemplateModel>>>;

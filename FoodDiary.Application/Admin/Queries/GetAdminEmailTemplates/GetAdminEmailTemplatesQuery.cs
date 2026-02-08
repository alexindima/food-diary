using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Admin;

namespace FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;

public sealed record GetAdminEmailTemplatesQuery()
    : IQuery<Result<IReadOnlyList<AdminEmailTemplateResponse>>>;

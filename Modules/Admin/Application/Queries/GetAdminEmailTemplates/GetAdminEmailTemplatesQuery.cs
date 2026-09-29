using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminEmailTemplates;

public sealed record GetAdminEmailTemplatesQuery(int Page = 1, int Limit = 50)
    : IQuery<Result<IReadOnlyList<AdminEmailTemplateModel>>>;

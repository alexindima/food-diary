using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Email.Contracts.Email.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminOutgoingEmails;

public sealed record GetAdminOutgoingEmailsQuery(int Page = 1, int Limit = 50, string? Purpose = null, string? Status = null, string? Recipient = null, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, Guid? Id = null, string? CorrelationId = null) : IQuery<Result<OutgoingEmailJournalPage>>;

using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelaySuppressions;

public sealed class GetMailRelaySuppressionsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelaySuppressionsQuery, Result<MailRelayPage<MailRelaySuppressionEntry>>> {
    public async Task<Result<MailRelayPage<MailRelaySuppressionEntry>>> Handle(
        GetMailRelaySuppressionsQuery request,
        CancellationToken cancellationToken) {
        MailRelayPage<MailRelaySuppressionEntry> suppressions = await useCases.GetSuppressionsPageAsync(request.Email, request.Page, request.Limit, cancellationToken).ConfigureAwait(false);
        return Result<MailRelayPage<MailRelaySuppressionEntry>>.Success(suppressions);
    }
}

using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelaySuppressionsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelaySuppressionsQuery, Result<IReadOnlyList<MailRelaySuppressionEntry>>> {
    public async Task<Result<IReadOnlyList<MailRelaySuppressionEntry>>> Handle(
        GetMailRelaySuppressionsQuery request,
        CancellationToken cancellationToken) {
        IReadOnlyList<MailRelaySuppressionEntry> suppressions = await useCases.GetSuppressionsAsync(request.Email, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<MailRelaySuppressionEntry>>.Success(suppressions);
    }
}

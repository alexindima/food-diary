using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelaySuppressionsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelaySuppressionsQuery, Result<IReadOnlyList<MailRelaySuppressionEntry>>> {
    public async Task<Result<IReadOnlyList<MailRelaySuppressionEntry>>> Handle(
        GetMailRelaySuppressionsQuery query,
        CancellationToken cancellationToken) {
        var suppressions = await useCases.GetSuppressionsAsync(query.Email, cancellationToken);
        return Result<IReadOnlyList<MailRelaySuppressionEntry>>.Success(suppressions);
    }
}

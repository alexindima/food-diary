using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelaySuppressionsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelaySuppressionsQuery, IReadOnlyList<MailRelaySuppressionEntry>> {
    public Task<IReadOnlyList<MailRelaySuppressionEntry>> Handle(
        GetMailRelaySuppressionsQuery query,
        CancellationToken cancellationToken) {
        return useCases.GetSuppressionsAsync(query.Email, cancellationToken);
    }
}

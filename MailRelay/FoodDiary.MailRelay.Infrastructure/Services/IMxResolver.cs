namespace FoodDiary.MailRelay.Infrastructure.Services;

public interface IMxResolver {
    Task<IReadOnlyList<MxRecord>> ResolveAsync(string domain, CancellationToken cancellationToken);
}

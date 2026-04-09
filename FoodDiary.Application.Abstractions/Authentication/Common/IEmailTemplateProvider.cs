namespace FoodDiary.Application.Authentication.Common;

public interface IEmailTemplateProvider {
    Task<EmailTemplateContent?> GetActiveTemplateAsync(string key, string locale, CancellationToken cancellationToken = default);
}

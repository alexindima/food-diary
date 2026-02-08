using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IEmailTemplateProvider
{
    Task<EmailTemplateContent?> GetActiveTemplateAsync(string key, string locale, CancellationToken cancellationToken = default);
}

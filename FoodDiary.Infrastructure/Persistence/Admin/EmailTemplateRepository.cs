using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class EmailTemplateRepository(FoodDiaryDbContext context) : IEmailTemplateRepository {
    public async Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default) {
        return await context.EmailTemplates
            .AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EmailTemplate?> GetByKeyAsync(string key, string locale, CancellationToken cancellationToken = default) {
        return await context.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken).ConfigureAwait(false);
    }

    public async Task<EmailTemplate> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken = default) {
        EmailTemplate? existing = await context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken).ConfigureAwait(false);

        if (existing is null) {
            var template = EmailTemplate.Create(key, locale, subject, htmlBody, textBody, isActive);
            await context.EmailTemplates.AddAsync(template, cancellationToken).ConfigureAwait(false);
            return template;
        }

        existing.Update(subject, htmlBody, textBody, isActive);
        await Task.CompletedTask.ConfigureAwait(false);
        return existing;
    }
}

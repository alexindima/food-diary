using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class EmailTemplateRepository(FoodDiaryDbContext context) : IEmailTemplateRepository
{
    public async Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.EmailTemplates
            .AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailTemplate?> GetByKeyAsync(string key, string locale, CancellationToken cancellationToken = default)
    {
        return await context.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken);
    }

    public async Task<EmailTemplate> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken);

        if (existing is null)
        {
            var template = EmailTemplate.Create(key, locale, subject, htmlBody, textBody, isActive);
            await context.EmailTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return template;
        }

        existing.Update(subject, htmlBody, textBody, isActive);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}

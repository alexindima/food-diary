using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminRetentionReader(FoodDiaryDbContext context) : IAdminRetentionReader {
    public async Task<AdminRetentionReport> GetAsync(DateTime fromUtc, DateTime toUtc, DateTime asOfUtc, CancellationToken cancellationToken) {
        DateTime observedEnd = toUtc < asOfUtc ? toUtc : asOfUtc;
        IQueryable<User> users = context.Users.AsNoTracking().IgnoreQueryFilters()
            .Where(user => user.CreatedOnUtc >= fromUtc && user.CreatedOnUtc < observedEnd);
        IQueryable<Meal> meals = context.Meals.AsNoTracking().Where(meal => meal.CreatedOnUtc < asOfUtc);
        var cohorts = await users.AsNoTracking().GroupBy(user => user.CreatedOnUtc.Date).OrderBy(group => group.Key)
            .Select(group => new {
                Date = group.Key,
                Registered = group.Count(),
                Activated = group.Count(user => meals.AsNoTracking().Any(meal => meal.UserId == user.Id && meal.CreatedOnUtc >= user.CreatedOnUtc && meal.CreatedOnUtc < user.CreatedOnUtc.Date.AddDays(7))),
                Day1 = group.Count(user => meals.AsNoTracking().Any(meal => meal.UserId == user.Id && meal.CreatedOnUtc >= user.CreatedOnUtc.Date.AddDays(1) && meal.CreatedOnUtc < user.CreatedOnUtc.Date.AddDays(2))),
                Day7 = group.Count(user => meals.AsNoTracking().Any(meal => meal.UserId == user.Id && meal.CreatedOnUtc >= user.CreatedOnUtc.Date.AddDays(7) && meal.CreatedOnUtc < user.CreatedOnUtc.Date.AddDays(8))),
                Day30 = group.Count(user => meals.AsNoTracking().Any(meal => meal.UserId == user.Id && meal.CreatedOnUtc >= user.CreatedOnUtc.Date.AddDays(30) && meal.CreatedOnUtc < user.CreatedOnUtc.Date.AddDays(31))),
            }).ToListAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<Meal> activity = meals.AsNoTracking().Where(meal => meal.CreatedOnUtc >= fromUtc && meal.CreatedOnUtc < observedEnd);
        int active = await activity.AsNoTracking().Select(meal => meal.UserId).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
        List<AdminRetentionDay> days = await activity.AsNoTracking().GroupBy(meal => meal.CreatedOnUtc.Date).OrderBy(group => group.Key)
            .Select(group => new AdminRetentionDay(group.Key, group.Select(meal => meal.UserId).Distinct().Count()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new AdminRetentionReport(fromUtc, toUtc, asOfUtc, active, cohorts.ConvertAll(row => new AdminRetentionCohort(
            row.Date, row.Registered, Mature(row.Date, 7) ? row.Activated : null, Mature(row.Date, 2) ? row.Day1 : null,
            Mature(row.Date, 8) ? row.Day7 : null, Mature(row.Date, 31) ? row.Day30 : null)), days);

        bool Mature(DateTime date, int elapsedDays) => date.AddDays(elapsedDays) <= asOfUtc.Date;
    }
}

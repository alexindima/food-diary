namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ProjectDependencyMatrixTests {
    private static readonly IReadOnlyDictionary<string, string[]> AllowedProductionProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["FoodDiary.Analyzers"] = [],
            ["FoodDiary.Development.Mcp"] = [],
            ["FoodDiary.Application.Runtime"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Admin"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Ai",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.ContentReports.Contracts",
                "FoodDiary.Modules.Gamification.Application",
                "FoodDiary.Modules.Lessons.Contracts",
            ],
            ["FoodDiary.Application.Billing"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Ai"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.BodyMetrics"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Cycles"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Cycles.Application.Abstractions",
                "FoodDiary.Modules.Cycles.Domain",
            ],
            ["FoodDiary.Modules.Cycles.Application.Abstractions"] = [
                "FoodDiary.Modules.Cycles.Domain",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Modules.Cycles.Domain"] = ["FoodDiary.Domain"],
            ["FoodDiary.Modules.Cycles.Infrastructure"] = [
                "FoodDiary.Application.Cycles",
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.Cycles.PersistenceModel",
            ],
            ["FoodDiary.Modules.Cycles.PersistenceModel"] = ["FoodDiary.Modules.Cycles.Domain"],
            ["FoodDiary.Application.Dashboard"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Cycles",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Modules.Hydration.Contracts",
                "FoodDiary.Application.Meals",
                "FoodDiary.Application.Statistics",
                "FoodDiary.Modules.Tdee.Application",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.DailyAdvices.Application",
                "FoodDiary.Modules.Dietologist.Application.Abstractions",
                "FoodDiary.Modules.Fasting.Contracts",
            ],
            ["FoodDiary.Modules.ContentReports.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.ContentReports.Application.Abstractions",
                "FoodDiary.Modules.ContentReports.Contracts",
                "FoodDiary.Modules.ContentReports.Domain",
            ],
            ["FoodDiary.Modules.ContentReports.Application.Abstractions"] = [
                "FoodDiary.Modules.ContentReports.Contracts",
                "FoodDiary.Modules.ContentReports.Domain",
            ],
            ["FoodDiary.Modules.ContentReports.Contracts"] = [
                "FoodDiary.Modules.ContentReports.Domain",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Modules.ContentReports.Domain"] = ["FoodDiary.Domain"],
            ["FoodDiary.Modules.ContentReports.Infrastructure"] = [
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.ContentReports.Application",
                "FoodDiary.Modules.ContentReports.PersistenceModel",
            ],
            ["FoodDiary.Modules.ContentReports.PersistenceModel"] = ["FoodDiary.Modules.ContentReports.Domain"],
            ["FoodDiary.Modules.DailyAdvices.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.DailyAdvices.Application.Abstractions",
                "FoodDiary.Modules.DailyAdvices.Domain",
            ],
            ["FoodDiary.Modules.DailyAdvices.Application.Abstractions"] = [],
            ["FoodDiary.Modules.DailyAdvices.Domain"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.DailyAdvices.Infrastructure"] = [
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.DailyAdvices.Application",
                "FoodDiary.Modules.DailyAdvices.PersistenceModel",
            ],
            ["FoodDiary.Modules.DailyAdvices.PersistenceModel"] = [
                "FoodDiary.Modules.DailyAdvices.Domain",
            ],
            ["FoodDiary.Modules.Hydration.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Hydration.Application.Abstractions",
                "FoodDiary.Modules.Hydration.Contracts",
            ],
            ["FoodDiary.Modules.Hydration.Application.Abstractions"] = [
                "FoodDiary.Domain",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Modules.Hydration.Contracts"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Hydration.Infrastructure"] = [
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.Hydration.Application",
                "FoodDiary.Modules.Hydration.PersistenceModel",
            ],
            ["FoodDiary.Modules.Hydration.PersistenceModel"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Lessons.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Gamification.Application.Abstractions",
                "FoodDiary.Modules.Lessons.Application.Abstractions",
                "FoodDiary.Modules.Lessons.Contracts",
                "FoodDiary.Modules.Lessons.Domain",
            ],
            ["FoodDiary.Modules.Lessons.Application.Abstractions"] = [
                "FoodDiary.Modules.Lessons.Contracts",
                "FoodDiary.Modules.Lessons.Domain",
            ],
            ["FoodDiary.Modules.Lessons.Contracts"] = [
                "FoodDiary.Modules.Lessons.Domain",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Modules.Lessons.Domain"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Lessons.Infrastructure"] = [
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.Lessons.Application",
                "FoodDiary.Modules.Lessons.PersistenceModel",
            ],
            ["FoodDiary.Modules.Lessons.PersistenceModel"] = [
                "FoodDiary.Modules.Lessons.Domain",
            ],
            ["FoodDiary.Modules.Dietologist.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Dietologist.Application.Abstractions",
                "FoodDiary.Modules.Dietologist.Domain",
            ],
            ["FoodDiary.Modules.Dietologist.Application.Abstractions"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Modules.Dietologist.Domain",
            ],
            ["FoodDiary.Modules.Dietologist.Domain"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Dietologist.Infrastructure"] = [
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.Dietologist.Application",
                "FoodDiary.Modules.Dietologist.PersistenceModel",
            ],
            ["FoodDiary.Modules.Dietologist.PersistenceModel"] = [
                "FoodDiary.Modules.Dietologist.Domain",
            ],
            ["FoodDiary.Application.Exercises"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Export"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Cycles",
                "FoodDiary.Application.Meals",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Modules.Fasting.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Fasting.Application.Abstractions",
                "FoodDiary.Modules.Fasting.Contracts",
                "FoodDiary.Modules.Fasting.Domain",
            ],
            ["FoodDiary.Modules.Fasting.Application.Abstractions"] = [
                "FoodDiary.Modules.Fasting.Domain",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Modules.Fasting.Contracts"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Fasting.Domain"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Fasting.Infrastructure"] = [
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.Fasting.Application",
                "FoodDiary.Modules.Fasting.PersistenceModel",
            ],
            ["FoodDiary.Modules.Fasting.PersistenceModel"] = [
                "FoodDiary.Modules.Fasting.Domain",
            ],
            ["FoodDiary.Application.Favorites"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Favorites.Domain",
            ],
            ["FoodDiary.Modules.Favorites.Domain"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Favorites.Infrastructure"] = [
                "FoodDiary.Application.Favorites",
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.Favorites.PersistenceModel",
            ],
            ["FoodDiary.Modules.Favorites.PersistenceModel"] = [
                "FoodDiary.Modules.Favorites.Domain",
            ],
            ["FoodDiary.Modules.Gamification.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Gamification.Application.Abstractions",
                "FoodDiary.Modules.Gamification.Domain",
            ],
            ["FoodDiary.Modules.Gamification.Application.Abstractions"] = [
                "FoodDiary.Domain",
                "FoodDiary.Modules.Gamification.Domain",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Modules.Gamification.Domain"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Gamification.PersistenceModel"] = [
                "FoodDiary.Modules.Gamification.Domain",
            ],
            ["FoodDiary.Modules.Gamification.Infrastructure"] = [
                "FoodDiary.Modules.Gamification.Application",
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.Gamification.Application.Abstractions",
                "FoodDiary.Modules.Gamification.PersistenceModel",
            ],
            ["FoodDiary.Application.Wearables"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Identity"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Images"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Images.Application.Abstractions",
            ],
            ["FoodDiary.Modules.Images.Application.Abstractions"] = [
                "FoodDiary.Domain",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Modules.Images.PersistenceModel"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.Images.Infrastructure"] = [
                "FoodDiary.Application.Images",
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.Images.PersistenceModel",
            ],
            ["FoodDiary.Application.MealPlanning"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Meals"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Images",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Gamification.Application.Abstractions",
            ],
            ["FoodDiary.Modules.WeeklyGoals.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.WeeklyGoals.Application.Abstractions",
                "FoodDiary.Modules.WeeklyGoals.Contracts",
                "FoodDiary.Modules.WeeklyGoals.Domain",
            ],
            ["FoodDiary.Modules.WeeklyGoals.Application.Abstractions"] = [
                "FoodDiary.Modules.WeeklyGoals.Domain",
            ],
            ["FoodDiary.Modules.WeeklyGoals.Contracts"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.WeeklyGoals.Domain"] = [
                "FoodDiary.Domain",
            ],
            ["FoodDiary.Modules.WeeklyGoals.Infrastructure"] = [
                "FoodDiary.Modules.WeeklyGoals.Application",
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.WeeklyGoals.PersistenceModel",
            ],
            ["FoodDiary.Modules.WeeklyGoals.PersistenceModel"] = [
                "FoodDiary.Modules.WeeklyGoals.Domain",
            ],
            ["FoodDiary.Application.Usda"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Meals",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Modules.WeeklyCheckIn.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Modules.Hydration.Contracts",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.RecipeCommunity"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Modules.Tdee.Application"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Statistics"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Marketing"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Notifications"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.OpenFoodFacts"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Products"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Images",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.Usda",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Recipes"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Images",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Users"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Application.Abstractions"] = [
                "FoodDiary.Domain",
                "FoodDiary.Domain.Primitives",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.Favorites.Domain",
                "FoodDiary.Modules.Cycles.Application.Abstractions",
                "FoodDiary.Modules.Images.Application.Abstractions",
                "FoodDiary.Results",
            ],
            ["FoodDiary.Domain"] = [
                "FoodDiary.Domain.Primitives",
            ],
            ["FoodDiary.Infrastructure"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Modules.ContentReports.Domain",
                "FoodDiary.Modules.ContentReports.PersistenceModel",
                "FoodDiary.Modules.Cycles.Domain",
                "FoodDiary.Modules.Cycles.PersistenceModel",
                "FoodDiary.Modules.DailyAdvices.Domain",
                "FoodDiary.Modules.DailyAdvices.PersistenceModel",
                "FoodDiary.Modules.Dietologist.Domain",
                "FoodDiary.Modules.Dietologist.PersistenceModel",
                "FoodDiary.Modules.Fasting.Domain",
                "FoodDiary.Modules.Fasting.PersistenceModel",
                "FoodDiary.Modules.Favorites.Domain",
                "FoodDiary.Modules.Favorites.PersistenceModel",
                "FoodDiary.Modules.Hydration.PersistenceModel",
                "FoodDiary.Modules.Images.PersistenceModel",
                "FoodDiary.Modules.Gamification.Application.Abstractions",
                "FoodDiary.Modules.Gamification.Domain",
                "FoodDiary.Modules.Gamification.PersistenceModel",
                "FoodDiary.Modules.Lessons.Domain",
                "FoodDiary.Modules.Lessons.PersistenceModel",
                "FoodDiary.Modules.WeeklyGoals.Domain",
                "FoodDiary.Modules.WeeklyGoals.PersistenceModel",
            ],
            ["FoodDiary.Initializer"] = [
                "FoodDiary.Application.Runtime",
                "FoodDiary.Application.Admin",
                "FoodDiary.Application.Ai",
                "FoodDiary.Application.Billing",
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Modules.Cycles.Infrastructure",
                "FoodDiary.Application.Dashboard",
                "FoodDiary.Modules.Dietologist.Infrastructure",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Application.Export",
                "FoodDiary.Modules.Gamification.Infrastructure",
                "FoodDiary.Modules.Fasting.Infrastructure",
                "FoodDiary.Modules.Favorites.Infrastructure",
                "FoodDiary.Modules.Hydration.Infrastructure",
                "FoodDiary.Modules.Images.Infrastructure",
                "FoodDiary.Application.Identity",
                "FoodDiary.Application.Images",
                "FoodDiary.Modules.Lessons.Infrastructure",
                "FoodDiary.Application.Marketing",
                "FoodDiary.Application.MealPlanning",
                "FoodDiary.Application.Meals",
                "FoodDiary.Modules.WeeklyGoals.Infrastructure",
                "FoodDiary.Application.Usda",
                "FoodDiary.Modules.WeeklyCheckIn.Application",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.Products",
                "FoodDiary.Application.Recipes",
                "FoodDiary.Application.RecipeCommunity",
                "FoodDiary.Application.Statistics",
                "FoodDiary.Modules.Tdee.Application",
                "FoodDiary.Application.Users",
                "FoodDiary.Application.Wearables",
                "FoodDiary.Infrastructure",
                "FoodDiary.Modules.ContentReports.Infrastructure",
                "FoodDiary.Modules.DailyAdvices.Infrastructure",
            ],
            ["FoodDiary.Integrations"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Domain",
                "FoodDiary.MailInbox.Client",
                "FoodDiary.MailRelay.Client",
            ],
            ["FoodDiary.JobManager"] = [
                "FoodDiary.Application.Runtime",
                "FoodDiary.Application.Admin",
                "FoodDiary.Application.Ai",
                "FoodDiary.Application.Billing",
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Application.Dashboard",
                "FoodDiary.Modules.Dietologist.Infrastructure",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Application.Export",
                "FoodDiary.Modules.Gamification.Infrastructure",
                "FoodDiary.Modules.Fasting.Contracts",
                "FoodDiary.Modules.Fasting.Infrastructure",
                "FoodDiary.Modules.Images.Infrastructure",
                "FoodDiary.Modules.Favorites.Infrastructure",
                "FoodDiary.Application.Identity",
                "FoodDiary.Application.Images",
                "FoodDiary.Application.Marketing",
                "FoodDiary.Application.MealPlanning",
                "FoodDiary.Application.Meals",
                "FoodDiary.Modules.WeeklyGoals.Application",
                "FoodDiary.Modules.WeeklyGoals.Infrastructure",
                "FoodDiary.Application.Usda",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.Products",
                "FoodDiary.Application.Recipes",
                "FoodDiary.Application.RecipeCommunity",
                "FoodDiary.Application.Statistics",
                "FoodDiary.Modules.Tdee.Application",
                "FoodDiary.Application.Users",
                "FoodDiary.Application.Wearables",
                "FoodDiary.Infrastructure",
                "FoodDiary.Integrations",
                "FoodDiary.Modules.DailyAdvices.Application",
                "FoodDiary.Resources",
            ],
            ["FoodDiary.MailInbox.Application"] = [
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Results",
            ],
            ["FoodDiary.MailInbox.Client"] = [],
            ["FoodDiary.MailInbox.Domain"] = [
                "FoodDiary.Domain.Primitives",
            ],
            ["FoodDiary.MailInbox.Infrastructure"] = [
                "FoodDiary.MailInbox.Application",
            ],
            ["FoodDiary.MailInbox.Initializer"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Infrastructure",
            ],
            ["FoodDiary.MailInbox.Presentation"] = [
                "FoodDiary.MailInbox.Application",
            ],
            ["FoodDiary.MailInbox.WebApi"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Infrastructure",
                "FoodDiary.MailInbox.Presentation",
            ],
            ["FoodDiary.MailRelay.Application"] = [
                "FoodDiary.MailRelay.Domain",
                "FoodDiary.Mediator",
                "FoodDiary.Results",
            ],
            ["FoodDiary.MailRelay.Client"] = [],
            ["FoodDiary.MailRelay.Domain"] = [
                "FoodDiary.Domain.Primitives",
            ],
            ["FoodDiary.MailRelay.Infrastructure"] = [
                "FoodDiary.MailRelay.Application",
            ],
            ["FoodDiary.MailRelay.Initializer"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Infrastructure",
            ],
            ["FoodDiary.MailRelay.Presentation"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Client",
            ],
            ["FoodDiary.MailRelay.WebApi"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Infrastructure",
                "FoodDiary.MailRelay.Presentation",
            ],
            ["FoodDiary.Mediator"] = [],
            ["FoodDiary.Presentation.Api"] = [
                "FoodDiary.Application.Abstractions",
                "FoodDiary.Application.Admin",
                "FoodDiary.Application.Ai",
                "FoodDiary.Application.Billing",
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Modules.ContentReports.Application",
                "FoodDiary.Application.Cycles",
                "FoodDiary.Application.Dashboard",
                "FoodDiary.Modules.Dietologist.Application",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Application.Export",
                "FoodDiary.Modules.Gamification.Application",
                "FoodDiary.Modules.Fasting.Application",
                "FoodDiary.Modules.Fasting.Contracts",
                "FoodDiary.Application.Favorites",
                "FoodDiary.Modules.Hydration.Application",
                "FoodDiary.Application.Identity",
                "FoodDiary.Application.Images",
                "FoodDiary.Modules.Lessons.Application",
                "FoodDiary.Application.Marketing",
                "FoodDiary.Application.MealPlanning",
                "FoodDiary.Application.Meals",
                "FoodDiary.Modules.WeeklyGoals.Application",
                "FoodDiary.Application.Usda",
                "FoodDiary.Modules.WeeklyCheckIn.Application",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.Products",
                "FoodDiary.Application.Recipes",
                "FoodDiary.Application.RecipeCommunity",
                "FoodDiary.Application.Statistics",
                "FoodDiary.Modules.Tdee.Application",
                "FoodDiary.Application.Users",
                "FoodDiary.Application.Wearables",
                "FoodDiary.Mediator",
                "FoodDiary.Results",
                "FoodDiary.Modules.DailyAdvices.Application",
            ],
            ["FoodDiary.Domain.Primitives"] = [],
            ["FoodDiary.Resources"] = [
                "FoodDiary.Application.Abstractions",
            ],
            ["FoodDiary.Results"] = [],
            ["FoodDiary.Telegram.Bot"] = [],
            ["FoodDiary.Web.Api"] = [
                "FoodDiary.Application.Runtime",
                "FoodDiary.Application.Admin",
                "FoodDiary.Application.Ai",
                "FoodDiary.Application.Billing",
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Modules.ContentReports.Infrastructure",
                "FoodDiary.Modules.Cycles.Infrastructure",
                "FoodDiary.Application.Dashboard",
                "FoodDiary.Modules.Dietologist.Infrastructure",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Application.Export",
                "FoodDiary.Modules.Gamification.Infrastructure",
                "FoodDiary.Modules.Fasting.Infrastructure",
                "FoodDiary.Modules.Favorites.Infrastructure",
                "FoodDiary.Modules.Hydration.Infrastructure",
                "FoodDiary.Modules.Images.Infrastructure",
                "FoodDiary.Application.Identity",
                "FoodDiary.Application.Images",
                "FoodDiary.Modules.Lessons.Infrastructure",
                "FoodDiary.Application.Marketing",
                "FoodDiary.Application.MealPlanning",
                "FoodDiary.Application.Meals",
                "FoodDiary.Modules.WeeklyGoals.Infrastructure",
                "FoodDiary.Application.Usda",
                "FoodDiary.Modules.WeeklyCheckIn.Application",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.Products",
                "FoodDiary.Application.Recipes",
                "FoodDiary.Application.RecipeCommunity",
                "FoodDiary.Application.Statistics",
                "FoodDiary.Modules.Tdee.Application",
                "FoodDiary.Application.Users",
                "FoodDiary.Application.Wearables",
                "FoodDiary.Infrastructure",
                "FoodDiary.Integrations",
                "FoodDiary.Presentation.Api",
                "FoodDiary.Resources",
                "FoodDiary.Modules.DailyAdvices.Infrastructure",
            ],
        };

    private static readonly IReadOnlyDictionary<string, string[]> AllowedTestProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["FoodDiary.Analyzers.Tests"] = [
                "FoodDiary.Analyzers",
            ],
            ["FoodDiary.Development.Mcp.Tests"] = [
                "FoodDiary.Development.Mcp",
            ],
            ["FoodDiary.Application.Tests"] = [
                "FoodDiary.Modules.ContentReports.Application",
                "FoodDiary.Modules.ContentReports.Application.Abstractions",
                "FoodDiary.Application.Runtime",
                "FoodDiary.Application.Admin",
                "FoodDiary.Application.Ai",
                "FoodDiary.Application.Billing",
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Application.Dashboard",
                "FoodDiary.Modules.Dietologist.Application",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Application.Export",
                "FoodDiary.Modules.Fasting.Contracts",
                "FoodDiary.Application.Favorites",
                "FoodDiary.Modules.Hydration.Application",
                "FoodDiary.Modules.Hydration.Infrastructure",
                "FoodDiary.Application.Identity",
                "FoodDiary.Modules.Lessons.Application",
                "FoodDiary.Modules.Lessons.Application.Abstractions",
                "FoodDiary.Modules.Lessons.Contracts",
                "FoodDiary.Application.Marketing",
                "FoodDiary.Application.MealPlanning",
                "FoodDiary.Application.Meals",
                "FoodDiary.Application.Usda",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.Products",
                "FoodDiary.Application.Recipes",
                "FoodDiary.Application.RecipeCommunity",
                "FoodDiary.Application.Statistics",
                "FoodDiary.Application.Users",
                "FoodDiary.Application.Wearables",
                "FoodDiary.Domain",
            ],
            ["FoodDiary.ArchitectureTests"] = [
                "FoodDiary.Domain",
                "FoodDiary.Infrastructure",
            ],
            ["FoodDiary.Domain.Primitives.Tests"] = [
                "FoodDiary.Domain.Primitives",
            ],
            ["FoodDiary.Domain.Tests"] = [
                "FoodDiary.Modules.ContentReports.Domain",
                "FoodDiary.Domain",
                "FoodDiary.Modules.Cycles.Domain",
                "FoodDiary.Modules.Dietologist.Domain",
                "FoodDiary.Modules.Fasting.Domain",
                "FoodDiary.Modules.Favorites.Domain",
                "FoodDiary.Modules.Gamification.Domain",
            ],
            ["FoodDiary.Modules.Favorites.Application.Tests"] = [
                "FoodDiary.Application.Favorites",
                "FoodDiary.Application.Recipes",
            ],
            ["FoodDiary.Modules.Favorites.Domain.Tests"] = [
                "FoodDiary.Modules.Favorites.Domain",
            ],
            ["FoodDiary.Modules.Gamification.Application.Tests"] = [
                "FoodDiary.Application.Admin",
                "FoodDiary.Application.Meals",
                "FoodDiary.Domain",
                "FoodDiary.Modules.Gamification.Application",
                "FoodDiary.Modules.Gamification.Application.Abstractions",
            ],
            ["FoodDiary.Modules.Gamification.Domain.Tests"] = [
                "FoodDiary.Modules.Gamification.Domain",
            ],
            ["FoodDiary.Modules.Gamification.Infrastructure.Tests"] = [
                "FoodDiary.Modules.Gamification.Infrastructure",
            ],
            ["FoodDiary.Modules.DailyAdvices.Application.Tests"] = [
                "FoodDiary.Application.Users",
                "FoodDiary.Modules.DailyAdvices.Application",
            ],
            ["FoodDiary.Modules.Gamification.Application.Tests"] = [
                "FoodDiary.Application.Admin",
                "FoodDiary.Application.Meals",
                "FoodDiary.Domain",
                "FoodDiary.Modules.Gamification.Application",
                "FoodDiary.Modules.Gamification.Application.Abstractions",
            ],
            ["FoodDiary.Modules.Gamification.Domain.Tests"] = [
                "FoodDiary.Modules.Gamification.Domain",
            ],
            ["FoodDiary.Modules.Gamification.Infrastructure.Tests"] = [
                "FoodDiary.Modules.Gamification.Infrastructure",
            ],
            ["FoodDiary.Modules.Cycles.Application.Tests"] = [
                "FoodDiary.Application.Cycles",
            ],
            ["FoodDiary.Modules.Cycles.Domain.Tests"] = [
                "FoodDiary.Modules.Cycles.Domain",
            ],
            ["FoodDiary.Modules.Cycles.Infrastructure.Tests"] = [
                "FoodDiary.Modules.Cycles.Infrastructure",
            ],
            ["FoodDiary.Modules.Cycles.Infrastructure.IntegrationTests"] = [
                "FoodDiary.Initializer",
                "FoodDiary.Modules.Cycles.Infrastructure",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.Modules.Dietologist.Application.Tests"] = [
                "FoodDiary.Application.Dashboard",
                "FoodDiary.Application.Meals",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.Users",
                "FoodDiary.Modules.Dietologist.Application",
                "FoodDiary.Modules.Hydration.Contracts",
            ],
            ["FoodDiary.Modules.Dietologist.Domain.Tests"] = [
                "FoodDiary.Modules.Dietologist.Domain",
            ],
            ["FoodDiary.Modules.Dietologist.Infrastructure.Tests"] = [
                "FoodDiary.Modules.Dietologist.Infrastructure",
            ],
            ["FoodDiary.Modules.DailyAdvices.Domain.Tests"] = [
                "FoodDiary.Modules.DailyAdvices.Domain",
            ],
            ["FoodDiary.Modules.DailyAdvices.Infrastructure.Tests"] = [
                "FoodDiary.Modules.DailyAdvices.Infrastructure",
            ],
            ["FoodDiary.Modules.Fasting.Application.Tests"] = [
                "FoodDiary.Application.Notifications",
                "FoodDiary.Modules.Fasting.Application",
            ],
            ["FoodDiary.Modules.Fasting.Domain.Tests"] = [
                "FoodDiary.Modules.Fasting.Domain",
            ],
            ["FoodDiary.Modules.Fasting.Infrastructure.Tests"] = [
                "FoodDiary.Initializer",
                "FoodDiary.Modules.Fasting.Infrastructure",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.Modules.Hydration.Application.Tests"] = [
                "FoodDiary.Modules.Hydration.Application",
            ],
            ["FoodDiary.Modules.Hydration.Infrastructure.Tests"] = [
                "FoodDiary.Initializer",
                "FoodDiary.Modules.Hydration.Infrastructure",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.Modules.Images.Application.Tests"] = [
                "FoodDiary.Application.Images",
                "FoodDiary.Application.Users",
            ],
            ["FoodDiary.Modules.Lessons.Application.Tests"] = [
                "FoodDiary.Modules.Lessons.Application",
            ],
            ["FoodDiary.Modules.Lessons.Domain.Tests"] = [
                "FoodDiary.Modules.Lessons.Domain",
            ],
            ["FoodDiary.Modules.ContentReports.Application.Tests"] = [
                "FoodDiary.Modules.ContentReports.Application",
            ],
            ["FoodDiary.Modules.Tdee.Application.Tests"] = [
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Modules.Tdee.Application",
            ],
            ["FoodDiary.Modules.WeeklyGoals.Application.Tests"] = [
                "FoodDiary.Application.Users",
                "FoodDiary.Modules.WeeklyGoals.Application",
            ],
            ["FoodDiary.Modules.WeeklyGoals.Domain.Tests"] = [
                "FoodDiary.Modules.WeeklyGoals.Domain",
            ],
            ["FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests"] = [
                "FoodDiary.Initializer",
                "FoodDiary.Modules.WeeklyGoals.Infrastructure",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.Modules.WeeklyCheckIn.Application.Tests"] = [
                "FoodDiary.Application.Users",
                "FoodDiary.Modules.WeeklyCheckIn.Application",
            ],
            ["FoodDiary.Infrastructure.IntegrationTests"] = [
                "FoodDiary.Modules.ContentReports.Infrastructure",
                "FoodDiary.Infrastructure",
                "FoodDiary.Initializer",
                "FoodDiary.Integrations",
                "FoodDiary.Modules.DailyAdvices.Infrastructure",
                "FoodDiary.Modules.Dietologist.Infrastructure",
                "FoodDiary.Modules.Fasting.Infrastructure",
                "FoodDiary.Modules.Favorites.Infrastructure",
                "FoodDiary.Modules.Gamification.Infrastructure",
                "FoodDiary.Modules.Hydration.Infrastructure",
                "FoodDiary.Modules.Lessons.Application.Abstractions",
                "FoodDiary.Modules.Lessons.Contracts",
                "FoodDiary.Modules.Lessons.Infrastructure",
                "FoodDiary.Modules.WeeklyGoals.Infrastructure",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.Infrastructure.Tests"] = [
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Modules.Cycles.Infrastructure",
                "FoodDiary.Modules.Dietologist.Infrastructure",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Modules.Hydration.Infrastructure",
                "FoodDiary.Application.Identity",
                "FoodDiary.Application.MealPlanning",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.RecipeCommunity",
                "FoodDiary.Modules.Tdee.Application",
                "FoodDiary.Infrastructure",
                "FoodDiary.Initializer",
                "FoodDiary.Integrations",
                "FoodDiary.Modules.Fasting.Infrastructure",
            ],
            ["FoodDiary.JobManager.Tests"] = [
                "FoodDiary.Application.Billing",
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Modules.Dietologist.Infrastructure",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Modules.Fasting.Application",
                "FoodDiary.Modules.Fasting.Contracts",
                "FoodDiary.Modules.Favorites.Infrastructure",
                "FoodDiary.Application.Identity",
                "FoodDiary.Application.Images",
                "FoodDiary.Application.MealPlanning",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.RecipeCommunity",
                "FoodDiary.Modules.Tdee.Application",
                "FoodDiary.Modules.WeeklyGoals.Application",
                "FoodDiary.Modules.WeeklyGoals.Infrastructure",
                "FoodDiary.JobManager",
            ],
            ["FoodDiary.MailInbox.Application.Tests"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Domain",
            ],
            ["FoodDiary.MailInbox.Client.Tests"] = [
                "FoodDiary.MailInbox.Client",
            ],
            ["FoodDiary.MailInbox.Domain.Tests"] = [
                "FoodDiary.MailInbox.Domain",
            ],
            ["FoodDiary.MailInbox.Infrastructure.Tests"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.MailInbox.Infrastructure",
            ],
            ["FoodDiary.MailInbox.Initializer.Tests"] = [
                "FoodDiary.MailInbox.Initializer",
            ],
            ["FoodDiary.MailInbox.IntegrationTests"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.MailInbox.Infrastructure",
                "FoodDiary.MailInbox.WebApi",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.MailInbox.Presentation.Tests"] = [
                "FoodDiary.MailInbox.Application",
                "FoodDiary.MailInbox.Domain",
                "FoodDiary.MailInbox.Presentation",
            ],
            ["FoodDiary.MailRelay.Application.Tests"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Domain",
            ],
            ["FoodDiary.MailRelay.Client.Tests"] = [
                "FoodDiary.MailRelay.Client",
            ],
            ["FoodDiary.MailRelay.Domain.Tests"] = [
                "FoodDiary.MailRelay.Domain",
            ],
            ["FoodDiary.MailRelay.Infrastructure.Tests"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Client",
                "FoodDiary.MailRelay.Domain",
                "FoodDiary.MailRelay.Infrastructure",
            ],
            ["FoodDiary.MailRelay.Initializer.Tests"] = [
                "FoodDiary.MailRelay.Initializer",
            ],
            ["FoodDiary.MailRelay.IntegrationTests"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Domain",
                "FoodDiary.MailRelay.Infrastructure",
                "FoodDiary.MailRelay.WebApi",
                "FoodDiary.Testing",
            ],
            ["FoodDiary.MailRelay.Presentation.Tests"] = [
                "FoodDiary.MailRelay.Application",
                "FoodDiary.MailRelay.Client",
                "FoodDiary.MailRelay.Domain",
                "FoodDiary.MailRelay.Presentation",
            ],
            ["FoodDiary.Mediator.Tests"] = [
                "FoodDiary.Mediator",
            ],
            ["FoodDiary.Presentation.Api.Tests"] = [
                "FoodDiary.Application.Admin",
                "FoodDiary.Application.Ai",
                "FoodDiary.Application.Billing",
                "FoodDiary.Application.BodyMetrics",
                "FoodDiary.Modules.ContentReports.Application",
                "FoodDiary.Application.Cycles",
                "FoodDiary.Application.Dashboard",
                "FoodDiary.Modules.Dietologist.Application",
                "FoodDiary.Application.Exercises",
                "FoodDiary.Application.Export",
                "FoodDiary.Modules.Gamification.Application",
                "FoodDiary.Modules.Fasting.Application",
                "FoodDiary.Modules.Fasting.Contracts",
                "FoodDiary.Application.Favorites",
                "FoodDiary.Modules.Hydration.Application",
                "FoodDiary.Application.Identity",
                "FoodDiary.Application.Images",
                "FoodDiary.Modules.Lessons.Application",
                "FoodDiary.Application.MealPlanning",
                "FoodDiary.Application.Meals",
                "FoodDiary.Modules.WeeklyGoals.Application",
                "FoodDiary.Application.Usda",
                "FoodDiary.Modules.WeeklyCheckIn.Application",
                "FoodDiary.Application.Notifications",
                "FoodDiary.Application.OpenFoodFacts",
                "FoodDiary.Application.RecipeCommunity",
                "FoodDiary.Application.Statistics",
                "FoodDiary.Modules.Tdee.Application",
                "FoodDiary.Application.Wearables",
                "FoodDiary.Domain",
                "FoodDiary.Modules.DailyAdvices.Application",
                "FoodDiary.Presentation.Api",
            ],
            ["FoodDiary.Resources.Tests"] = [
                "FoodDiary.Resources",
            ],
            ["FoodDiary.Results.Tests"] = [
                "FoodDiary.Results",
            ],
            ["FoodDiary.Telegram.Bot.Tests"] = [
                "FoodDiary.Telegram.Bot",
            ],
            ["FoodDiary.Testing"] = [],
            ["FoodDiary.Web.Api.IntegrationTests"] = [
                "FoodDiary.Infrastructure",
                "FoodDiary.Presentation.Api",
                "FoodDiary.Testing",
                "FoodDiary.Web.Api",
            ],
            ["FoodDiary.Web.Api.Tests"] = [
                "FoodDiary.Integrations",
                "FoodDiary.Presentation.Api",
                "FoodDiary.Web.Api",
            ],
        };

    [Fact]
    public void AllProductionProjects_AreCoveredByDependencyMatrix() {
        IReadOnlyList<string> actualProjects = ProjectReferenceReader.ReadProductionProjectNames();
        string[] expectedProjects = [.. AllowedProductionProjectReferences.Keys.Order(StringComparer.Ordinal)];

        Assert.Equal(expectedProjects, actualProjects);
    }

    [Fact]
    public void ProductionProjectReferences_MatchDependencyMatrix() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadProductionProjectReferences();

        foreach ((string? projectName, string[]? expectedReferences) in AllowedProductionProjectReferences) {
            Assert.True(
                actualReferencesByProject.TryGetValue(projectName, out string[]? actualReferences),
                $"Project '{projectName}' is missing from discovered production projects.");

            Assert.Equal(
                expectedReferences.Order(StringComparer.Ordinal).ToArray(),
                actualReferences);
        }
    }

    [Fact]
    public void AllTestProjects_AreCoveredByDependencyMatrix() {
        IReadOnlyList<string> actualProjects = ProjectReferenceReader.ReadTestProjectNames();
        string[] expectedProjects = [.. AllowedTestProjectReferences.Keys.Order(StringComparer.Ordinal)];

        Assert.Equal(expectedProjects, actualProjects);
    }

    [Fact]
    public void TestProjectReferences_MatchDependencyMatrix() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadTestProjectReferences();

        foreach ((string? projectName, string[]? expectedReferences) in AllowedTestProjectReferences) {
            Assert.True(
                actualReferencesByProject.TryGetValue(projectName, out string[]? actualReferences),
                $"Test project '{projectName}' is missing from discovered test projects.");

            Assert.Equal(
                expectedReferences.Order(StringComparer.Ordinal).ToArray(),
                actualReferences);
        }
    }

    [Fact]
    public void CoreProjects_ReferenceMailBoundedContextsOnlyThroughAllowedClientProjects() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadProductionProjectReferences();
        string[] coreProjects = [.. actualReferencesByProject.Keys
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal))
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))];

        var allowedMailClientReferences = new HashSet<string>(StringComparer.Ordinal) {
            "FoodDiary.MailInbox.Client",
            "FoodDiary.MailRelay.Client",
        };

        string[] violations = [.. coreProjects
            .SelectMany(projectName => actualReferencesByProject[projectName]
                .Where(static reference => reference.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal) ||
                                           reference.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))
                .Where(reference => !allowedMailClientReferences.Contains(reference) ||
                                    !string.Equals(projectName, "FoodDiary.Integrations", StringComparison.Ordinal))
                .Select(reference => $"{projectName} -> {reference}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void CoreProjectSource_ReferencesMailBoundedContextsOnlyFromIntegrations() {
        string[] coreSourceRoots = [.. ProjectReferenceReader.ReadProductionProjectNames()
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal))
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))
            .Where(static projectName => !string.Equals(projectName, "FoodDiary.Integrations", StringComparison.Ordinal))
            .Select(projectName => ArchitectureTestPaths.FromRoot(ProjectFolderFromProjectName(projectName)))];

        string[] violations = SourceScanner.FindLinePatternViolations(coreSourceRoots, [
            "FoodDiary.MailInbox",
            "FoodDiary.MailRelay",
        ]);

        Assert.Empty(violations);
    }

    private static string ProjectFolderFromProjectName(string projectName) =>
        projectName switch {
            "FoodDiary.Mediator" => Path.Combine("Shared", "FoodDiary.Mediator"),
            "FoodDiary.Results" => Path.Combine("Shared", "FoodDiary.Results"),
            "FoodDiary.Domain.Primitives" => Path.Combine("Shared", "FoodDiary.Domain.Primitives"),
            _ => projectName,
        };
}

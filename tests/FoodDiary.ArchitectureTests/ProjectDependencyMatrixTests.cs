namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ProjectDependencyMatrixTests {
    private static readonly string[] ModulePresentationProjectNames = [
        "FoodDiary.Modules.Admin.Presentation",
        "FoodDiary.Modules.Ai.Presentation",
        "FoodDiary.Modules.Billing.Presentation",
        "FoodDiary.Modules.BodyMetrics.Presentation",
        "FoodDiary.Modules.ContentReports.Presentation",
        "FoodDiary.Modules.Cycles.Presentation",
        "FoodDiary.Modules.Dashboard.Presentation",
        "FoodDiary.Modules.Dietologist.Presentation",
        "FoodDiary.Modules.Exercises.Presentation",
        "FoodDiary.Modules.Export.Presentation",
        "FoodDiary.Modules.Fasting.Presentation",
        "FoodDiary.Modules.Favorites.Presentation",
        "FoodDiary.Modules.Gamification.Presentation",
        "FoodDiary.Modules.Hydration.Presentation",
        "FoodDiary.Modules.Identity.Presentation",
        "FoodDiary.Modules.Images.Presentation",
        "FoodDiary.Modules.Lessons.Presentation",
        "FoodDiary.Modules.Marketing.Presentation",
        "FoodDiary.Modules.MealPlanning.Presentation",
        "FoodDiary.Modules.Meals.Presentation",
        "FoodDiary.Modules.Notifications.Presentation",
        "FoodDiary.Modules.OpenFoodFacts.Presentation",
        "FoodDiary.Modules.Products.Presentation",
        "FoodDiary.Modules.RecipeCommunity.Presentation",
        "FoodDiary.Modules.Recipes.Presentation",
        "FoodDiary.Modules.Statistics.Presentation",
        "FoodDiary.Modules.Tdee.Presentation",
        "FoodDiary.Modules.Usda.Presentation",
        "FoodDiary.Modules.Users.Presentation",
        "FoodDiary.Modules.Wearables.Presentation",
        "FoodDiary.Modules.WeeklyCheckIn.Presentation",
        "FoodDiary.Modules.WeeklyGoals.Presentation",
    ];

    private static readonly string[] ModulePresentationTestProjectNames = [.. ModulePresentationProjectNames
        .Where(static projectName => !string.Equals(projectName, "FoodDiary.Modules.Marketing.Presentation", StringComparison.Ordinal))
        .Select(static projectName => $"{projectName}.Tests")];

    private static readonly IReadOnlyDictionary<string, string[]> AllowedProductionProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["FoodDiary.Analyzers"] = [],
            ["FoodDiary.Application.Billing"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Billing.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Application.BodyMetrics"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.BodyMetrics.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Application.Contracts"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Mediator", "FoodDiary.Results"],
            ["FoodDiary.Application.Cycles"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Application.Exercises"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Exercises.Application.Abstractions", "FoodDiary.Modules.Exercises.Contracts", "FoodDiary.Modules.Exercises.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Application.Favorites"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Application.Images"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Application.Marketing"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Marketing.Application.Abstractions"],
            ["FoodDiary.Application.MealPlanning"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.MealPlanning.Application.Abstractions", "FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Application.Notifications"] = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Application.RecipeCommunity"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.RecipeCommunity.Application.Abstractions", "FoodDiary.Modules.RecipeCommunity.Domain", "FoodDiary.Modules.Recipes.Application.Abstractions", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Application.Runtime"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator"],
            ["FoodDiary.Application.Usda"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Usda.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Application.Wearables"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Wearables.Application.Abstractions", "FoodDiary.Modules.Wearables.Domain"],
            ["FoodDiary.Audit.Contracts"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Audit.PersistenceModel"] = [],
            ["FoodDiary.Authentication.Contracts"] = [],
            ["FoodDiary.Development.Mcp"] = [],
            ["FoodDiary.Domain.Primitives"] = [],
            ["FoodDiary.Email.Contracts"] = [],
            ["FoodDiary.Email.MailRelay"] = ["FoodDiary.Email.Contracts", "FoodDiary.MailRelay.Client"],
            ["FoodDiary.Email.PersistenceModel"] = ["FoodDiary.Email.Contracts", "FoodDiary.Outbox.Abstractions"],
            ["FoodDiary.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Audit.PersistenceModel", "FoodDiary.Authentication.Contracts", "FoodDiary.Email.Contracts", "FoodDiary.Email.PersistenceModel", "FoodDiary.Mediator", "FoodDiary.Modules.Admin.PersistenceModel", "FoodDiary.Modules.Ai.PersistenceModel", "FoodDiary.Modules.Billing.Domain", "FoodDiary.Modules.Billing.PersistenceModel", "FoodDiary.Modules.BodyMetrics.Domain", "FoodDiary.Modules.BodyMetrics.PersistenceModel", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.ContentReports.PersistenceModel", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Cycles.PersistenceModel", "FoodDiary.Modules.DailyAdvices.Domain", "FoodDiary.Modules.DailyAdvices.PersistenceModel", "FoodDiary.Modules.Dietologist.Domain", "FoodDiary.Modules.Dietologist.PersistenceModel", "FoodDiary.Modules.Exercises.Domain", "FoodDiary.Modules.Exercises.PersistenceModel", "FoodDiary.Modules.Fasting.Domain", "FoodDiary.Modules.Fasting.PersistenceModel", "FoodDiary.Modules.Favorites.Domain", "FoodDiary.Modules.Favorites.PersistenceModel", "FoodDiary.Modules.Gamification.Application.Abstractions", "FoodDiary.Modules.Gamification.Domain", "FoodDiary.Modules.Gamification.PersistenceModel", "FoodDiary.Modules.Hydration.Domain", "FoodDiary.Modules.Hydration.PersistenceModel", "FoodDiary.Modules.Identity.Domain", "FoodDiary.Modules.Identity.PersistenceModel", "FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Images.PersistenceModel", "FoodDiary.Modules.Lessons.Domain", "FoodDiary.Modules.Lessons.PersistenceModel", "FoodDiary.Modules.Marketing.Domain", "FoodDiary.Modules.Marketing.PersistenceModel", "FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.MealPlanning.PersistenceModel", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Meals.PersistenceModel", "FoodDiary.Modules.Notifications.Domain", "FoodDiary.Modules.Notifications.PersistenceModel", "FoodDiary.Modules.OpenFoodFacts.Domain", "FoodDiary.Modules.OpenFoodFacts.PersistenceModel", "FoodDiary.Modules.Products.PersistenceModel", "FoodDiary.Modules.RecentItems.Domain", "FoodDiary.Modules.RecentItems.PersistenceModel", "FoodDiary.Modules.RecipeCommunity.PersistenceModel", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Recipes.PersistenceModel", "FoodDiary.Modules.Usda.PersistenceModel", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Users.PersistenceModel", "FoodDiary.Modules.Wearables.PersistenceModel", "FoodDiary.Modules.WeeklyGoals.Domain", "FoodDiary.Modules.WeeklyGoals.PersistenceModel", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts", "FoodDiary.Outbox.PersistenceModel"],
            ["FoodDiary.Initializer"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Application.Exercises", "FoodDiary.Application.Images", "FoodDiary.Application.MealPlanning", "FoodDiary.Application.Notifications", "FoodDiary.Application.RecipeCommunity", "FoodDiary.Application.Runtime", "FoodDiary.Application.Usda", "FoodDiary.Infrastructure", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Admin.Infrastructure", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Infrastructure", "FoodDiary.Modules.Billing.Infrastructure", "FoodDiary.Modules.BodyMetrics.Infrastructure", "FoodDiary.Modules.ContentReports.Infrastructure", "FoodDiary.Modules.Cycles.Infrastructure", "FoodDiary.Modules.DailyAdvices.Infrastructure", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dashboard.Infrastructure", "FoodDiary.Modules.Dietologist.Infrastructure", "FoodDiary.Modules.Exercises.Infrastructure", "FoodDiary.Modules.Export.Application", "FoodDiary.Modules.Export.Infrastructure", "FoodDiary.Modules.Fasting.Infrastructure", "FoodDiary.Modules.Favorites.Infrastructure", "FoodDiary.Modules.Gamification.Infrastructure", "FoodDiary.Modules.Hydration.Infrastructure", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Identity.Infrastructure", "FoodDiary.Modules.Images.Infrastructure", "FoodDiary.Modules.Lessons.Infrastructure", "FoodDiary.Modules.Marketing.Infrastructure", "FoodDiary.Modules.MealPlanning.Infrastructure", "FoodDiary.Modules.Meals.Infrastructure", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.Infrastructure", "FoodDiary.Modules.OpenFoodFacts.Infrastructure", "FoodDiary.Modules.Products.Application", "FoodDiary.Modules.Products.Infrastructure", "FoodDiary.Modules.RecentItems.Infrastructure", "FoodDiary.Modules.RecipeCommunity.Infrastructure", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Infrastructure", "FoodDiary.Modules.Statistics.Application", "FoodDiary.Modules.Tdee.Application", "FoodDiary.Modules.Usda.Infrastructure", "FoodDiary.Modules.Users.Infrastructure", "FoodDiary.Modules.Wearables.Infrastructure", "FoodDiary.Modules.WeeklyCheckIn.Application", "FoodDiary.Modules.WeeklyGoals.Infrastructure", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts"],
            ["FoodDiary.Integrations.Http"] = [],
            ["FoodDiary.JobManager"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Exercises", "FoodDiary.Application.Images", "FoodDiary.Application.MealPlanning", "FoodDiary.Application.Notifications", "FoodDiary.Application.RecipeCommunity", "FoodDiary.Application.Runtime", "FoodDiary.Application.Usda", "FoodDiary.Email.Contracts", "FoodDiary.Email.MailRelay", "FoodDiary.Infrastructure", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Admin.Infrastructure", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Infrastructure", "FoodDiary.Modules.Billing.Infrastructure", "FoodDiary.Modules.DailyAdvices.Application", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dashboard.Infrastructure", "FoodDiary.Modules.Dietologist.Infrastructure", "FoodDiary.Modules.Export.Application", "FoodDiary.Modules.Export.Infrastructure", "FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Fasting.Infrastructure", "FoodDiary.Modules.Favorites.Infrastructure", "FoodDiary.Modules.Gamification.Infrastructure", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Identity.Infrastructure", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Images.Infrastructure", "FoodDiary.Modules.Marketing.Infrastructure", "FoodDiary.Modules.Meals.Infrastructure", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.Infrastructure", "FoodDiary.Modules.OpenFoodFacts.Application", "FoodDiary.Modules.OpenFoodFacts.Infrastructure", "FoodDiary.Modules.Products.Application", "FoodDiary.Modules.Products.Infrastructure", "FoodDiary.Modules.RecentItems.Infrastructure", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Infrastructure", "FoodDiary.Modules.Statistics.Application", "FoodDiary.Modules.Tdee.Application", "FoodDiary.Modules.Usda.Infrastructure", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Infrastructure", "FoodDiary.Modules.Wearables.Infrastructure", "FoodDiary.Modules.WeeklyGoals.Application", "FoodDiary.Modules.WeeklyGoals.Infrastructure"],
            ["FoodDiary.MailInbox.Application"] = ["FoodDiary.MailInbox.Domain", "FoodDiary.Mediator", "FoodDiary.Results"],
            ["FoodDiary.MailInbox.Client"] = [],
            ["FoodDiary.MailInbox.Domain"] = ["FoodDiary.Domain.Primitives"],
            ["FoodDiary.MailInbox.Infrastructure"] = ["FoodDiary.MailInbox.Application"],
            ["FoodDiary.MailInbox.Initializer"] = ["FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Infrastructure"],
            ["FoodDiary.MailInbox.Presentation"] = ["FoodDiary.MailInbox.Application"],
            ["FoodDiary.MailInbox.WebApi"] = ["FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Infrastructure", "FoodDiary.MailInbox.Presentation"],
            ["FoodDiary.MailRelay.Application"] = ["FoodDiary.MailRelay.Domain", "FoodDiary.Mediator", "FoodDiary.Results"],
            ["FoodDiary.MailRelay.Client"] = [],
            ["FoodDiary.MailRelay.Domain"] = ["FoodDiary.Domain.Primitives"],
            ["FoodDiary.MailRelay.Infrastructure"] = ["FoodDiary.MailRelay.Application"],
            ["FoodDiary.MailRelay.Initializer"] = ["FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Infrastructure"],
            ["FoodDiary.MailRelay.Presentation"] = ["FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Client"],
            ["FoodDiary.MailRelay.WebApi"] = ["FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Infrastructure", "FoodDiary.MailRelay.Presentation"],
            ["FoodDiary.Mediator"] = [],
            ["FoodDiary.Modules.Admin.Presentation"] = ["FoodDiary.Application.Marketing", "FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Admin.Domain", "FoodDiary.Modules.Fasting.Application", "FoodDiary.Modules.Fasting.Application.Abstractions", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Ai.Presentation"] = ["FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Ai.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Billing.Presentation"] = ["FoodDiary.Application.Billing", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Billing.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.BodyMetrics.Presentation"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.BodyMetrics.Domain", "FoodDiary.Modules.Users.Presentation", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.ContentReports.Presentation"] = ["FoodDiary.Modules.ContentReports.Application", "FoodDiary.Modules.ContentReports.Application.Abstractions", "FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Cycles.Presentation"] = ["FoodDiary.Application.Cycles", "FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Dashboard.Presentation"] = ["FoodDiary.Modules.BodyMetrics.Presentation", "FoodDiary.Modules.Cycles.Presentation", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dashboard.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Fasting.Presentation", "FoodDiary.Modules.Hydration.Presentation", "FoodDiary.Modules.Meals.Presentation", "FoodDiary.Modules.Tdee.Presentation", "FoodDiary.Modules.Users.Presentation", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Dietologist.Presentation"] = ["FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Dashboard.Presentation", "FoodDiary.Modules.Dietologist.Application", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Dietologist.Domain", "FoodDiary.Modules.Dietologist.Presentation.Contracts", "FoodDiary.Modules.Users.Presentation", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Dietologist.Presentation.Contracts"] = [],
            ["FoodDiary.Modules.Exercises.Presentation"] = ["FoodDiary.Application.Exercises", "FoodDiary.Modules.Exercises.Application.Abstractions", "FoodDiary.Modules.Exercises.Contracts", "FoodDiary.Modules.Exercises.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Export.Presentation"] = ["FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Export.Application", "FoodDiary.Modules.Export.Application.Abstractions", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Fasting.Presentation"] = ["FoodDiary.Modules.Fasting.Application", "FoodDiary.Modules.Fasting.Application.Abstractions", "FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Fasting.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Favorites.Presentation"] = ["FoodDiary.Application.Favorites", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Gamification.Presentation"] = ["FoodDiary.Modules.Gamification.Application", "FoodDiary.Modules.Gamification.Application.Abstractions", "FoodDiary.Modules.Gamification.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Hydration.Presentation"] = ["FoodDiary.Modules.Hydration.Application", "FoodDiary.Modules.Hydration.Application.Abstractions", "FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Hydration.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Identity.Presentation"] = ["FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Identity.Domain", "FoodDiary.Modules.Users.Presentation", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Images.Presentation"] = ["FoodDiary.Application.Images", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Images.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Lessons.Presentation"] = ["FoodDiary.Modules.Lessons.Application", "FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Marketing.Presentation"] = ["FoodDiary.Application.Marketing", "FoodDiary.Modules.Marketing.Application.Abstractions", "FoodDiary.Modules.Marketing.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.MealPlanning.Presentation"] = ["FoodDiary.Application.MealPlanning", "FoodDiary.Modules.MealPlanning.Application.Abstractions", "FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Meals.Presentation"] = ["FoodDiary.Modules.Favorites.Presentation", "FoodDiary.Modules.Meals.Application", "FoodDiary.Modules.Meals.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Notifications.Presentation"] = ["FoodDiary.Application.Notifications", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.OpenFoodFacts.Presentation"] = ["FoodDiary.Modules.OpenFoodFacts.Application", "FoodDiary.Modules.OpenFoodFacts.Application.Abstractions", "FoodDiary.Modules.OpenFoodFacts.Contracts", "FoodDiary.Modules.OpenFoodFacts.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Products.Presentation"] = ["FoodDiary.Modules.Favorites.Presentation", "FoodDiary.Modules.Products.Application", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.RecipeCommunity.Presentation"] = ["FoodDiary.Application.RecipeCommunity", "FoodDiary.Modules.RecipeCommunity.Application.Abstractions", "FoodDiary.Modules.RecipeCommunity.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Recipes.Presentation"] = ["FoodDiary.Modules.Favorites.Presentation", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Application.Abstractions", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Statistics.Presentation"] = ["FoodDiary.Modules.BodyMetrics.Presentation", "FoodDiary.Modules.Statistics.Application", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Tdee.Presentation"] = ["FoodDiary.Modules.Tdee.Application", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Usda.Presentation"] = ["FoodDiary.Application.Usda", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Usda.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Users.Presentation"] = ["FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Dietologist.Presentation.Contracts", "FoodDiary.Modules.Notifications.Presentation", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Wearables.Presentation"] = ["FoodDiary.Application.Wearables", "FoodDiary.Modules.Wearables.Application.Abstractions", "FoodDiary.Modules.Wearables.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.WeeklyCheckIn.Presentation"] = ["FoodDiary.Modules.WeeklyCheckIn.Application", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.WeeklyGoals.Presentation"] = ["FoodDiary.Modules.WeeklyGoals.Application", "FoodDiary.Modules.WeeklyGoals.Application.Abstractions", "FoodDiary.Modules.WeeklyGoals.Contracts", "FoodDiary.Modules.WeeklyGoals.Domain", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Modules.Admin.Application.Abstractions"] = ["FoodDiary.Modules.Admin.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Admin.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Email.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Billing.Domain", "FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Gamification.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Admin.Domain"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Admin.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Infrastructure", "FoodDiary.MailInbox.Client", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Admin.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Admin.PersistenceModel"] = ["FoodDiary.Modules.Admin.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Ai.Application.Abstractions"] = ["FoodDiary.Modules.Ai.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Ai.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Ai.Domain", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Ai.Domain"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Ai.Infrastructure"] = ["FoodDiary.Infrastructure", "FoodDiary.Integrations.Http", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Ai.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Ai.PersistenceModel"] = ["FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Ai.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Billing.Application.Abstractions"] = ["FoodDiary.Modules.Billing.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Billing.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Billing.Infrastructure"] = ["FoodDiary.Application.Billing", "FoodDiary.Infrastructure", "FoodDiary.Integrations.Http", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Billing.Domain", "FoodDiary.Modules.Billing.PersistenceModel", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Billing.PersistenceModel"] = ["FoodDiary.Modules.Billing.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.BodyMetrics.Application.Abstractions"] = ["FoodDiary.Modules.BodyMetrics.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.BodyMetrics.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.BodyMetrics.Infrastructure"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Infrastructure", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.BodyMetrics.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.BodyMetrics.PersistenceModel"] = ["FoodDiary.Modules.BodyMetrics.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.ContentReports.Application.Abstractions"] = ["FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.ContentReports.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.ContentReports.Application.Abstractions", "FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.ContentReports.Contracts"] = ["FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.ContentReports.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.ContentReports.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Domain.Primitives", "FoodDiary.Infrastructure", "FoodDiary.Modules.ContentReports.Application", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.ContentReports.PersistenceModel", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.ContentReports.PersistenceModel"] = ["FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Cycles.Application.Abstractions"] = ["FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Cycles.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Cycles.Infrastructure"] = ["FoodDiary.Application.Cycles", "FoodDiary.Infrastructure", "FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Cycles.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Cycles.PersistenceModel"] = ["FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.DailyAdvices.Application.Abstractions"] = ["FoodDiary.Results"],
            ["FoodDiary.Modules.DailyAdvices.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.DailyAdvices.Application.Abstractions", "FoodDiary.Modules.DailyAdvices.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.DailyAdvices.Domain"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.DailyAdvices.Infrastructure"] = ["FoodDiary.Infrastructure", "FoodDiary.Modules.DailyAdvices.Application", "FoodDiary.Modules.DailyAdvices.Application.Abstractions", "FoodDiary.Modules.DailyAdvices.PersistenceModel"],
            ["FoodDiary.Modules.DailyAdvices.PersistenceModel"] = ["FoodDiary.Modules.DailyAdvices.Domain"],
            ["FoodDiary.Modules.Dashboard.Application.Abstractions"] = ["FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Dashboard.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Cycles", "FoodDiary.Audit.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.DailyAdvices.Application", "FoodDiary.Modules.Dashboard.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Exercises.Contracts", "FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Meals.Application", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Tdee.Application", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Dashboard.Contracts"] = ["FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Dashboard.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Dashboard.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Dietologist.Application.Abstractions"] = ["FoodDiary.Modules.Dietologist.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Dietologist.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Email.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Dietologist.Domain", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Dietologist.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Dietologist.Infrastructure"] = ["FoodDiary.Infrastructure", "FoodDiary.Modules.Dietologist.Application", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Dietologist.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Dietologist.PersistenceModel"] = ["FoodDiary.Modules.Dietologist.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Exercises.Application.Abstractions"] = ["FoodDiary.Modules.Exercises.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Exercises.Contracts"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Exercises.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Exercises.Infrastructure"] = ["FoodDiary.Application.Exercises", "FoodDiary.Infrastructure", "FoodDiary.Modules.Exercises.Application.Abstractions", "FoodDiary.Modules.Exercises.PersistenceModel", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Exercises.PersistenceModel"] = ["FoodDiary.Modules.Exercises.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Export.Application.Abstractions"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Export.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Cycles", "FoodDiary.Authentication.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Export.Application.Abstractions", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Export.Infrastructure"] = ["FoodDiary.Modules.Export.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain"],
            ["FoodDiary.Modules.Fasting.Application.Abstractions"] = ["FoodDiary.Modules.Fasting.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Fasting.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Fasting.Application.Abstractions", "FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Fasting.Domain", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Fasting.Contracts"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Fasting.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Fasting.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Fasting.Application", "FoodDiary.Modules.Fasting.Application.Abstractions", "FoodDiary.Modules.Fasting.PersistenceModel", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Fasting.PersistenceModel"] = ["FoodDiary.Modules.Fasting.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Favorites.Application.Abstractions"] = ["FoodDiary.Modules.Favorites.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Favorites.Contracts"] = ["FoodDiary.Modules.Favorites.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Favorites.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Favorites.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Favorites", "FoodDiary.Domain.Primitives", "FoodDiary.Infrastructure", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Favorites.PersistenceModel", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Favorites.PersistenceModel"] = ["FoodDiary.Modules.Favorites.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Gamification.Application.Abstractions"] = ["FoodDiary.Modules.Gamification.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Gamification.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Gamification.Application.Abstractions", "FoodDiary.Modules.Gamification.Domain", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Gamification.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Gamification.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Gamification.Application", "FoodDiary.Modules.Gamification.Application.Abstractions", "FoodDiary.Modules.Gamification.PersistenceModel", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts"],
            ["FoodDiary.Modules.Gamification.PersistenceModel"] = ["FoodDiary.Modules.Gamification.Domain", "FoodDiary.Modules.Users.Domain", "FoodDiary.Outbox.Abstractions"],
            ["FoodDiary.Modules.Hydration.Application.Abstractions"] = ["FoodDiary.Modules.Hydration.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Hydration.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Hydration.Application.Abstractions", "FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Hydration.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Hydration.Contracts"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Hydration.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Hydration.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Hydration.Application", "FoodDiary.Modules.Hydration.Application.Abstractions", "FoodDiary.Modules.Hydration.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Hydration.PersistenceModel"] = ["FoodDiary.Modules.Hydration.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Identity.Application.Abstractions"] = ["FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Identity.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Identity.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Email.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Identity.Domain", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Identity.Domain"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Identity.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Identity.PersistenceModel", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Identity.PersistenceModel"] = ["FoodDiary.Modules.Identity.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Images.Application.Abstractions"] = ["FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Images.Contracts"] = ["FoodDiary.Domain.Primitives"],
            ["FoodDiary.Modules.Images.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Images.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Images", "FoodDiary.Infrastructure", "FoodDiary.Integrations.Http", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Images.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts"],
            ["FoodDiary.Modules.Images.PersistenceModel"] = ["FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Users.Domain", "FoodDiary.Outbox.Abstractions"],
            ["FoodDiary.Modules.Lessons.Application.Abstractions"] = ["FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Lessons.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Gamification.Application.Abstractions", "FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Lessons.Contracts"] = ["FoodDiary.Modules.Lessons.Domain", "FoodDiary.Results"],
            ["FoodDiary.Modules.Lessons.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Lessons.Infrastructure"] = ["FoodDiary.Infrastructure", "FoodDiary.Modules.Lessons.Application", "FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Lessons.PersistenceModel", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Lessons.PersistenceModel"] = ["FoodDiary.Modules.Lessons.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Marketing.Application.Abstractions"] = [],
            ["FoodDiary.Modules.Marketing.Domain"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Marketing.Infrastructure"] = ["FoodDiary.Application.Marketing", "FoodDiary.Infrastructure", "FoodDiary.Modules.Marketing.Application.Abstractions", "FoodDiary.Modules.Marketing.Domain", "FoodDiary.Modules.Marketing.PersistenceModel", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Marketing.PersistenceModel"] = ["FoodDiary.Modules.Marketing.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.MealPlanning.Application.Abstractions"] = ["FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.MealPlanning.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.MealPlanning.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.MealPlanning", "FoodDiary.Infrastructure", "FoodDiary.Modules.MealPlanning.Application.Abstractions", "FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.MealPlanning.PersistenceModel", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.MealPlanning.PersistenceModel"] = ["FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Meals.Application.Abstractions"] = ["FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Meals.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Images", "FoodDiary.Mediator", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Meals.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Application.Abstractions", "FoodDiary.Modules.Recipes.Application.Abstractions", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Nutrition.Contracts"],
            ["FoodDiary.Modules.Meals.Contracts"] = ["FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Favorites.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Meals.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Meals.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Meals.Application", "FoodDiary.Modules.Meals.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Meals.PersistenceModel", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Meals.PersistenceModel"] = ["FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Notifications.Application.Abstractions"] = ["FoodDiary.Modules.Notifications.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Notifications.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Notifications.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.PersistenceModel", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts"],
            ["FoodDiary.Modules.Notifications.PersistenceModel"] = ["FoodDiary.Modules.Notifications.Domain", "FoodDiary.Modules.Users.Domain", "FoodDiary.Outbox.Abstractions"],
            ["FoodDiary.Modules.OpenFoodFacts.Application.Abstractions"] = ["FoodDiary.Modules.OpenFoodFacts.Contracts"],
            ["FoodDiary.Modules.OpenFoodFacts.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.OpenFoodFacts.Application.Abstractions", "FoodDiary.Modules.OpenFoodFacts.Contracts"],
            ["FoodDiary.Modules.OpenFoodFacts.Contracts"] = [],
            ["FoodDiary.Modules.OpenFoodFacts.Domain"] = [],
            ["FoodDiary.Modules.OpenFoodFacts.Infrastructure"] = ["FoodDiary.Infrastructure", "FoodDiary.Integrations.Http", "FoodDiary.Modules.OpenFoodFacts.Application", "FoodDiary.Modules.OpenFoodFacts.Application.Abstractions", "FoodDiary.Modules.OpenFoodFacts.Contracts", "FoodDiary.Modules.OpenFoodFacts.PersistenceModel"],
            ["FoodDiary.Modules.OpenFoodFacts.PersistenceModel"] = ["FoodDiary.Modules.OpenFoodFacts.Domain"],
            ["FoodDiary.Modules.Products.Application.Abstractions"] = ["FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Products.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Images", "FoodDiary.Domain.Primitives", "FoodDiary.Mediator", "FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.OpenFoodFacts.Contracts", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Application.Abstractions", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Products.Contracts"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Products.Domain.Contracts"] = ["FoodDiary.Domain.Primitives"],
            ["FoodDiary.Modules.Products.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Products.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Domain.Primitives", "FoodDiary.Infrastructure", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Products.Application", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Products.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Products.PersistenceModel"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Usda.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.RecentItems.Application.Abstractions"] = ["FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.RecentItems.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.RecentItems.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Application.Abstractions", "FoodDiary.Modules.RecentItems.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.RecentItems.PersistenceModel"] = ["FoodDiary.Modules.RecentItems.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.RecipeCommunity.Application.Abstractions"] = ["FoodDiary.Modules.RecipeCommunity.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.RecipeCommunity.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.RecipeCommunity.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.RecipeCommunity", "FoodDiary.Infrastructure", "FoodDiary.Modules.RecipeCommunity.Application.Abstractions", "FoodDiary.Modules.RecipeCommunity.PersistenceModel", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.RecipeCommunity.PersistenceModel"] = ["FoodDiary.Modules.RecipeCommunity.Domain", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Recipes.Application.Abstractions"] = ["FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Recipes.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Images", "FoodDiary.Domain.Primitives", "FoodDiary.Mediator", "FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Application.Abstractions", "FoodDiary.Modules.Recipes.Application.Abstractions", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Nutrition.Contracts"],
            ["FoodDiary.Modules.Recipes.Contracts"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Recipes.Domain.Contracts"] = ["FoodDiary.Domain.Primitives"],
            ["FoodDiary.Modules.Recipes.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Recipes.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Domain.Primitives", "FoodDiary.Infrastructure", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Application.Abstractions", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Recipes.PersistenceModel", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Recipes.PersistenceModel"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Statistics.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Tdee.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Exercises.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Usda.Application.Abstractions"] = ["FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Usda.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Usda.Contracts"] = [],
            ["FoodDiary.Modules.Usda.Domain"] = ["FoodDiary.Domain.Primitives"],
            ["FoodDiary.Modules.Usda.Infrastructure"] = ["FoodDiary.Application.Usda", "FoodDiary.Infrastructure", "FoodDiary.Integrations.Http", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Usda.PersistenceModel"],
            ["FoodDiary.Modules.Usda.PersistenceModel"] = ["FoodDiary.Modules.Usda.Domain"],
            ["FoodDiary.Modules.Users.Application.Abstractions"] = ["FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Users.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Users.Contracts"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"],
            ["FoodDiary.Modules.Users.Domain.Contracts"] = ["FoodDiary.Domain.Primitives"],
            ["FoodDiary.Modules.Users.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Users.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.PersistenceModel"],
            ["FoodDiary.Modules.Users.PersistenceModel"] = ["FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Wearables.Application.Abstractions"] = ["FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Wearables.Domain", "FoodDiary.Results"],
            ["FoodDiary.Modules.Wearables.Domain"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Wearables.Infrastructure"] = ["FoodDiary.Application.Wearables", "FoodDiary.Infrastructure", "FoodDiary.Integrations.Http", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Wearables.Application.Abstractions", "FoodDiary.Modules.Wearables.Domain", "FoodDiary.Modules.Wearables.PersistenceModel"],
            ["FoodDiary.Modules.Wearables.PersistenceModel"] = ["FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Wearables.Domain"],
            ["FoodDiary.Modules.WeeklyCheckIn.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.WeeklyGoals.Application.Abstractions"] = ["FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.WeeklyGoals.Domain"],
            ["FoodDiary.Modules.WeeklyGoals.Application"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.WeeklyGoals.Application.Abstractions", "FoodDiary.Modules.WeeklyGoals.Contracts", "FoodDiary.Modules.WeeklyGoals.Domain"],
            ["FoodDiary.Modules.WeeklyGoals.Contracts"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.WeeklyGoals.Domain"] = ["FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.WeeklyGoals.Infrastructure"] = ["FoodDiary.Application.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.WeeklyGoals.Application", "FoodDiary.Modules.WeeklyGoals.PersistenceModel"],
            ["FoodDiary.Modules.WeeklyGoals.PersistenceModel"] = ["FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.WeeklyGoals.Domain"],
            ["FoodDiary.Nutrition.Contracts"] = [],
            ["FoodDiary.Outbox.Abstractions"] = [],
            ["FoodDiary.Outbox.PersistenceModel"] = [],
            ["FoodDiary.Outbox.Management.Contracts"] = [],
            ["FoodDiary.Presentation.Api"] = ["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Export.Application", "FoodDiary.Results"],
            ["FoodDiary.Results"] = [],
            ["FoodDiary.Telegram.Bot"] = [],
            ["FoodDiary.Web.Api"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Application.Exercises", "FoodDiary.Application.Images", "FoodDiary.Application.MealPlanning", "FoodDiary.Application.Notifications", "FoodDiary.Application.RecipeCommunity", "FoodDiary.Application.Runtime", "FoodDiary.Application.Usda", "FoodDiary.Authentication.Contracts", "FoodDiary.Email.MailRelay", "FoodDiary.Infrastructure", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Admin.Infrastructure", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Infrastructure", "FoodDiary.Modules.Billing.Infrastructure", "FoodDiary.Modules.BodyMetrics.Infrastructure", "FoodDiary.Modules.ContentReports.Infrastructure", "FoodDiary.Modules.Cycles.Infrastructure", "FoodDiary.Modules.DailyAdvices.Infrastructure", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dashboard.Infrastructure", "FoodDiary.Modules.Dietologist.Infrastructure", "FoodDiary.Modules.Exercises.Infrastructure", "FoodDiary.Modules.Export.Application", "FoodDiary.Modules.Export.Infrastructure", "FoodDiary.Modules.Fasting.Infrastructure", "FoodDiary.Modules.Favorites.Infrastructure", "FoodDiary.Modules.Gamification.Infrastructure", "FoodDiary.Modules.Hydration.Infrastructure", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Infrastructure", "FoodDiary.Modules.Images.Infrastructure", "FoodDiary.Modules.Lessons.Infrastructure", "FoodDiary.Modules.Marketing.Infrastructure", "FoodDiary.Modules.MealPlanning.Infrastructure", "FoodDiary.Modules.Meals.Infrastructure", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.Infrastructure", "FoodDiary.Modules.OpenFoodFacts.Infrastructure", "FoodDiary.Modules.Products.Application", "FoodDiary.Modules.Products.Infrastructure", "FoodDiary.Modules.RecentItems.Infrastructure", "FoodDiary.Modules.RecipeCommunity.Infrastructure", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Infrastructure", "FoodDiary.Modules.Statistics.Application", "FoodDiary.Modules.Tdee.Application", "FoodDiary.Modules.Usda.Infrastructure", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Infrastructure", "FoodDiary.Modules.Wearables.Infrastructure", "FoodDiary.Modules.WeeklyCheckIn.Application", "FoodDiary.Modules.WeeklyGoals.Infrastructure", "FoodDiary.Presentation.Api"],
        };

    private static readonly IReadOnlyDictionary<string, string[]> AllowedTestProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["FoodDiary.Analyzers.Tests"] = ["FoodDiary.Analyzers"],
            ["FoodDiary.Application.Tests"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Application.Exercises", "FoodDiary.Application.Favorites", "FoodDiary.Application.Notifications", "FoodDiary.Application.RecipeCommunity", "FoodDiary.Application.Runtime", "FoodDiary.Application.Usda", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Domain.Primitives", "FoodDiary.Email.Contracts", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.ContentReports.Application", "FoodDiary.Modules.ContentReports.Application.Abstractions", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dietologist.Application", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Export.Application", "FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Hydration.Application", "FoodDiary.Modules.Hydration.Infrastructure", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Lessons.Application", "FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.MealPlanning.Application.Abstractions", "FoodDiary.Modules.Meals.Application.Abstractions", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.OpenFoodFacts.Application", "FoodDiary.Modules.OpenFoodFacts.Application.Abstractions", "FoodDiary.Modules.Products.Application", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Application.Abstractions", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Statistics.Application", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Wearables.Application.Abstractions", "FoodDiary.Nutrition.Contracts"],
            ["FoodDiary.ArchitectureTests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Domain.Primitives", "FoodDiary.Email.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Usda.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Nutrition.Contracts"],
            ["FoodDiary.Development.Mcp.Tests"] = ["FoodDiary.Development.Mcp"],
            ["FoodDiary.Domain.Primitives.Tests"] = ["FoodDiary.Domain.Primitives"],
            ["FoodDiary.Domain.Tests"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Ai.Domain", "FoodDiary.Modules.Billing.Domain", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Dietologist.Domain", "FoodDiary.Modules.Fasting.Domain", "FoodDiary.Modules.Favorites.Domain", "FoodDiary.Modules.Gamification.Domain", "FoodDiary.Modules.Identity.Domain", "FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Notifications.Domain", "FoodDiary.Modules.OpenFoodFacts.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecipeCommunity.Domain", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Usda.Domain", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Wearables.Domain"],
            ["FoodDiary.Email.MailRelay.Tests"] = ["FoodDiary.Email.Contracts", "FoodDiary.Email.MailRelay", "FoodDiary.MailRelay.Client"],
            ["FoodDiary.Infrastructure.IntegrationTests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Domain.Primitives", "FoodDiary.Email.Contracts", "FoodDiary.Infrastructure", "FoodDiary.Initializer", "FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Admin.Infrastructure", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Ai.Infrastructure", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Billing.Infrastructure", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.BodyMetrics.Infrastructure", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.ContentReports.Infrastructure", "FoodDiary.Modules.Cycles.Infrastructure", "FoodDiary.Modules.DailyAdvices.Application.Abstractions", "FoodDiary.Modules.DailyAdvices.Infrastructure", "FoodDiary.Modules.Dashboard.Infrastructure", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Dietologist.Infrastructure", "FoodDiary.Modules.Exercises.Application.Abstractions", "FoodDiary.Modules.Exercises.Infrastructure", "FoodDiary.Modules.Fasting.Application.Abstractions", "FoodDiary.Modules.Fasting.Infrastructure", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Favorites.Infrastructure", "FoodDiary.Modules.Gamification.Infrastructure", "FoodDiary.Modules.Hydration.Application.Abstractions", "FoodDiary.Modules.Hydration.Infrastructure", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Images.Infrastructure", "FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Infrastructure", "FoodDiary.Modules.MealPlanning.Application.Abstractions", "FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.MealPlanning.Infrastructure", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Meals.Infrastructure", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.Infrastructure", "FoodDiary.Modules.OpenFoodFacts.Infrastructure", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Products.Infrastructure", "FoodDiary.Modules.RecentItems.Infrastructure", "FoodDiary.Modules.RecipeCommunity.Application.Abstractions", "FoodDiary.Modules.RecipeCommunity.Infrastructure", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Recipes.Infrastructure", "FoodDiary.Modules.Usda.Infrastructure", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Infrastructure", "FoodDiary.Modules.Wearables.Application.Abstractions", "FoodDiary.Modules.Wearables.Infrastructure", "FoodDiary.Modules.WeeklyGoals.Infrastructure", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts", "FoodDiary.Testing"],
            ["FoodDiary.Infrastructure.Tests"] = [
            "FoodDiary.Application.BodyMetrics",
            "FoodDiary.Application.Contracts",
            "FoodDiary.Application.Exercises",
            "FoodDiary.Application.MealPlanning",
            "FoodDiary.Application.Notifications",
            "FoodDiary.Application.RecipeCommunity",
            "FoodDiary.Audit.Contracts",
            "FoodDiary.Authentication.Contracts",
            "FoodDiary.Email.Contracts",
            "FoodDiary.Email.MailRelay",
            "FoodDiary.Infrastructure",
            "FoodDiary.Initializer",
            "FoodDiary.Integrations.Http",
            "FoodDiary.Modules.Admin.Application.Abstractions",
            "FoodDiary.Modules.Admin.Infrastructure",
            "FoodDiary.Modules.Ai.Application.Abstractions",
            "FoodDiary.Modules.Ai.Infrastructure",
            "FoodDiary.Modules.Billing.Application.Abstractions",
            "FoodDiary.Modules.Billing.Infrastructure",
            "FoodDiary.Modules.BodyMetrics.Application.Abstractions",
            "FoodDiary.Modules.BodyMetrics.Infrastructure",
            "FoodDiary.Modules.ContentReports.Infrastructure",
            "FoodDiary.Modules.Cycles.Infrastructure",
            "FoodDiary.Modules.Dashboard.Contracts",
            "FoodDiary.Modules.Dashboard.Infrastructure",
            "FoodDiary.Modules.Dietologist.Application.Abstractions",
            "FoodDiary.Modules.Dietologist.Infrastructure",
            "FoodDiary.Modules.Exercises.Application.Abstractions",
            "FoodDiary.Modules.Exercises.Infrastructure",
            "FoodDiary.Modules.Fasting.Infrastructure",
            "FoodDiary.Modules.Hydration.Application.Abstractions",
            "FoodDiary.Modules.Hydration.Infrastructure",
            "FoodDiary.Modules.Identity.Application",
            "FoodDiary.Modules.Identity.Application.Abstractions",
            "FoodDiary.Modules.Identity.Infrastructure",
            "FoodDiary.Modules.Images.Application.Abstractions",
            "FoodDiary.Modules.Images.Infrastructure",
            "FoodDiary.Modules.Lessons.Infrastructure",
            "FoodDiary.Modules.MealPlanning.Infrastructure",
            "FoodDiary.Modules.Meals.Domain",
            "FoodDiary.Modules.Meals.Infrastructure",
            "FoodDiary.Modules.Notifications.Application.Abstractions",
            "FoodDiary.Modules.Notifications.Infrastructure",
            "FoodDiary.Modules.OpenFoodFacts.Application.Abstractions",
            "FoodDiary.Modules.OpenFoodFacts.Contracts",
            "FoodDiary.Modules.OpenFoodFacts.Infrastructure",
            "FoodDiary.Modules.Products.Domain.Contracts",
            "FoodDiary.Modules.Products.Infrastructure",
            "FoodDiary.Modules.RecentItems.Infrastructure",
            "FoodDiary.Modules.RecipeCommunity.Infrastructure",
            "FoodDiary.Modules.Recipes.Infrastructure",
            "FoodDiary.Modules.Tdee.Application",
            "FoodDiary.Modules.Usda.Application.Abstractions",
            "FoodDiary.Modules.Usda.Infrastructure",
            "FoodDiary.Modules.Users.Domain",
            "FoodDiary.Modules.Users.Infrastructure",
            "FoodDiary.Modules.Wearables.Application.Abstractions",
            "FoodDiary.Modules.Wearables.Infrastructure",
            "FoodDiary.Outbox.Abstractions",
            "FoodDiary.Outbox.Management.Contracts",
        ],
            ["FoodDiary.JobManager.Tests"] = ["FoodDiary.Application.Billing", "FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Application.Exercises", "FoodDiary.Application.Images", "FoodDiary.Application.MealPlanning", "FoodDiary.Application.Notifications", "FoodDiary.Application.RecipeCommunity", "FoodDiary.Email.Contracts", "FoodDiary.JobManager", "FoodDiary.Modules.Ai.Infrastructure", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Billing.Infrastructure", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Dietologist.Infrastructure", "FoodDiary.Modules.Fasting.Application", "FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Favorites.Infrastructure", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Marketing.Application.Abstractions", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.OpenFoodFacts.Application", "FoodDiary.Modules.OpenFoodFacts.Infrastructure", "FoodDiary.Modules.Products.Infrastructure", "FoodDiary.Modules.Tdee.Application", "FoodDiary.Modules.Usda.Infrastructure", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Wearables.Infrastructure", "FoodDiary.Modules.WeeklyGoals.Application", "FoodDiary.Modules.WeeklyGoals.Infrastructure"],
            ["FoodDiary.MailInbox.Application.Tests"] = ["FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Domain"],
            ["FoodDiary.MailInbox.Client.Tests"] = ["FoodDiary.MailInbox.Client"],
            ["FoodDiary.MailInbox.Domain.Tests"] = ["FoodDiary.MailInbox.Domain"],
            ["FoodDiary.MailInbox.Infrastructure.Tests"] = ["FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Domain", "FoodDiary.MailInbox.Infrastructure"],
            ["FoodDiary.MailInbox.Initializer.Tests"] = ["FoodDiary.MailInbox.Initializer"],
            ["FoodDiary.MailInbox.IntegrationTests"] = ["FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Domain", "FoodDiary.MailInbox.Infrastructure", "FoodDiary.MailInbox.WebApi", "FoodDiary.Testing"],
            ["FoodDiary.MailInbox.Presentation.Tests"] = ["FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Domain", "FoodDiary.MailInbox.Presentation"],
            ["FoodDiary.MailRelay.Application.Tests"] = ["FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Domain"],
            ["FoodDiary.MailRelay.Client.Tests"] = ["FoodDiary.MailRelay.Client"],
            ["FoodDiary.MailRelay.Domain.Tests"] = ["FoodDiary.MailRelay.Domain"],
            ["FoodDiary.MailRelay.Infrastructure.Tests"] = ["FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Client", "FoodDiary.MailRelay.Domain", "FoodDiary.MailRelay.Infrastructure"],
            ["FoodDiary.MailRelay.Initializer.Tests"] = ["FoodDiary.MailRelay.Initializer"],
            ["FoodDiary.MailRelay.IntegrationTests"] = ["FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Domain", "FoodDiary.MailRelay.Infrastructure", "FoodDiary.MailRelay.WebApi", "FoodDiary.Testing"],
            ["FoodDiary.MailRelay.Presentation.Tests"] = ["FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Client", "FoodDiary.MailRelay.Domain", "FoodDiary.MailRelay.Presentation"],
            ["FoodDiary.Mediator.Tests"] = ["FoodDiary.Mediator"],
            ["FoodDiary.Integrations.Http.Tests"] = ["FoodDiary.Integrations.Http"],
            ["FoodDiary.Modules.Admin.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Email.Contracts", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Ai.Domain", "FoodDiary.Modules.ContentReports.Application", "FoodDiary.Modules.ContentReports.Application.Abstractions", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Lessons.Application", "FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Admin.Domain.Tests"] = ["FoodDiary.Modules.Admin.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Admin.Infrastructure.IntegrationTests"] = ["FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Admin.Infrastructure", "FoodDiary.Modules.Users.Domain", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Admin.Infrastructure.Tests"] = ["FoodDiary.Authentication.Contracts", "FoodDiary.MailInbox.Client", "FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Admin.Infrastructure", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Infrastructure"],
            ["FoodDiary.Modules.Ai.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Images", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Ai.Domain.Tests"] = ["FoodDiary.Modules.Ai.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Ai.Infrastructure.Tests"] = ["FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Ai.Infrastructure", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Billing.Application.Tests"] = ["FoodDiary.Application.Billing", "FoodDiary.Application.Contracts", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Marketing.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Billing.Domain.Tests"] = ["FoodDiary.Modules.Billing.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Billing.Infrastructure.Tests"] = ["FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Billing.Infrastructure", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.BodyMetrics.Application.Tests"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Results"],
            ["FoodDiary.Modules.BodyMetrics.Domain.Tests"] = ["FoodDiary.Modules.BodyMetrics.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.ContentReports.Application.Tests"] = ["FoodDiary.Modules.ContentReports.Application", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.ContentReports.Domain.Tests"] = ["FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.ContentReports.Infrastructure.Tests"] = ["FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.ContentReports.Infrastructure", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Cycles.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Cycles", "FoodDiary.Modules.Users.Contracts"],
            ["FoodDiary.Modules.Cycles.Domain.Tests"] = ["FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Cycles.Infrastructure.IntegrationTests"] = ["FoodDiary.Initializer", "FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Cycles.Infrastructure", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Users.Domain", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Cycles.Infrastructure.Tests"] = ["FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Infrastructure"],
            ["FoodDiary.Modules.DailyAdvices.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.DailyAdvices.Application", "FoodDiary.Modules.DailyAdvices.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.DailyAdvices.Domain.Tests"] = ["FoodDiary.Modules.DailyAdvices.Domain"],
            ["FoodDiary.Modules.DailyAdvices.Infrastructure.Tests"] = ["FoodDiary.Modules.DailyAdvices.Application.Abstractions", "FoodDiary.Modules.DailyAdvices.Infrastructure"],
            ["FoodDiary.Modules.Dashboard.Application.Tests"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Application.Exercises", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Exercises.Application.Abstractions", "FoodDiary.Modules.Hydration.Application", "FoodDiary.Modules.Hydration.Application.Abstractions", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Statistics.Application", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Dashboard.Infrastructure.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Dashboard.Infrastructure", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Meals.Infrastructure", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Dietologist.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Notifications", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dietologist.Application", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Dietologist.Domain.Tests"] = ["FoodDiary.Modules.Dietologist.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Dietologist.Infrastructure.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Email.Contracts", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Dietologist.Infrastructure", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Exercises.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Exercises", "FoodDiary.Modules.Exercises.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Exercises.Domain.Tests"] = ["FoodDiary.Modules.Exercises.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Export.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Cycles", "FoodDiary.Modules.Export.Application", "FoodDiary.Modules.Meals.Application", "FoodDiary.Modules.Users.Contracts"],
            ["FoodDiary.Modules.Export.Infrastructure.Tests"] = ["FoodDiary.Modules.Export.Infrastructure", "FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Fasting.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Notifications", "FoodDiary.Modules.Fasting.Application", "FoodDiary.Modules.Fasting.Application.Abstractions", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Fasting.Domain.Tests"] = ["FoodDiary.Modules.Fasting.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Fasting.Infrastructure.Tests"] = ["FoodDiary.Initializer", "FoodDiary.Modules.Fasting.Application.Abstractions", "FoodDiary.Modules.Fasting.Infrastructure", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Favorites.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Favorites", "FoodDiary.Domain.Primitives", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Favorites.Domain.Tests"] = ["FoodDiary.Modules.Favorites.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Gamification.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Gamification.Application", "FoodDiary.Modules.Gamification.Application.Abstractions", "FoodDiary.Modules.Meals.Application", "FoodDiary.Modules.Meals.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Gamification.Domain.Tests"] = ["FoodDiary.Modules.Gamification.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Gamification.Infrastructure.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Gamification.Infrastructure", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts"],
            ["FoodDiary.Modules.Hydration.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Hydration.Application", "FoodDiary.Modules.Hydration.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Hydration.Domain.Tests"] = ["FoodDiary.Modules.Hydration.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Hydration.Infrastructure.Tests"] = ["FoodDiary.Initializer", "FoodDiary.Modules.Hydration.Infrastructure", "FoodDiary.Modules.Users.Domain", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Identity.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Notifications", "FoodDiary.Application.Runtime", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Email.Contracts", "FoodDiary.Modules.Dietologist.Application", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Identity.Domain.Tests"] = ["FoodDiary.Modules.Identity.Domain"],
            ["FoodDiary.Modules.Identity.Infrastructure.IntegrationTests"] = ["FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Identity.Infrastructure", "FoodDiary.Modules.Users.Domain", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Identity.Infrastructure.Tests"] = ["FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Identity.Infrastructure"],
            ["FoodDiary.Modules.Images.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Images", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Images.Domain.Tests"] = ["FoodDiary.Modules.Images.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Images.Infrastructure.IntegrationTests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Images.Infrastructure", "FoodDiary.Modules.Users.Domain", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Images.Infrastructure.Tests"] = ["FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Images.Infrastructure", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Lessons.Application.Tests"] = ["FoodDiary.Modules.Lessons.Application", "FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Lessons.Domain.Tests"] = ["FoodDiary.Modules.Lessons.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Lessons.Infrastructure.Tests"] = ["FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Lessons.Infrastructure"],
            ["FoodDiary.Modules.Marketing.Application.Tests"] = ["FoodDiary.Application.Marketing", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.Marketing.Application.Abstractions"],
            ["FoodDiary.Modules.Marketing.Domain.Tests"] = ["FoodDiary.Modules.Marketing.Domain"],
            ["FoodDiary.Modules.Marketing.Infrastructure.IntegrationTests"] = ["FoodDiary.Initializer", "FoodDiary.Modules.Marketing.Application.Abstractions", "FoodDiary.Modules.Marketing.Infrastructure", "FoodDiary.Testing"],
            ["FoodDiary.Modules.MealPlanning.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.MealPlanning", "FoodDiary.Domain.Primitives", "FoodDiary.Modules.MealPlanning.Application.Abstractions", "FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.MealPlanning.Domain.Tests"] = ["FoodDiary.Modules.MealPlanning.Domain", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.MealPlanning.Infrastructure.IntegrationTests"] = ["FoodDiary.Infrastructure", "FoodDiary.Modules.MealPlanning.Infrastructure", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Meals.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Favorites", "FoodDiary.Domain.Primitives", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Meals.Application", "FoodDiary.Modules.Meals.Application.Abstractions", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Nutrition.Contracts"],
            ["FoodDiary.Modules.Meals.Domain.Tests"] = ["FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Meals.Infrastructure.IntegrationTests"] = ["FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Meals.Infrastructure", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Notifications.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Notifications", "FoodDiary.Audit.Contracts", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Notifications.Domain.Tests"] = ["FoodDiary.Modules.Notifications.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Notifications.Infrastructure.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Notifications", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.Infrastructure", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Outbox.Abstractions", "FoodDiary.Outbox.Management.Contracts"],
            ["FoodDiary.Modules.OpenFoodFacts.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.OpenFoodFacts.Application"],
            ["FoodDiary.Modules.OpenFoodFacts.Domain.Tests"] = ["FoodDiary.Modules.OpenFoodFacts.Domain"],
            ["FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests"] = ["FoodDiary.Modules.OpenFoodFacts.Application.Abstractions", "FoodDiary.Modules.OpenFoodFacts.Contracts", "FoodDiary.Modules.OpenFoodFacts.Infrastructure"],
            ["FoodDiary.Modules.Products.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Favorites", "FoodDiary.Application.Usda", "FoodDiary.Domain.Primitives", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.OpenFoodFacts.Application", "FoodDiary.Modules.OpenFoodFacts.Application.Abstractions", "FoodDiary.Modules.Products.Application", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Application.Abstractions", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Usda.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Products.Domain.Tests"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Products.Infrastructure.IntegrationTests"] = ["FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Products.Infrastructure", "FoodDiary.Modules.Users.Domain", "FoodDiary.Testing"],
            ["FoodDiary.Modules.RecentItems.Domain.Tests"] = ["FoodDiary.Modules.RecentItems.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.RecentItems.Infrastructure.IntegrationTests"] = ["FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Infrastructure", "FoodDiary.Modules.Users.Domain", "FoodDiary.Testing"],
            ["FoodDiary.Modules.RecentItems.Infrastructure.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Infrastructure", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.RecipeCommunity.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.RecipeCommunity", "FoodDiary.Domain.Primitives", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.RecipeCommunity.Application.Abstractions", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.RecipeCommunity.Domain.Tests"] = ["FoodDiary.Modules.RecipeCommunity.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Recipes.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Favorites", "FoodDiary.Domain.Primitives", "FoodDiary.Modules.Favorites.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Application.Abstractions", "FoodDiary.Modules.Recipes.Application", "FoodDiary.Modules.Recipes.Application.Abstractions", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Nutrition.Contracts"],
            ["FoodDiary.Modules.Recipes.Domain.Tests"] = ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Statistics.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Statistics.Application", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Tdee.Application.Tests"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Application.Exercises", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Exercises.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Tdee.Application", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"],
            ["FoodDiary.Modules.Usda.Application.Tests"] = ["FoodDiary.Application.Usda", "FoodDiary.Modules.Meals.Application", "FoodDiary.Modules.Meals.Application.Abstractions", "FoodDiary.Modules.Products.Application", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Usda.Domain.Tests"] = ["FoodDiary.Modules.Usda.Domain"],
            ["FoodDiary.Modules.Usda.Infrastructure.Tests"] = ["FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Usda.Infrastructure"],
            ["FoodDiary.Modules.Users.Application.Tests"] = ["FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Application.Notifications", "FoodDiary.Application.Runtime", "FoodDiary.Audit.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.ContentReports.Application", "FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dietologist.Application", "FoodDiary.Modules.Dietologist.Application.Abstractions", "FoodDiary.Modules.Hydration.Application", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Lessons.Application", "FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Domain", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Users.Infrastructure"],
            ["FoodDiary.Modules.Users.Domain.Tests"] = ["FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"],
            ["FoodDiary.Modules.Users.Infrastructure.IntegrationTests"] = ["FoodDiary.Modules.Users.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Infrastructure", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Wearables.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Application.Wearables", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Wearables.Application.Abstractions"],
            ["FoodDiary.Modules.Wearables.Domain.Tests"] = ["FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Wearables.Domain"],
            ["FoodDiary.Modules.Wearables.Infrastructure.IntegrationTests"] = ["FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Wearables.Infrastructure", "FoodDiary.Testing"],
            ["FoodDiary.Modules.Wearables.Infrastructure.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Wearables.Application.Abstractions", "FoodDiary.Modules.Wearables.Domain", "FoodDiary.Modules.Wearables.Infrastructure"],
            ["FoodDiary.Modules.WeeklyCheckIn.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.Dashboard.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.WeeklyCheckIn.Application"],
            ["FoodDiary.Modules.WeeklyGoals.Application.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Application", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.WeeklyGoals.Application"],
            ["FoodDiary.Modules.WeeklyGoals.Domain.Tests"] = ["FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.WeeklyGoals.Domain"],
            ["FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Initializer", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.WeeklyGoals.Infrastructure", "FoodDiary.Testing"],
            ["FoodDiary.Presentation.Api.Tests"] = ["FoodDiary.Application.Billing", "FoodDiary.Application.BodyMetrics", "FoodDiary.Application.Contracts", "FoodDiary.Application.Cycles", "FoodDiary.Application.Exercises", "FoodDiary.Application.Favorites", "FoodDiary.Application.Images", "FoodDiary.Application.MealPlanning", "FoodDiary.Application.Notifications", "FoodDiary.Application.RecipeCommunity", "FoodDiary.Application.Usda", "FoodDiary.Application.Wearables", "FoodDiary.Authentication.Contracts", "FoodDiary.Domain.Primitives", "FoodDiary.Modules.Admin.Application", "FoodDiary.Modules.Admin.Application.Abstractions", "FoodDiary.Modules.Ai.Application", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Billing.Application.Abstractions", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.ContentReports.Application", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.DailyAdvices.Application", "FoodDiary.Modules.Dashboard.Application", "FoodDiary.Modules.Dietologist.Application", "FoodDiary.Modules.Dietologist.Presentation.Contracts", "FoodDiary.Modules.Export.Application", "FoodDiary.Modules.Fasting.Application", "FoodDiary.Modules.Fasting.Application.Abstractions", "FoodDiary.Modules.Fasting.Contracts", "FoodDiary.Modules.Gamification.Application", "FoodDiary.Modules.Hydration.Application", "FoodDiary.Modules.Identity.Application", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Lessons.Application", "FoodDiary.Modules.Marketing.Application.Abstractions", "FoodDiary.Modules.Meals.Application", "FoodDiary.Modules.OpenFoodFacts.Application", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Statistics.Application", "FoodDiary.Modules.Tdee.Application", "FoodDiary.Modules.Usda.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.WeeklyCheckIn.Application", "FoodDiary.Modules.WeeklyGoals.Application", "FoodDiary.Presentation.Api"],
            ["FoodDiary.Results.Tests"] = ["FoodDiary.Results"],
            ["FoodDiary.Telegram.Bot.Tests"] = ["FoodDiary.Telegram.Bot"],
            ["FoodDiary.Testing"] = [],
            ["FoodDiary.Web.Api.IntegrationTests"] = ["FoodDiary.Authentication.Contracts", "FoodDiary.Domain.Primitives", "FoodDiary.Infrastructure", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Images.Application.Abstractions", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Users.Domain", "FoodDiary.Presentation.Api", "FoodDiary.Testing", "FoodDiary.Web.Api"],
            ["FoodDiary.Web.Api.Tests"] = ["FoodDiary.Application.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Modules.Images.Infrastructure", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Presentation.Api", "FoodDiary.Web.Api"],
        };

    [Fact]
    public void AllProductionProjects_AreCoveredByDependencyMatrix() {
        string[] actualProjects = [.. ProjectReferenceReader.ReadProductionProjectNames()];
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
                WithExpectedModulePresentationReferences(projectName, expectedReferences),
                actualReferences);
        }
    }

    [Fact]
    public void AllTestProjects_AreCoveredByDependencyMatrix() {
        IReadOnlyList<string> actualProjects = ProjectReferenceReader.ReadTestProjectNames();
        string[] expectedProjects = [.. AllowedTestProjectReferences.Keys
            .Concat(ModulePresentationTestProjectNames)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(expectedProjects, actualProjects);
    }

    [Fact]
    public void ModulePresentationTestProjects_ReferenceOnlyTheirOwnedPresentationAssembly() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadTestProjectReferences();

        foreach (string testProjectName in ModulePresentationTestProjectNames) {
            Assert.True(
                actualReferencesByProject.TryGetValue(testProjectName, out string[]? actualReferences),
                $"Test project '{testProjectName}' is missing from discovered test projects.");

            string expectedPresentationProject = testProjectName[..^".Tests".Length];
            Assert.Equal([expectedPresentationProject], actualReferences);
        }
    }

    [Fact]
    public void TestProjectReferences_MatchDependencyMatrix() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadTestProjectReferences();

        foreach ((string? projectName, string[]? expectedReferences) in AllowedTestProjectReferences) {
            Assert.True(
                actualReferencesByProject.TryGetValue(projectName, out string[]? actualReferences),
                $"Test project '{projectName}' is missing from discovered test projects.");

            Assert.Equal(
                WithExpectedModulePresentationReferences(projectName, expectedReferences),
                actualReferences);
        }
    }

    [Fact]
    public void CoreProjects_ReferenceMailBoundedContextsOnlyThroughAllowedClientProjects() {
        IReadOnlyDictionary<string, string[]> actualReferencesByProject = ProjectReferenceReader.ReadProductionProjectReferences();
        string[] coreProjects = [.. actualReferencesByProject.Keys
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal))
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))];

        var allowedMailClientOwners = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["FoodDiary.MailInbox.Client"] = "FoodDiary.Modules.Admin.Infrastructure",
            ["FoodDiary.MailRelay.Client"] = "FoodDiary.Email.MailRelay",
        };

        string[] violations = [.. coreProjects
            .SelectMany(projectName => actualReferencesByProject[projectName]
                .Where(static reference => reference.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal) ||
                                           reference.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))
                .Where(reference => !allowedMailClientOwners.TryGetValue(reference, out string? owner) ||
                                    !string.Equals(projectName, owner, StringComparison.Ordinal))
                .Select(reference => $"{projectName} -> {reference}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void CoreProjectSource_ReferencesMailBoundedContextsOnlyFromIntegrations() {
        string[] coreSourceRoots = [.. ProjectReferenceReader.ReadProductionProjectNames()
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailRelay.", StringComparison.Ordinal))
            .Where(static projectName => !projectName.StartsWith("FoodDiary.MailInbox.", StringComparison.Ordinal))
            .Where(static projectName => !string.Equals(projectName, "FoodDiary.Email.MailRelay", StringComparison.Ordinal))
            .Where(static projectName => !string.Equals(projectName, "FoodDiary.Modules.Admin.Infrastructure", StringComparison.Ordinal))
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

    private static string[] WithExpectedModulePresentationReferences(string projectName, IEnumerable<string> references) {
        IEnumerable<string> expected = projectName is
            "FoodDiary.Web.Api" or
            "FoodDiary.Presentation.Api.Tests" or
            "FoodDiary.Web.Api.Tests" or
            "FoodDiary.Web.Api.IntegrationTests"
                ? references.Concat(ModulePresentationProjectNames)
                : references;

        return [.. expected.Order(StringComparer.Ordinal)];
    }
}

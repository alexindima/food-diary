using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Presentation.Api.Features.Consumptions.Responses;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Features.Goals.Responses;
using FoodDiary.Presentation.Api.Features.Lessons.Responses;
using FoodDiary.Presentation.Api.Features.MealPlans.Responses;
using FoodDiary.Presentation.Api.Features.Usda.Responses;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class HttpResponseContractCoverageTests {
    [Fact]
    public void GoalsHttpResponse_ExposesAllGoalFields() {
        var response = new GoalsHttpResponse(
            DailyCalorieTarget: 2000,
            ProteinTarget: 120,
            FatTarget: 70,
            CarbTarget: 220,
            FiberTarget: 30,
            WaterGoal: 2500,
            DesiredWeight: 75,
            DesiredWaist: 82,
            CalorieCyclingEnabled: true,
            MondayCalories: 1900,
            TuesdayCalories: 1950,
            WednesdayCalories: 2000,
            ThursdayCalories: 2050,
            FridayCalories: 2100,
            SaturdayCalories: 2150,
            SundayCalories: 2200);

        Assert.Multiple(
            () => Assert.Equal(2000, response.DailyCalorieTarget),
            () => Assert.Equal(120, response.ProteinTarget),
            () => Assert.Equal(70, response.FatTarget),
            () => Assert.Equal(220, response.CarbTarget),
            () => Assert.Equal(30, response.FiberTarget),
            () => Assert.Equal(2500, response.WaterGoal),
            () => Assert.Equal(75, response.DesiredWeight),
            () => Assert.Equal(82, response.DesiredWaist),
            () => Assert.True(response.CalorieCyclingEnabled),
            () => Assert.Equal(1900, response.MondayCalories),
            () => Assert.Equal(1950, response.TuesdayCalories),
            () => Assert.Equal(2000, response.WednesdayCalories),
            () => Assert.Equal(2050, response.ThursdayCalories),
            () => Assert.Equal(2100, response.FridayCalories),
            () => Assert.Equal(2150, response.SaturdayCalories),
            () => Assert.Equal(2200, response.SundayCalories));
    }

    [Fact]
    public void ConsumptionAiResponses_ExposeSessionAndItemFields() {
        var sessionId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var item = new ConsumptionAiItemHttpResponse(
            Guid.NewGuid(), sessionId, "Toast", "toast-local", 1.5, "slice", 120, 4, 3, 20, 2, 0, 0.91, "Accepted");
        var session = new ConsumptionAiSessionHttpResponse(
            sessionId, mealId, Guid.NewGuid(), "https://example.test/ai.webp", "Vision", "Reviewed", DateTime.UtcNow, "ok", [item]);

        ConsumptionAiItemHttpResponse responseItem = Assert.Single(session.Items);

        Assert.Multiple(
            () => Assert.Equal(sessionId, session.Id),
            () => Assert.Equal(mealId, session.ConsumptionId),
            () => Assert.NotNull(session.ImageAssetId),
            () => Assert.Equal("https://example.test/ai.webp", session.ImageUrl),
            () => Assert.Equal("Vision", session.Source),
            () => Assert.Equal("Reviewed", session.Status),
            () => Assert.Equal("ok", session.Notes),
            () => Assert.Same(item, responseItem),
            () => Assert.Equal(sessionId, item.SessionId),
            () => Assert.Equal("Toast", item.NameEn),
            () => Assert.Equal("toast-local", item.NameLocal),
            () => Assert.Equal(1.5, item.Amount),
            () => Assert.Equal("slice", item.Unit),
            () => Assert.Equal(120, item.Calories),
            () => Assert.Equal(4, item.Proteins),
            () => Assert.Equal(3, item.Fats),
            () => Assert.Equal(20, item.Carbs),
            () => Assert.Equal(2, item.Fiber),
            () => Assert.Equal(0, item.Alcohol),
            () => Assert.Equal(0.91, item.Confidence),
            () => Assert.Equal("Accepted", item.Resolution));
    }

    [Fact]
    public void AdminResponses_ExposeAuditAndMailFields() {
        DateTime now = DateTime.UtcNow;
        DateTimeOffset receivedAt = DateTimeOffset.UtcNow;
        var mailDetails = new AdminMailInboxMessageDetailsHttpResponse(
            Guid.NewGuid(), "message-id", "from@example.com", ["to@example.com"], "Subject", "Text", "<p>Html</p>", "raw", "general", "received", receivedAt, receivedAt);
        var login = new AdminUserLoginEventHttpResponse(
            Guid.NewGuid(), Guid.NewGuid(), "user@example.com", "password", "127.0.0.1", "agent", "Chrome", "1", "Windows", "Desktop", now);
        var report = new AdminContentReportHttpResponse(Guid.NewGuid(), Guid.NewGuid(), "Recipe", Guid.NewGuid(), "Spam", "Pending", AdminNote: "note", now, ReviewedAtUtc: now);
        var template = new AdminEmailTemplateHttpResponse(Guid.NewGuid(), "welcome", "en", "Subject", "<p>Body</p>", "Body", IsActive: true, now, UpdatedOnUtc: now);
        var prompt = new AdminAiPromptHttpResponse(Guid.NewGuid(), "key", "en", "Prompt", 2, IsActive: true, now, UpdatedOnUtc: now);
        var audit = new AdminUserRoleAuditEventHttpResponse(Guid.NewGuid(), Guid.NewGuid(), "Admin", "Added", Guid.NewGuid(), "actor@example.com", "manual", now);
        var session = new AdminImpersonationSessionHttpResponse(Guid.NewGuid(), Guid.NewGuid(), "actor@example.com", Guid.NewGuid(), "target@example.com", "support", "ip", "agent", now);
        var mailSummary = new AdminMailInboxMessageSummaryHttpResponse(Guid.NewGuid(), "from@example.com", ["to@example.com"], "Subject", "general", "received", receivedAt, receivedAt);
        var impersonationStart = new AdminImpersonationStartHttpResponse("token", Guid.NewGuid(), "target@example.com", Guid.NewGuid(), "support");

        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, mailDetails.Id),
            () => Assert.Equal("message-id", mailDetails.MessageId),
            () => Assert.Equal("from@example.com", mailDetails.FromAddress),
            () => Assert.Equal(["to@example.com"], mailDetails.ToRecipients),
            () => Assert.Equal("Subject", mailDetails.Subject),
            () => Assert.Equal("Text", mailDetails.TextBody),
            () => Assert.Equal("<p>Html</p>", mailDetails.HtmlBody),
            () => Assert.Equal("raw", mailDetails.RawMime),
            () => Assert.Equal("general", mailDetails.Category),
            () => Assert.Equal("received", mailDetails.Status),
            () => Assert.Equal(receivedAt, mailDetails.ReadAtUtc),
            () => Assert.Equal(receivedAt, mailDetails.ReceivedAtUtc),
            () => Assert.Equal("user@example.com", login.UserEmail),
            () => Assert.Equal("password", login.AuthProvider),
            () => Assert.Equal("127.0.0.1", login.MaskedIpAddress),
            () => Assert.Equal("agent", login.UserAgent),
            () => Assert.Equal("Chrome", login.BrowserName),
            () => Assert.Equal("1", login.BrowserVersion),
            () => Assert.Equal("Windows", login.OperatingSystem),
            () => Assert.Equal("Desktop", login.DeviceType),
            () => Assert.Equal(now, login.LoggedInAtUtc),
            () => Assert.NotEqual(Guid.Empty, report.Id),
            () => Assert.NotEqual(Guid.Empty, report.ReporterId),
            () => Assert.Equal("Recipe", report.TargetType),
            () => Assert.NotEqual(Guid.Empty, report.TargetId),
            () => Assert.Equal("Spam", report.Reason),
            () => Assert.Equal("Pending", report.Status),
            () => Assert.Equal("note", report.AdminNote),
            () => Assert.Equal(now, report.CreatedAtUtc),
            () => Assert.Equal(now, report.ReviewedAtUtc),
            () => Assert.NotEqual(Guid.Empty, template.Id),
            () => Assert.Equal("welcome", template.Key),
            () => Assert.Equal("en", template.Locale),
            () => Assert.Equal("Subject", template.Subject),
            () => Assert.Equal("<p>Body</p>", template.HtmlBody),
            () => Assert.Equal("Body", template.TextBody),
            () => Assert.True(template.IsActive),
            () => Assert.Equal(now, template.CreatedOnUtc),
            () => Assert.Equal(now, template.UpdatedOnUtc),
            () => Assert.NotEqual(Guid.Empty, prompt.Id),
            () => Assert.Equal("key", prompt.Key),
            () => Assert.Equal("en", prompt.Locale),
            () => Assert.Equal("Prompt", prompt.PromptText),
            () => Assert.Equal(2, prompt.Version),
            () => Assert.True(prompt.IsActive),
            () => Assert.Equal(now, prompt.CreatedOnUtc),
            () => Assert.Equal(now, prompt.UpdatedOnUtc),
            () => Assert.NotEqual(Guid.Empty, audit.Id),
            () => Assert.NotEqual(Guid.Empty, audit.UserId),
            () => Assert.Equal("Admin", audit.RoleName),
            () => Assert.Equal("Added", audit.Action),
            () => Assert.NotNull(audit.ActorUserId),
            () => Assert.Equal("actor@example.com", audit.ActorEmail),
            () => Assert.Equal("manual", audit.Source),
            () => Assert.Equal(now, audit.OccurredAtUtc),
            () => Assert.NotEqual(Guid.Empty, session.Id),
            () => Assert.NotEqual(Guid.Empty, session.ActorUserId),
            () => Assert.Equal("actor@example.com", session.ActorEmail),
            () => Assert.NotEqual(Guid.Empty, session.TargetUserId),
            () => Assert.Equal("target@example.com", session.TargetEmail),
            () => Assert.Equal("support", session.Reason),
            () => Assert.Equal("ip", session.ActorIpAddress),
            () => Assert.Equal("agent", session.ActorUserAgent),
            () => Assert.Equal(now, session.StartedAtUtc),
            () => Assert.NotEqual(Guid.Empty, mailSummary.Id),
            () => Assert.Equal("from@example.com", mailSummary.FromAddress),
            () => Assert.Equal(["to@example.com"], mailSummary.ToRecipients),
            () => Assert.Equal("Subject", mailSummary.Subject),
            () => Assert.Equal("general", mailSummary.Category),
            () => Assert.Equal("received", mailSummary.Status),
            () => Assert.Equal(receivedAt, mailSummary.ReadAtUtc),
            () => Assert.Equal(receivedAt, mailSummary.ReceivedAtUtc),
            () => Assert.Equal("token", impersonationStart.AccessToken),
            () => Assert.NotEqual(Guid.Empty, impersonationStart.TargetUserId),
            () => Assert.Equal("target@example.com", impersonationStart.TargetEmail),
            () => Assert.NotEqual(Guid.Empty, impersonationStart.ActorUserId),
            () => Assert.Equal("support", impersonationStart.Reason));
    }

    [Fact]
    public void DietologistAndNutritionResponses_ExposeAllFields() {
        DateTime now = DateTime.UtcNow;
        var permissions = new DietologistPermissionsHttpResponse(
            ShareMeals: true,
            ShareStatistics: false,
            ShareWeight: true,
            ShareWaist: false,
            ShareGoals: true,
            ShareHydration: false,
            ShareProfile: true,
            ShareFasting: false);
        var client = new ClientSummaryHttpResponse(Guid.NewGuid(), "client@example.com", "First", "Last", "image", now.Date, "female", 170, "active", permissions, now);
        var nutrition = new FoodNutritionItemHttpResponse("Apple", 100, "g", 52, 0.3m, 0.2m, 14, 2.4m, 0);

        Assert.Multiple(
            () => Assert.Equal("client@example.com", client.Email),
            () => Assert.Equal("First", client.FirstName),
            () => Assert.Equal("Last", client.LastName),
            () => Assert.Equal("image", client.ProfileImage),
            () => Assert.Equal(now.Date, client.BirthDate),
            () => Assert.Equal("female", client.Gender),
            () => Assert.Equal(170, client.Height),
            () => Assert.Equal("active", client.ActivityLevel),
            () => Assert.Same(permissions, client.Permissions),
            () => Assert.Equal(now, client.AcceptedAtUtc),
            () => Assert.True(permissions.ShareMeals),
            () => Assert.False(permissions.ShareStatistics),
            () => Assert.True(permissions.ShareWeight),
            () => Assert.False(permissions.ShareWaist),
            () => Assert.True(permissions.ShareGoals),
            () => Assert.False(permissions.ShareHydration),
            () => Assert.True(permissions.ShareProfile),
            () => Assert.False(permissions.ShareFasting),
            () => Assert.Equal("Apple", nutrition.Name),
            () => Assert.Equal(100, nutrition.Amount),
            () => Assert.Equal("g", nutrition.Unit),
            () => Assert.Equal(52, nutrition.Calories),
            () => Assert.Equal(0.3m, nutrition.Protein),
            () => Assert.Equal(0.2m, nutrition.Fat),
            () => Assert.Equal(14, nutrition.Carbs),
            () => Assert.Equal(2.4m, nutrition.Fiber),
            () => Assert.Equal(0, nutrition.Alcohol));
    }

    [Fact]
    public void WeeklyCheckInAndUsdaResponses_ExposeAllFields() {
        var summary = new WeekSummaryHttpResponse(14000, 2000, 120, 70, 220, 21, 7, 80, 79, 90, 89, 14000, 2000);
        var trend = new WeekTrendHttpResponse(100, 5, -2, 10, -1, -0.5, 500, 2);
        var daily = new DailyMicronutrientHttpResponse(1, "Iron", "mg", 12, 18, 66.7);
        var micro = new MicronutrientHttpResponse(2, "Vitamin C", "mg", 30, 90, 33.3);
        var portion = new UsdaFoodPortionHttpResponse(3, 1, "cup", 240, "1 cup", "chopped");

        Assert.Multiple(
            () => Assert.Equal(14000, summary.TotalCalories),
            () => Assert.Equal(2000, summary.AvgDailyCalories),
            () => Assert.Equal(120, summary.AvgProteins),
            () => Assert.Equal(70, summary.AvgFats),
            () => Assert.Equal(220, summary.AvgCarbs),
            () => Assert.Equal(21, summary.MealsLogged),
            () => Assert.Equal(7, summary.DaysLogged),
            () => Assert.Equal(80, summary.WeightStart),
            () => Assert.Equal(79, summary.WeightEnd),
            () => Assert.Equal(90, summary.WaistStart),
            () => Assert.Equal(89, summary.WaistEnd),
            () => Assert.Equal(14000, summary.TotalHydrationMl),
            () => Assert.Equal(2000, summary.AvgDailyHydrationMl),
            () => Assert.Equal(100, trend.CalorieChange),
            () => Assert.Equal(5, trend.ProteinChange),
            () => Assert.Equal(-2, trend.FatChange),
            () => Assert.Equal(10, trend.CarbChange),
            () => Assert.Equal(-1, trend.WeightChange),
            () => Assert.Equal(-0.5, trend.WaistChange),
            () => Assert.Equal(500, trend.HydrationChange),
            () => Assert.Equal(2, trend.MealsLoggedChange),
            () => Assert.Equal("Iron", daily.Name),
            () => Assert.Equal(18, daily.DailyValue),
            () => Assert.Equal("Vitamin C", micro.Name),
            () => Assert.Equal(30, micro.AmountPer100g),
            () => Assert.Equal(3, portion.Id),
            () => Assert.Equal("cup", portion.MeasureUnitName),
            () => Assert.Equal(240, portion.GramWeight),
            () => Assert.Equal("1 cup", portion.PortionDescription),
            () => Assert.Equal("chopped", portion.Modifier));
    }

    [Fact]
    public void MealPlanCycleAndLessonResponses_ExposeAllFields() {
        DateTime date = DateTime.UtcNow.Date;
        var meal = new MealPlanMealHttpResponse(Guid.NewGuid(), "Breakfast", Guid.NewGuid(), "Oats", 2, 350, 20, 10, 40);
        var day = new MealPlanDayHttpResponse(Guid.NewGuid(), 1, [meal]);
        var plan = new MealPlanHttpResponse(Guid.NewGuid(), "Plan", "Description", "Balanced", 7, 2000, IsCurated: true, [day]);
        var bleeding = new BleedingEntryHttpResponse(Guid.NewGuid(), Guid.NewGuid(), date, 1, 2, 3, "notes");
        var cycleNutrition = new CycleNutritionSummaryHttpResponse(date, date.AddDays(7), 7, 6, 3, 1800, 1900, 24, 28, 2.5, HasEnoughNutritionData: true);
        var lessonSummary = new LessonSummaryHttpResponse(Guid.NewGuid(), "Title", "Summary", "Basics", "Easy", 5, IsRead: true);
        var lessonDetail = new LessonDetailHttpResponse(Guid.NewGuid(), "Title", "Content", "Summary", "Basics", "Easy", 5, IsRead: true);
        MealPlanDayHttpResponse responseDay = Assert.Single(plan.Days);
        MealPlanMealHttpResponse responseMeal = Assert.Single(day.Meals);

        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, plan.Id),
            () => Assert.Equal("Plan", plan.Name),
            () => Assert.Equal("Description", plan.Description),
            () => Assert.Equal("Balanced", plan.DietType),
            () => Assert.Equal(7, plan.DurationDays),
            () => Assert.Equal(2000, plan.TargetCaloriesPerDay),
            () => Assert.True(plan.IsCurated),
            () => Assert.Same(day, responseDay),
            () => Assert.NotEqual(Guid.Empty, day.Id),
            () => Assert.Equal(1, day.DayNumber),
            () => Assert.Same(meal, responseMeal),
            () => Assert.NotEqual(Guid.Empty, meal.Id),
            () => Assert.Equal("Breakfast", meal.MealType),
            () => Assert.NotEqual(Guid.Empty, meal.RecipeId),
            () => Assert.Equal("Oats", meal.RecipeName),
            () => Assert.Equal(2, meal.Servings),
            () => Assert.Equal(350, meal.Calories),
            () => Assert.Equal(20, meal.Proteins),
            () => Assert.Equal(10, meal.Fats),
            () => Assert.Equal(40, meal.Carbs),
            () => Assert.NotEqual(Guid.Empty, bleeding.Id),
            () => Assert.NotEqual(Guid.Empty, bleeding.CycleProfileId),
            () => Assert.Equal(date, bleeding.Date),
            () => Assert.Equal(1, bleeding.Type),
            () => Assert.Equal(2, bleeding.Flow),
            () => Assert.Equal(3, bleeding.PainImpact),
            () => Assert.Equal("notes", bleeding.Notes),
            () => Assert.Equal(date, cycleNutrition.DateFrom),
            () => Assert.Equal(date.AddDays(7), cycleNutrition.DateTo),
            () => Assert.Equal(7, cycleNutrition.LoggedCycleDays),
            () => Assert.Equal(6, cycleNutrition.DaysWithMeals),
            () => Assert.Equal(3, cycleNutrition.BleedingDays),
            () => Assert.Equal(1800, cycleNutrition.AverageCaloriesOnBleedingDays),
            () => Assert.Equal(1900, cycleNutrition.AverageCaloriesOnNonBleedingCycleDays),
            () => Assert.Equal(24, cycleNutrition.AverageFiberOnBleedingDays),
            () => Assert.Equal(28, cycleNutrition.AverageFiberOnNonBleedingCycleDays),
            () => Assert.Equal(2.5, cycleNutrition.AveragePainImpactOnDaysWithMeals),
            () => Assert.True(cycleNutrition.HasEnoughNutritionData),
            () => Assert.NotEqual(Guid.Empty, lessonSummary.Id),
            () => Assert.Equal("Title", lessonSummary.Title),
            () => Assert.Equal("Summary", lessonSummary.Summary),
            () => Assert.Equal("Basics", lessonSummary.Category),
            () => Assert.Equal("Easy", lessonSummary.Difficulty),
            () => Assert.Equal(5, lessonSummary.EstimatedReadMinutes),
            () => Assert.True(lessonSummary.IsRead),
            () => Assert.NotEqual(Guid.Empty, lessonDetail.Id),
            () => Assert.Equal("Title", lessonDetail.Title),
            () => Assert.Equal("Content", lessonDetail.Content),
            () => Assert.Equal("Summary", lessonDetail.Summary),
            () => Assert.Equal("Basics", lessonDetail.Category),
            () => Assert.Equal("Easy", lessonDetail.Difficulty),
            () => Assert.Equal(5, lessonDetail.EstimatedReadMinutes),
            () => Assert.True(lessonDetail.IsRead));
    }
}

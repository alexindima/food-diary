import { HttpClient } from '@angular/common/http';
import { ModuleWithProviders, NgModule, Optional, SkipSelf } from '@angular/core';
import { Configuration } from './configuration';

import { AiSdk } from './api/ai.service';
import { AuthSessionsSdk } from './api/auth-sessions.service';
import { AuthSdk } from './api/auth.service';
import { BillingSdk } from './api/billing.service';
import { ClientTasksSdk } from './api/client-tasks.service';
import { CyclesSdk } from './api/cycles.service';
import { DashboardSdk } from './api/dashboard.service';
import { DietologistSdk } from './api/dietologist.service';
import { ExercisesSdk } from './api/exercises.service';
import { ExportSdk } from './api/export.service';
import { FastingSdk } from './api/fasting.service';
import { FavoriteMealsSdk } from './api/favorite-meals.service';
import { FavoriteProductsSdk } from './api/favorite-products.service';
import { FavoriteRecipesSdk } from './api/favorite-recipes.service';
import { GamificationSdk } from './api/gamification.service';
import { GoalsSdk } from './api/goals.service';
import { HydrationSdk } from './api/hydration.service';
import { ImagesSdk } from './api/images.service';
import { LessonsSdk } from './api/lessons.service';
import { MealPlansSdk } from './api/meal-plans.service';
import { MealsSdk } from './api/meals.service';
import { NotificationsSdk } from './api/notifications.service';
import { OpenFoodFactsSdk } from './api/open-food-facts.service';
import { ProductsSdk } from './api/products.service';
import { RecipesSdk } from './api/recipes.service';
import { RecommendationsSdk } from './api/recommendations.service';
import { ReportsSdk } from './api/reports.service';
import { ShoppingListsSdk } from './api/shopping-lists.service';
import { StatisticsSdk } from './api/statistics.service';
import { TdeeSdk } from './api/tdee.service';
import { TelegramAuthSdk } from './api/telegram-auth.service';
import { TelegramBotSdk } from './api/telegram-bot.service';
import { UsdaSdk } from './api/usda.service';
import { UsersSdk } from './api/users.service';
import { WaistEntriesSdk } from './api/waist-entries.service';
import { WearablesSdk } from './api/wearables.service';
import { WeeklyCheckInSdk } from './api/weekly-check-in.service';
import { WeeklyGoalsSdk } from './api/weekly-goals.service';
import { WeightEntriesSdk } from './api/weight-entries.service';

@NgModule({
    imports: [],
    declarations: [],
    exports: [],
    providers: [
        AiSdk,
        AuthSdk,
        AuthSessionsSdk,
        BillingSdk,
        ClientTasksSdk,
        CyclesSdk,
        DashboardSdk,
        DietologistSdk,
        ExercisesSdk,
        ExportSdk,
        FastingSdk,
        FavoriteMealsSdk,
        FavoriteProductsSdk,
        FavoriteRecipesSdk,
        GamificationSdk,
        GoalsSdk,
        HydrationSdk,
        ImagesSdk,
        LessonsSdk,
        MealPlansSdk,
        MealsSdk,
        NotificationsSdk,
        OpenFoodFactsSdk,
        ProductsSdk,
        RecipesSdk,
        RecommendationsSdk,
        ReportsSdk,
        ShoppingListsSdk,
        StatisticsSdk,
        TdeeSdk,
        TelegramAuthSdk,
        TelegramBotSdk,
        UsdaSdk,
        UsersSdk,
        WaistEntriesSdk,
        WearablesSdk,
        WeeklyCheckInSdk,
        WeeklyGoalsSdk,
        WeightEntriesSdk,
    ],
})
export class ApiModule {
    public static forRoot(configurationFactory: () => Configuration): ModuleWithProviders<ApiModule> {
        return {
            ngModule: ApiModule,
            providers: [{ provide: Configuration, useFactory: configurationFactory }],
        };
    }

    constructor(@Optional() @SkipSelf() parentModule: ApiModule, @Optional() http: HttpClient) {
        if (parentModule) {
            throw new Error('ApiModule is already loaded. Import in your base AppModule only.');
        }
        if (!http) {
            throw new Error(
                'You need to import the HttpClientModule in your AppModule! \n' +
                    'See also https://github.com/angular/angular/issues/20575',
            );
        }
    }
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

type SeoPageSlug =
    | 'food-diary'
    | 'calorie-counter'
    | 'meal-planner'
    | 'macro-tracker'
    | 'intermittent-fasting'
    | 'meal-tracker'
    | 'weight-loss-app'
    | 'dietologist-collaboration'
    | 'nutrition-planner'
    | 'weight-tracker'
    | 'body-progress-tracker'
    | 'shopping-list-for-meal-planning'
    | 'nutrition-tracker'
    | 'food-log'
    | 'protein-tracker'
    | 'meal-prep-planner';

interface SeoLandingPageData {
    baseKey: string;
    featureKeys: readonly string[];
    stepKeys: readonly string[];
    faqKeys: readonly string[];
    relatedPaths: readonly SeoPageSlug[];
}

const PAGE_LABEL_KEYS: Record<SeoPageSlug, string> = {
    'food-diary': 'SEO.FOOD_DIARY_PAGE',
    'calorie-counter': 'SEO.CALORIE_COUNTER_PAGE',
    'meal-planner': 'SEO.MEAL_PLANNER_PAGE',
    'macro-tracker': 'SEO.MACRO_TRACKER_PAGE',
    'intermittent-fasting': 'SEO.INTERMITTENT_FASTING_PAGE',
    'meal-tracker': 'SEO.MEAL_TRACKER_PAGE',
    'weight-loss-app': 'SEO.WEIGHT_LOSS_APP_PAGE',
    'dietologist-collaboration': 'SEO.DIETOLOGIST_COLLABORATION_PAGE',
    'nutrition-planner': 'SEO.NUTRITION_PLANNER_PAGE',
    'weight-tracker': 'SEO.WEIGHT_TRACKER_PAGE',
    'body-progress-tracker': 'SEO.BODY_PROGRESS_TRACKER_PAGE',
    'shopping-list-for-meal-planning': 'SEO.SHOPPING_LIST_MEAL_PLANNER_PAGE',
    'nutrition-tracker': 'SEO.NUTRITION_TRACKER_PAGE',
    'food-log': 'SEO.FOOD_LOG_PAGE',
    'protein-tracker': 'SEO.PROTEIN_TRACKER_PAGE',
    'meal-prep-planner': 'SEO.MEAL_PREP_PLANNER_PAGE',
};

@Component({
    selector: 'fd-seo-landing-page',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent],
    templateUrl: './seo-landing-page.component.html',
    styleUrl: './seo-landing-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeoLandingPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly page = this.route.snapshot.data['seoPage'] as SeoLandingPageData;

    protected readonly baseKey = this.page.baseKey;
    protected readonly featureKeys = this.page.featureKeys;
    protected readonly stepKeys = this.page.stepKeys;
    protected readonly faqKeys = this.page.faqKeys;
    protected readonly relatedPages = this.page.relatedPaths.map(path => ({
        path: `/${path}`,
        labelKey: PAGE_LABEL_KEYS[path],
    }));
}

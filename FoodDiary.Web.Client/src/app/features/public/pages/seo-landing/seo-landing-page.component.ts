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

interface SeoLandingTextKeys {
    eyebrowKey: string;
    titleKey: string;
    subtitleKey: string;
}

interface SeoLandingHeroKeys extends SeoLandingTextKeys {
    primaryActionKey: string;
    secondaryActionKey: string;
    chipKeys: readonly string[];
    panel: {
        eyebrowKey: string;
        titleKey: string;
        textKey: string;
        itemKeys: readonly string[];
    };
}

interface SeoLandingContentItemKeys {
    key: string;
    titleKey: string;
    textKey: string;
}

interface SeoLandingFaqItemKeys {
    key: string;
    questionKey: string;
    answerKey: string;
}

interface SeoLandingCtaKeys {
    titleKey: string;
    subtitleKey: string;
    primaryKey: string;
    secondaryKey: string;
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
    protected readonly hero = this.createHeroKeys();
    protected readonly audience = {
        ...this.createSectionKeys('AUDIENCE'),
        items: ['FIRST', 'SECOND', 'THIRD', 'FOURTH'].map(key => this.createContentItemKeys('AUDIENCE.ITEMS', key)),
    };
    protected readonly features = {
        ...this.createSectionKeys('FEATURES'),
        items: this.page.featureKeys.map(key => this.createContentItemKeys('FEATURES.ITEMS', key)),
    };
    protected readonly steps = {
        ...this.createSectionKeys('STEPS'),
        items: this.page.stepKeys.map(key => this.createContentItemKeys('STEPS.ITEMS', key)),
    };
    protected readonly faq = {
        ...this.createSectionKeys('FAQ'),
        items: this.page.faqKeys.map(key => this.createFaqItemKeys(key)),
    };
    protected readonly cta: SeoLandingCtaKeys = {
        titleKey: this.buildKey('CTA.TITLE'),
        subtitleKey: this.buildKey('CTA.SUBTITLE'),
        primaryKey: this.buildKey('CTA.PRIMARY'),
        secondaryKey: this.buildKey('CTA.SECONDARY'),
    };
    protected readonly relatedPages = this.page.relatedPaths.map(path => ({
        path: `/${path}`,
        labelKey: PAGE_LABEL_KEYS[path],
    }));

    private createHeroKeys(): SeoLandingHeroKeys {
        return {
            eyebrowKey: this.buildKey('EYEBROW'),
            titleKey: this.buildKey('TITLE'),
            subtitleKey: this.buildKey('SUBTITLE'),
            primaryActionKey: this.buildKey('PRIMARY_ACTION'),
            secondaryActionKey: this.buildKey('SECONDARY_ACTION'),
            chipKeys: ['FREE', 'CALORIES', 'PLANNING', 'PROGRESS'].map(key => this.buildKey(`CHIPS.${key}`)),
            panel: {
                eyebrowKey: this.buildKey('PANEL.EYEBROW'),
                titleKey: this.buildKey('PANEL.TITLE'),
                textKey: this.buildKey('PANEL.TEXT'),
                itemKeys: ['FIRST', 'SECOND', 'THIRD', 'FOURTH'].map(key => this.buildKey(`PANEL.ITEMS.${key}`)),
            },
        };
    }

    private createSectionKeys(section: string): SeoLandingTextKeys {
        return {
            eyebrowKey: this.buildKey(`${section}.EYEBROW`),
            titleKey: this.buildKey(`${section}.TITLE`),
            subtitleKey: this.buildKey(`${section}.SUBTITLE`),
        };
    }

    private createContentItemKeys(section: string, key: string): SeoLandingContentItemKeys {
        return {
            key,
            titleKey: this.buildKey(`${section}.${key}.TITLE`),
            textKey: this.buildKey(`${section}.${key}.TEXT`),
        };
    }

    private createFaqItemKeys(key: string): SeoLandingFaqItemKeys {
        return {
            key,
            questionKey: this.buildKey(`FAQ.ITEMS.${key}.QUESTION`),
            answerKey: this.buildKey(`FAQ.ITEMS.${key}.ANSWER`),
        };
    }

    private buildKey(suffix: string): string {
        return `${this.baseKey}.${suffix}`;
    }
}

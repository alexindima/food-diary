import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { isRecord } from '../../../../shared/lib/unknown-value.utils';
import type {
    SeoLandingContentItemKeys,
    SeoLandingCtaKeys,
    SeoLandingFaqItemKeys,
    SeoLandingHeroKeys,
    SeoLandingPageData,
    SeoLandingTextKeys,
    SeoPageSlug,
} from './seo-landing.types';
import { SeoLandingContentSectionsComponent } from './seo-landing-content-sections.component';
import { SeoLandingFooterSectionsComponent } from './seo-landing-footer-sections.component';

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
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent, SeoLandingContentSectionsComponent, SeoLandingFooterSectionsComponent],
    templateUrl: './seo-landing-page.component.html',
    styleUrl: './seo-landing-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeoLandingPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly page = this.resolvePageData(this.route.snapshot.data['seoPage']);

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

    private resolvePageData(value: unknown): SeoLandingPageData {
        if (
            isRecord(value) &&
            typeof value['baseKey'] === 'string' &&
            Array.isArray(value['featureKeys']) &&
            Array.isArray(value['stepKeys']) &&
            Array.isArray(value['faqKeys']) &&
            Array.isArray(value['relatedPaths'])
        ) {
            return {
                baseKey: value['baseKey'],
                featureKeys: value['featureKeys'].filter(item => typeof item === 'string'),
                stepKeys: value['stepKeys'].filter(item => typeof item === 'string'),
                faqKeys: value['faqKeys'].filter(item => typeof item === 'string'),
                relatedPaths: value['relatedPaths'].filter((item): item is SeoPageSlug => this.isSeoPageSlug(item)),
            };
        }

        return {
            baseKey: 'SEO_PAGES.FOOD_DIARY',
            featureKeys: [],
            stepKeys: [],
            faqKeys: [],
            relatedPaths: [],
        };
    }

    private isSeoPageSlug(value: unknown): value is SeoPageSlug {
        return typeof value === 'string' && value in PAGE_LABEL_KEYS;
    }
}

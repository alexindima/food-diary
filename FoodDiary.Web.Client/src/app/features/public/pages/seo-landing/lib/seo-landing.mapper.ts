import { isRecord } from '../../../../../shared/lib/unknown-value.utils';
import type {
    SeoLandingContentItemKeys,
    SeoLandingFaqItemKeys,
    SeoLandingHeroKeys,
    SeoLandingPageData,
    SeoLandingTextKeys,
    SeoLandingViewModel,
    SeoPageSlug,
} from './seo-landing.types';

export const SEO_PAGE_LABEL_KEYS: Record<SeoPageSlug, string> = {
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

export function resolveSeoLandingPageData(value: unknown): SeoLandingPageData {
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
            relatedPaths: value['relatedPaths'].filter(isSeoPageSlug),
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

export function buildSeoLandingViewModel(page: SeoLandingPageData): SeoLandingViewModel {
    const buildKey = (suffix: string): string => `${page.baseKey}.${suffix}`;

    return {
        baseKey: page.baseKey,
        hero: createHeroKeys(buildKey),
        audience: {
            ...createSectionKeys(buildKey, 'AUDIENCE'),
            items: ['FIRST', 'SECOND', 'THIRD', 'FOURTH'].map(key => createContentItemKeys(buildKey, 'AUDIENCE.ITEMS', key)),
        },
        features: {
            ...createSectionKeys(buildKey, 'FEATURES'),
            items: page.featureKeys.map(key => createContentItemKeys(buildKey, 'FEATURES.ITEMS', key)),
        },
        steps: {
            ...createSectionKeys(buildKey, 'STEPS'),
            items: page.stepKeys.map(key => createContentItemKeys(buildKey, 'STEPS.ITEMS', key)),
        },
        faq: {
            ...createSectionKeys(buildKey, 'FAQ'),
            items: page.faqKeys.map(key => createFaqItemKeys(buildKey, key)),
        },
        cta: {
            titleKey: buildKey('CTA.TITLE'),
            subtitleKey: buildKey('CTA.SUBTITLE'),
            primaryKey: buildKey('CTA.PRIMARY'),
            secondaryKey: buildKey('CTA.SECONDARY'),
        },
        relatedPages: page.relatedPaths.map(path => ({
            path: `/${path}`,
            labelKey: SEO_PAGE_LABEL_KEYS[path],
        })),
    };
}

function createHeroKeys(buildKey: (suffix: string) => string): SeoLandingHeroKeys {
    return {
        eyebrowKey: buildKey('EYEBROW'),
        titleKey: buildKey('TITLE'),
        subtitleKey: buildKey('SUBTITLE'),
        primaryActionKey: buildKey('PRIMARY_ACTION'),
        secondaryActionKey: buildKey('SECONDARY_ACTION'),
        chipKeys: ['FREE', 'CALORIES', 'PLANNING', 'PROGRESS'].map(key => buildKey(`CHIPS.${key}`)),
        panel: {
            eyebrowKey: buildKey('PANEL.EYEBROW'),
            titleKey: buildKey('PANEL.TITLE'),
            textKey: buildKey('PANEL.TEXT'),
            itemKeys: ['FIRST', 'SECOND', 'THIRD', 'FOURTH'].map(key => buildKey(`PANEL.ITEMS.${key}`)),
        },
    };
}

function createSectionKeys(buildKey: (suffix: string) => string, section: string): SeoLandingTextKeys {
    return {
        eyebrowKey: buildKey(`${section}.EYEBROW`),
        titleKey: buildKey(`${section}.TITLE`),
        subtitleKey: buildKey(`${section}.SUBTITLE`),
    };
}

function createContentItemKeys(buildKey: (suffix: string) => string, section: string, key: string): SeoLandingContentItemKeys {
    return {
        key,
        titleKey: buildKey(`${section}.${key}.TITLE`),
        textKey: buildKey(`${section}.${key}.TEXT`),
    };
}

function createFaqItemKeys(buildKey: (suffix: string) => string, key: string): SeoLandingFaqItemKeys {
    return {
        key,
        questionKey: buildKey(`FAQ.ITEMS.${key}.QUESTION`),
        answerKey: buildKey(`FAQ.ITEMS.${key}.ANSWER`),
    };
}

function isSeoPageSlug(value: unknown): value is SeoPageSlug {
    return typeof value === 'string' && value in SEO_PAGE_LABEL_KEYS;
}

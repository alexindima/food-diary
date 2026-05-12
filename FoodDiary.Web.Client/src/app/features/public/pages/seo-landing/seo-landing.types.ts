export type SeoPageSlug =
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

export interface SeoLandingPageData {
    baseKey: string;
    featureKeys: readonly string[];
    stepKeys: readonly string[];
    faqKeys: readonly string[];
    relatedPaths: readonly SeoPageSlug[];
}

export interface SeoLandingTextKeys {
    eyebrowKey: string;
    titleKey: string;
    subtitleKey: string;
}

export interface SeoLandingHeroKeys extends SeoLandingTextKeys {
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

export interface SeoLandingContentItemKeys {
    key: string;
    titleKey: string;
    textKey: string;
}

export interface SeoLandingFaqItemKeys {
    key: string;
    questionKey: string;
    answerKey: string;
}

export interface SeoLandingCtaKeys {
    titleKey: string;
    subtitleKey: string;
    primaryKey: string;
    secondaryKey: string;
}

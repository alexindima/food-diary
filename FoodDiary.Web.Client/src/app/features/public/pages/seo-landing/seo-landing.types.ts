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

export type SeoLandingPageData = {
    baseKey: string;
    featureKeys: readonly string[];
    stepKeys: readonly string[];
    faqKeys: readonly string[];
    relatedPaths: readonly SeoPageSlug[];
};

export type SeoLandingTextKeys = {
    eyebrowKey: string;
    titleKey: string;
    subtitleKey: string;
};

export type SeoLandingHeroKeys = {
    primaryActionKey: string;
    secondaryActionKey: string;
    chipKeys: readonly string[];
    panel: {
        eyebrowKey: string;
        titleKey: string;
        textKey: string;
        itemKeys: readonly string[];
    };
} & SeoLandingTextKeys;

export type SeoLandingContentItemKeys = {
    key: string;
    titleKey: string;
    textKey: string;
};

export type SeoLandingFaqItemKeys = {
    key: string;
    questionKey: string;
    answerKey: string;
};

export type SeoLandingCtaKeys = {
    titleKey: string;
    subtitleKey: string;
    primaryKey: string;
    secondaryKey: string;
};

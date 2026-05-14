export type FeatureCategory = {
    icon: string;
    key: string;
    labelKey: string;
    eyebrowKey: string;
    titleKey: string;
    descriptionKey: string;
    tabId: string;
    panelId: string;
    itemKeys: FeatureItem[];
};

export type FeatureItem = {
    key: string;
    kickerKey: string;
    titleKey: string;
    descriptionKey: string;
};

export const FEATURE_CATEGORIES: FeatureCategory[] = [
    {
        key: 'TRACK',
        icon: 'restaurant_menu',
        itemKeys: createItemKeys(['LOG_MEALS', 'PRODUCT_LIBRARY', 'RECIPE_FLOW']),
    },
    {
        key: 'PLAN',
        icon: 'event_note',
        itemKeys: createItemKeys(['MEAL_PLANS', 'SHOPPING_LISTS', 'GOALS']),
    },
    {
        key: 'PROGRESS',
        icon: 'bar_chart',
        itemKeys: createItemKeys(['STATISTICS', 'BODY_HISTORY', 'WEEKLY_CHECKINS']),
    },
    {
        key: 'SPECIAL',
        icon: 'health_and_safety',
        itemKeys: createItemKeys(['FASTING', 'CYCLE_TRACKING', 'PREMIUM_AI']),
    },
    {
        key: 'MOTIVATION',
        icon: 'school',
        itemKeys: createItemKeys(['LESSONS', 'GAMIFICATION', 'PROFILE_SYNC']),
    },
].map(category => ({
    ...category,
    tabId: `features-tab-${category.key}`,
    panelId: `features-panel-${category.key}`,
    labelKey: `FEATURES.CATEGORIES.${category.key}.LABEL`,
    eyebrowKey: `FEATURES.CATEGORIES.${category.key}.EYEBROW`,
    titleKey: `FEATURES.CATEGORIES.${category.key}.TITLE`,
    descriptionKey: `FEATURES.CATEGORIES.${category.key}.DESCRIPTION`,
}));

function createItemKeys(keys: string[]): FeatureItem[] {
    return keys.map(key => ({
        key,
        kickerKey: `FEATURES.ITEMS.${key}.KICKER`,
        titleKey: `FEATURES.ITEMS.${key}.TITLE`,
        descriptionKey: `FEATURES.ITEMS.${key}.DESCRIPTION`,
    }));
}

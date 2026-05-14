export const DIETOLOGIST_PROMO_WORKFLOW_STEPS = ['INVITE', 'SHARE', 'ADJUST'].map(key => ({
    key,
    titleKey: `LANDING_DIETOLOGIST.STEPS.${key}.TITLE`,
    textKey: `LANDING_DIETOLOGIST.STEPS.${key}.TEXT`,
}));

export const DIETOLOGIST_PROMO_PERMISSIONS = ['MEALS', 'STATISTICS', 'WEIGHT', 'GOALS', 'FASTING'].map(key => ({
    key,
    labelKey: `LANDING_DIETOLOGIST.PERMISSIONS.${key}`,
}));

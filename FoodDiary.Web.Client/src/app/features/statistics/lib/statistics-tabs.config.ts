import type { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

export const STATISTICS_RANGE_TABS: FdUiTab[] = [
    { value: 'week', labelKey: 'STATISTICS.RANGES.WEEK' },
    { value: 'month', labelKey: 'STATISTICS.RANGES.MONTH' },
    { value: 'year', labelKey: 'STATISTICS.RANGES.YEAR' },
    { value: 'custom', labelKey: 'STATISTICS.RANGES.CUSTOM' },
];

export const STATISTICS_NUTRITION_TABS: FdUiTab[] = [
    { value: 'calories', labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES' },
    { value: 'macros', labelKey: 'STATISTICS.NUTRITION_TABS.MACROS' },
    { value: 'distribution', labelKey: 'STATISTICS.NUTRITION_TABS.DISTRIBUTION' },
];

export const STATISTICS_BODY_TABS: FdUiTab[] = [
    { value: 'weight', labelKey: 'STATISTICS.BODY_TABS.WEIGHT' },
    { value: 'bmi', labelKey: 'STATISTICS.BODY_TABS.BMI' },
    { value: 'waist', labelKey: 'STATISTICS.BODY_TABS.WAIST' },
    { value: 'whtr', labelKey: 'STATISTICS.BODY_TABS.WHTR' },
];

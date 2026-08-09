import type { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs';

export const STATISTICS_RANGE_TABS: FdUiTab[] = [
    { value: 'week', labelKey: 'STATISTICS.RANGES.WEEK' },
    { value: 'month', labelKey: 'STATISTICS.RANGES.MONTH' },
    { value: 'quarter', labelKey: 'STATISTICS.RANGES.QUARTER' },
    { value: 'halfYear', labelKey: 'STATISTICS.RANGES.HALF_YEAR' },
    { value: 'year', labelKey: 'STATISTICS.RANGES.YEAR' },
    { value: 'custom', labelKey: 'STATISTICS.RANGES.CUSTOM' },
];

export const STATISTICS_NUTRITION_TABS: FdUiTab[] = [
    { value: 'calories', labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES' },
    { value: 'macros', labelKey: 'STATISTICS.NUTRITION_TABS.MACROS' },
    { value: 'distribution', labelKey: 'STATISTICS.NUTRITION_TABS.DISTRIBUTION' },
];

import { devDay, devRecord } from '../../../shared/development/development-input';
import { connectDevelopmentScreen, DevelopmentToolError } from '../../../shared/development/development-tools';
import { formatDateInputValue, parseLocalDateInputValue } from '../../../shared/lib/local-date.utils';
import type { StatisticsFacade } from './statistics.facade';
import { getQuantizationDays, isNutritionTab, isStatisticsRange, normalizeEndOfDay, normalizeStartOfDay } from './statistics-data-mapper';

function readCustomPeriod(value: Record<string, unknown>): { start: Date; end: Date } | null {
    if (value['dateFrom'] === undefined && value['dateTo'] === undefined) {
        return null;
    }
    const start = parseLocalDateInputValue(devDay(value['dateFrom']));
    const end = parseLocalDateInputValue(devDay(value['dateTo']));
    if (start === null || end === null || start > end) {
        throw new DevelopmentToolError('Use an ordered custom calendar range.');
    }
    return { start, end };
}

export function connectStatisticsDevelopmentScreen(facade: StatisticsFacade): void {
    const period = (range: { start: Date; end: Date }): { dateFrom: string; dateTo: string } => ({
        dateFrom: formatDateInputValue(range.start),
        dateTo: formatDateInputValue(range.end),
    });
    connectDevelopmentScreen({
        kind: 'statistics',
        read: () => {
            const selected = period(facade.currentRange());
            const applied = facade.appliedRange();
            const current = applied !== null && JSON.stringify(period(applied)) === JSON.stringify(selected);
            const available = current && !facade.isLoading() && !facade.hasLoadError();
            return {
                filterFields: ['range', 'dateFrom', 'dateTo', 'nutritionTab'],
                range: facade.selectedRange(),
                period: selected,
                appliedPeriod: applied === null ? null : period(applied),
                timeZone: new Intl.DateTimeFormat().resolvedOptions().timeZone,
                quantizationDays: getQuantizationDays(
                    normalizeStartOfDay(facade.currentRange().start),
                    normalizeEndOfDay(facade.currentRange().end),
                ),
                nutritionTab: facade.selectedNutritionTab(),
                units: { calories: 'kcal', nutrients: 'g', weight: 'kg', waist: 'cm' },
                loading: facade.isLoading(),
                error: facade.hasLoadError(),
                available,
                provenance: facade.requestProvenance(),
                aggregates: available
                    ? {
                          ...facade.dashboardCardsView().overview,
                          totalCalories: facade.chartStatisticsData()?.calories.reduce((sum, value) => sum + value, 0) ?? 0,
                          nutrients: facade.chartStatisticsData()?.aggregatedNutrients ?? null,
                          mealStructure: facade.dashboardCardsView().mealStructure,
                      }
                    : null,
            };
        },
        setFilters: input => {
            const value = devRecord(input, ['range', 'dateFrom', 'dateTo', 'nutritionTab']);
            const custom = readCustomPeriod(value);
            const range = value['range'] ?? (custom === null ? facade.selectedRange() : 'custom');
            const tab = value['nutritionTab'] ?? facade.selectedNutritionTab();
            if (!isStatisticsRange(range) || !isNutritionTab(tab)) {
                throw new DevelopmentToolError('Unsupported statistics range or nutrition tab.');
            }
            if (custom !== null) {
                if (range !== 'custom') {
                    throw new DevelopmentToolError('Use an ordered custom calendar range.');
                }
                facade.customRangeModel.set({ range: custom });
            }
            facade.changeRange(range);
            facade.changeNutritionTab(tab);
        },
    });
}

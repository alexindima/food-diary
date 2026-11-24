import { effect, inject, untracked } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import type { StatisticsFacade } from './statistics.facade';
import {
    readStatisticsRouteQuery,
    type StatisticsRouteParams,
    type StatisticsRouteQuery,
    writeStatisticsRouteQuery,
} from './statistics-route-query';

type StatisticsRouteOwner = Pick<StatisticsFacade, 'selectedRange' | 'selectedNutritionTab' | 'customRangeModel' | 'initialize'>;

function readSelection(facade: StatisticsRouteOwner): StatisticsRouteQuery {
    const custom = facade.customRangeModel().range;
    return {
        range: facade.selectedRange(),
        nutritionTab: facade.selectedNutritionTab(),
        customRange:
            custom?.start !== null && custom?.start !== undefined && custom.end !== null ? { start: custom.start, end: custom.end } : null,
    };
}

export function connectStatisticsRouteState(facade: StatisticsRouteOwner, readParams: () => StatisticsRouteParams): void {
    const route = inject(ActivatedRoute);
    const router = inject(Router);
    let pendingKey: string | null = null;
    let initialized = false;
    const write = (query: StatisticsRouteQuery, replaceUrl: boolean): void => {
        const serialized = writeStatisticsRouteQuery(query);
        const key = JSON.stringify(serialized);
        const params = untracked(readParams);
        // A selection matching the current URL can still supersede a different pending destination.
        if (
            pendingKey === key ||
            (pendingKey === null && Object.entries(serialized).every(([name, value]) => (params[name] ?? null) === value))
        ) {
            return;
        }
        pendingKey = key;
        void router
            .navigate([], { relativeTo: route, queryParams: serialized, queryParamsHandling: 'merge', replaceUrl, scroll: 'manual' })
            .finally(() => {
                if (pendingKey === key) {
                    pendingKey = null;
                }
            });
    };
    effect(() => {
        const query = readStatisticsRouteQuery(readParams());
        untracked(() => {
            facade.selectedRange.set(query.range);
            facade.selectedNutritionTab.set(query.nutritionTab);
            if (query.customRange !== null) {
                facade.customRangeModel.set({ range: query.customRange });
            }
            write(query, true);
            if (!initialized) {
                initialized = true;
                facade.initialize();
            }
        });
    });
    effect(() => {
        write(readSelection(facade), false);
    });
}

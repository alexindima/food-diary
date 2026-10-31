import { DestroyRef, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, type ParamMap, Router } from '@angular/router';

import type { StatisticsFacade } from './statistics.facade';
import { readStatisticsRouteQuery, type StatisticsRouteQuery, writeStatisticsRouteQuery } from './statistics-route-query';

type StatisticsRouteOwner = Pick<StatisticsFacade, 'selectedRange' | 'selectedNutritionTab' | 'customRangeModel'>;

function readSelection(facade: StatisticsRouteOwner): StatisticsRouteQuery {
    const custom = facade.customRangeModel().range;
    return {
        range: facade.selectedRange(),
        nutritionTab: facade.selectedNutritionTab(),
        customRange:
            custom?.start !== null && custom?.start !== undefined && custom.end !== null ? { start: custom.start, end: custom.end } : null,
    };
}

function matchesParams(params: ParamMap, query: StatisticsRouteQuery): boolean {
    return Object.entries(writeStatisticsRouteQuery(query)).every(([key, value]) => params.get(key) === value);
}

export function connectStatisticsRouteState(facade: StatisticsRouteOwner): void {
    const route = inject(ActivatedRoute);
    const router = inject(Router);
    const destroyRef = inject(DestroyRef);
    let params = route.snapshot.queryParamMap;
    let pendingKey: string | null = null;
    const write = (query: StatisticsRouteQuery, replaceUrl: boolean): void => {
        const serialized = writeStatisticsRouteQuery(query);
        const key = JSON.stringify(serialized);
        if (pendingKey === key || matchesParams(params, query)) {
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
    const apply = (value: ParamMap): void => {
        params = value;
        const query = readStatisticsRouteQuery(value);
        facade.selectedRange.set(query.range);
        facade.selectedNutritionTab.set(query.nutritionTab);
        if (query.customRange !== null) {
            facade.customRangeModel.set({ range: query.customRange });
        }
        write(query, true);
    };
    apply(params);
    route.queryParamMap.pipe(takeUntilDestroyed(destroyRef)).subscribe(apply);
    effect(() => {
        write(readSelection(facade), false);
    });
}

import { computed, type Signal } from '@angular/core';

import type { WaistEntrySummaryPoint } from '../../waist-history/models/waist-entry.data';
import type { WeightEntrySummaryPoint } from '../../weight-history/models/weight-entry.data';
import type { WeightTrendPoint } from '../components/weight-trend-card/weight-trend-card.component';
import { getWeightTrendRange } from './dashboard-date.utils';

type WeightTrendValuePoint = WeightTrendPoint & { value: number };

const TREND_ROUNDING_FACTOR = 10;

function hasTrendValue(point: WeightTrendPoint): point is WeightTrendValuePoint {
    return point.value !== null;
}

function buildFallbackTrend(latestValue: number | null, selectedDate: Date, trendDays: number): WeightTrendPoint[] {
    if (latestValue === null || latestValue <= 0) {
        return [];
    }

    const { start } = getWeightTrendRange(selectedDate, trendDays);
    const points: WeightTrendPoint[] = [];

    for (let i = 0; i < trendDays; i++) {
        const date = new Date(start);
        date.setDate(start.getDate() + i);
        points.push({ date: date.toISOString(), value: latestValue });
    }

    return points;
}

function computeTrendChange(series: WeightTrendPoint[]): number | null {
    const ordered = [...series].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
    const first = ordered.find(hasTrendValue);
    const last = [...ordered].reverse().find(hasTrendValue);

    if (first === undefined || last === undefined) {
        return null;
    }

    const diff = last.value - first.value;
    return Math.round(diff * TREND_ROUNDING_FACTOR) / TREND_ROUNDING_FACTOR;
}

function computeTrendCurrent(series: WeightTrendPoint[], fallbackValue: number | null): number | null {
    const ordered = [...series].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
    const last = [...ordered].reverse().find(hasTrendValue);
    return last !== undefined ? last.value : fallbackValue;
}

export function createWeightTrendSignals(
    weightTrendPoints: Signal<WeightEntrySummaryPoint[]>,
    latestWeight: Signal<number | null>,
    selectedDate: Signal<Date>,
    trendDays: number,
): {
    weightTrendSeries: Signal<WeightTrendPoint[]>;
    weightTrendChange: Signal<number | null>;
    weightTrendCurrent: Signal<number | null>;
} {
    const weightTrendSeries = computed<WeightTrendPoint[]>(() => {
        const points = weightTrendPoints().map(point => ({
            date: point.startDate,
            value: point.averageWeight > 0 ? point.averageWeight : null,
        }));
        return points.length > 0 ? points : buildFallbackTrend(latestWeight(), selectedDate(), trendDays);
    });

    const weightTrendChange = computed(() => computeTrendChange(weightTrendSeries()));
    const weightTrendCurrent = computed(() => computeTrendCurrent(weightTrendSeries(), latestWeight()));

    return { weightTrendSeries, weightTrendChange, weightTrendCurrent };
}

export function createWaistTrendSignals(
    waistTrendPoints: Signal<WaistEntrySummaryPoint[]>,
    latestWaist: Signal<number | null>,
    selectedDate: Signal<Date>,
    trendDays: number,
): {
    waistTrendSeries: Signal<WeightTrendPoint[]>;
    waistTrendChange: Signal<number | null>;
    waistTrendCurrent: Signal<number | null>;
} {
    const waistTrendSeries = computed<WeightTrendPoint[]>(() => {
        const points = waistTrendPoints().map(point => ({
            date: point.startDate,
            value: point.averageCircumference > 0 ? point.averageCircumference : null,
        }));
        return points.length > 0 ? points : buildFallbackTrend(latestWaist(), selectedDate(), trendDays);
    });

    const waistTrendChange = computed(() => computeTrendChange(waistTrendSeries()));
    const waistTrendCurrent = computed(() => computeTrendCurrent(waistTrendSeries(), latestWaist()));

    return { waistTrendSeries, waistTrendChange, waistTrendCurrent };
}

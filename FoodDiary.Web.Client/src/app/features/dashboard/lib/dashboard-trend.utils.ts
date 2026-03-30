import { computed, Signal } from '@angular/core';
import { WeightTrendPoint } from '../components/weight-trend-card/weight-trend-card.component';
import { WeightEntrySummaryPoint } from '../../weight-history/models/weight-entry.data';
import { WaistEntrySummaryPoint } from '../../waist-history/models/waist-entry.data';
import { getWeightTrendRange } from './dashboard-date.utils';

function buildFallbackTrend(latestValue: number | null, selectedDate: Date, trendDays: number): WeightTrendPoint[] {
    if (!latestValue) {
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
    const ordered = [...series].sort((a, b) => new Date(a.date as string).getTime() - new Date(b.date as string).getTime());
    const first = ordered.find(point => point.value !== null && point.value !== undefined);
    const last = [...ordered].reverse().find(point => point.value !== null && point.value !== undefined);

    if (!first || !last) {
        return null;
    }

    const diff = (last.value ?? 0) - (first.value ?? 0);
    return Math.round(diff * 10) / 10;
}

function computeTrendCurrent(series: WeightTrendPoint[], fallbackValue: number | null): number | null {
    const ordered = [...series].sort((a, b) => new Date(a.date as string).getTime() - new Date(b.date as string).getTime());
    const last = [...ordered].reverse().find(point => point.value !== null && point.value !== undefined);
    return last?.value ?? fallbackValue ?? null;
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
            date: point.dateFrom,
            value: point.averageWeight > 0 ? point.averageWeight : null,
        }));
        return points.length ? points : buildFallbackTrend(latestWeight(), selectedDate(), trendDays);
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
            date: point.dateFrom,
            value: point.averageCircumference > 0 ? point.averageCircumference : null,
        }));
        return points.length ? points : buildFallbackTrend(latestWaist(), selectedDate(), trendDays);
    });

    const waistTrendChange = computed(() => computeTrendChange(waistTrendSeries()));
    const waistTrendCurrent = computed(() => computeTrendCurrent(waistTrendSeries(), latestWaist()));

    return { waistTrendSeries, waistTrendChange, waistTrendCurrent };
}

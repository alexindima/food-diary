import { computed, type Signal } from '@angular/core';

import type { WaistEntrySummaryPoint } from '../../waist-history/models/waist-entry.data';
import type { WeightEntrySummaryPoint } from '../../weight-history/models/weight-entry.data';
import type { WeightTrendPoint } from '../components/weight-trend-card/weight-trend-card';

type WeightTrendValuePoint = WeightTrendPoint & { value: number };

const TREND_ROUNDING_FACTOR = 10;

function hasTrendValue(point: WeightTrendPoint): point is WeightTrendValuePoint {
    return point.value !== null;
}

function computeTrendChange(series: WeightTrendPoint[]): number | null {
    const validPoints = series.filter(hasTrendValue);
    if (validPoints.length === 0) {
        return null;
    }

    const first = validPoints[0];
    const last = validPoints.at(-1);
    if (last === undefined) {
        return null;
    }

    const diff = last.value - first.value;
    return Math.round(diff * TREND_ROUNDING_FACTOR) / TREND_ROUNDING_FACTOR;
}

function computeTrendCurrent(series: WeightTrendPoint[], fallbackValue: number | null): number | null {
    const validPoints = series.filter(hasTrendValue);
    return validPoints.at(-1)?.value ?? fallbackValue;
}

export function createWeightTrendSignals(
    weightTrendPoints: Signal<WeightEntrySummaryPoint[]>,
    latestWeight: Signal<number | null>,
): {
    weightTrendSeries: Signal<WeightTrendPoint[]>;
    weightTrendChange: Signal<number | null>;
    weightTrendCurrent: Signal<number | null>;
} {
    const weightTrendSeries = computed<WeightTrendPoint[]>(() => {
        const points = weightTrendPoints().map(point => ({
            date: point.startDate,
            value: point.averageWeightKg > 0 ? point.averageWeightKg : null,
        }));
        return points;
    });

    const weightTrendChange = computed(() => computeTrendChange(weightTrendSeries()));
    const weightTrendCurrent = computed(() => computeTrendCurrent(weightTrendSeries(), latestWeight()));

    return { weightTrendSeries, weightTrendChange, weightTrendCurrent };
}

export function createWaistTrendSignals(
    waistTrendPoints: Signal<WaistEntrySummaryPoint[]>,
    latestWaist: Signal<number | null>,
): {
    waistTrendSeries: Signal<WeightTrendPoint[]>;
    waistTrendChange: Signal<number | null>;
    waistTrendCurrent: Signal<number | null>;
} {
    const waistTrendSeries = computed<WeightTrendPoint[]>(() => {
        const points = waistTrendPoints().map(point => ({
            date: point.startDate,
            value: point.averageCircumferenceCm > 0 ? point.averageCircumferenceCm : null,
        }));
        return points;
    });

    const waistTrendChange = computed(() => computeTrendChange(waistTrendSeries()));
    const waistTrendCurrent = computed(() => computeTrendCurrent(waistTrendSeries(), latestWaist()));

    return { waistTrendSeries, waistTrendChange, waistTrendCurrent };
}

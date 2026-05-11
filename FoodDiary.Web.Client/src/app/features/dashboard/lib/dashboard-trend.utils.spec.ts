import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { createWaistTrendSignals, createWeightTrendSignals } from './dashboard-trend.utils';

const SELECTED_YEAR = 2026;
const MARCH = 2;
const SELECTED_DAY = 15;
const TREND_DAYS = 7;
const WEIGHT_75 = 75;
const WEIGHT_74_5 = 74.5;
const WEIGHT_80 = 80;
const WEIGHT_79 = 79;
const WEIGHT_78 = 78;
const WEIGHT_72 = 72;
const WAIST_85 = 85;
const WAIST_90 = 90;

describe('dashboard-trend.utils', () => {
    registerWeightTrendTests();
    registerWaistTrendTests();
});

function registerWeightTrendTests(): void {
    describe('createWeightTrendSignals', () => {
        it('should map weight trend points to WeightTrendPoint[]', () => {
            const points = signal([
                { startDate: '2026-03-10', endDate: '2026-03-10', averageWeight: WEIGHT_75 },
                { startDate: '2026-03-11', endDate: '2026-03-11', averageWeight: WEIGHT_74_5 },
            ]);
            const latestWeight = signal<number | null>(WEIGHT_75);
            const selectedDate = signal(new Date(SELECTED_YEAR, MARCH, SELECTED_DAY));

            const { weightTrendSeries } = createWeightTrendSignals(points, latestWeight, selectedDate, TREND_DAYS);
            const series = weightTrendSeries();

            expect(series).toHaveLength(2);
            expect(series[0].date).toBe('2026-03-10');
            expect(series[0].value).toBe(WEIGHT_75);
            expect(series[1].value).toBe(WEIGHT_74_5);
        });

        it('should set value to null for zero averageWeight', () => {
            const points = signal([{ startDate: '2026-03-10', endDate: '2026-03-10', averageWeight: 0 }]);
            const { weightTrendSeries } = createWeightTrendSignals(points, signal(null), signal(new Date()), TREND_DAYS);
            expect(weightTrendSeries()[0].value).toBeNull();
        });

        it('should build fallback trend when points array is empty', () => {
            const points = signal<{ startDate: string; endDate: string; averageWeight: number }[]>([]);
            const latestWeight = signal<number | null>(WEIGHT_80);
            const selectedDate = signal(new Date(SELECTED_YEAR, MARCH, SELECTED_DAY));

            const { weightTrendSeries } = createWeightTrendSignals(points, latestWeight, selectedDate, TREND_DAYS);
            const series = weightTrendSeries();

            expect(series).toHaveLength(TREND_DAYS);
            expect(series.every(p => p.value === WEIGHT_80)).toBe(true);
        });

        it('should return empty array when no points and no latest weight', () => {
            const points = signal<{ startDate: string; endDate: string; averageWeight: number }[]>([]);
            const { weightTrendSeries } = createWeightTrendSignals(points, signal(null), signal(new Date()), TREND_DAYS);
            expect(weightTrendSeries()).toHaveLength(0);
        });

        it('should compute trend change correctly', () => {
            const points = signal([
                { startDate: '2026-03-10', endDate: '2026-03-10', averageWeight: WEIGHT_80 },
                { startDate: '2026-03-11', endDate: '2026-03-11', averageWeight: WEIGHT_79 },
            ]);
            const { weightTrendChange } = createWeightTrendSignals(points, signal(null), signal(new Date()), TREND_DAYS);
            expect(weightTrendChange()).toBe(-1);
        });

        it('should return null for trend change when no valid points', () => {
            const points = signal([{ startDate: '2026-03-10', endDate: '2026-03-10', averageWeight: 0 }]);
            const { weightTrendChange } = createWeightTrendSignals(points, signal(null), signal(new Date()), TREND_DAYS);
            expect(weightTrendChange()).toBeNull();
        });

        it('should compute current weight from latest series point', () => {
            const points = signal([
                { startDate: '2026-03-10', endDate: '2026-03-10', averageWeight: WEIGHT_80 },
                { startDate: '2026-03-11', endDate: '2026-03-11', averageWeight: WEIGHT_78 },
            ]);
            const { weightTrendCurrent } = createWeightTrendSignals(points, signal(null), signal(new Date()), TREND_DAYS);
            expect(weightTrendCurrent()).toBe(WEIGHT_78);
        });

        it('should fall back to latestWeight when series has no valid points', () => {
            const points = signal<{ startDate: string; endDate: string; averageWeight: number }[]>([]);
            const { weightTrendCurrent } = createWeightTrendSignals(points, signal(WEIGHT_72), signal(new Date()), TREND_DAYS);
            expect(weightTrendCurrent()).toBe(WEIGHT_72);
        });
    });
}

function registerWaistTrendTests(): void {
    describe('createWaistTrendSignals', () => {
        it('should map waist trend points', () => {
            const points = signal([{ startDate: '2026-03-10', endDate: '2026-03-10', averageCircumference: WAIST_85 }]);
            const { waistTrendSeries } = createWaistTrendSignals(points, signal(null), signal(new Date()), TREND_DAYS);
            expect(waistTrendSeries()[0].value).toBe(WAIST_85);
        });

        it('should build fallback for empty waist points', () => {
            const points = signal<{ startDate: string; endDate: string; averageCircumference: number }[]>([]);
            const { waistTrendSeries } = createWaistTrendSignals(
                points,
                signal(WAIST_90),
                signal(new Date(SELECTED_YEAR, MARCH, SELECTED_DAY)),
                TREND_DAYS,
            );
            expect(waistTrendSeries()).toHaveLength(TREND_DAYS);
            expect(waistTrendSeries().every(p => p.value === WAIST_90)).toBe(true);
        });
    });
}

import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { createWaistTrendSignals, createWeightTrendSignals } from './dashboard-trend.utils';

describe('dashboard-trend.utils', () => {
    describe('createWeightTrendSignals', () => {
        it('should map weight trend points to WeightTrendPoint[]', () => {
            const points = signal([
                { dateFrom: '2026-03-10', dateTo: '2026-03-10', averageWeight: 75 },
                { dateFrom: '2026-03-11', dateTo: '2026-03-11', averageWeight: 74.5 },
            ]);
            const latestWeight = signal<number | null>(75);
            const selectedDate = signal(new Date(2026, 2, 15));

            const { weightTrendSeries } = createWeightTrendSignals(points, latestWeight, selectedDate, 7);
            const series = weightTrendSeries();

            expect(series).toHaveLength(2);
            expect(series[0].date).toBe('2026-03-10');
            expect(series[0].value).toBe(75);
            expect(series[1].value).toBe(74.5);
        });

        it('should set value to null for zero averageWeight', () => {
            const points = signal([{ dateFrom: '2026-03-10', dateTo: '2026-03-10', averageWeight: 0 }]);
            const { weightTrendSeries } = createWeightTrendSignals(points, signal(null), signal(new Date()), 7);
            expect(weightTrendSeries()[0].value).toBeNull();
        });

        it('should build fallback trend when points array is empty', () => {
            const points = signal<{ dateFrom: string; dateTo: string; averageWeight: number }[]>([]);
            const latestWeight = signal<number | null>(80);
            const selectedDate = signal(new Date(2026, 2, 15));

            const { weightTrendSeries } = createWeightTrendSignals(points, latestWeight, selectedDate, 7);
            const series = weightTrendSeries();

            expect(series).toHaveLength(7);
            expect(series.every(p => p.value === 80)).toBe(true);
        });

        it('should return empty array when no points and no latest weight', () => {
            const points = signal<{ dateFrom: string; dateTo: string; averageWeight: number }[]>([]);
            const { weightTrendSeries } = createWeightTrendSignals(points, signal(null), signal(new Date()), 7);
            expect(weightTrendSeries()).toHaveLength(0);
        });

        it('should compute trend change correctly', () => {
            const points = signal([
                { dateFrom: '2026-03-10', dateTo: '2026-03-10', averageWeight: 80 },
                { dateFrom: '2026-03-11', dateTo: '2026-03-11', averageWeight: 79 },
            ]);
            const { weightTrendChange } = createWeightTrendSignals(points, signal(null), signal(new Date()), 7);
            expect(weightTrendChange()).toBe(-1);
        });

        it('should return null for trend change when no valid points', () => {
            const points = signal([{ dateFrom: '2026-03-10', dateTo: '2026-03-10', averageWeight: 0 }]);
            const { weightTrendChange } = createWeightTrendSignals(points, signal(null), signal(new Date()), 7);
            expect(weightTrendChange()).toBeNull();
        });

        it('should compute current weight from latest series point', () => {
            const points = signal([
                { dateFrom: '2026-03-10', dateTo: '2026-03-10', averageWeight: 80 },
                { dateFrom: '2026-03-11', dateTo: '2026-03-11', averageWeight: 78 },
            ]);
            const { weightTrendCurrent } = createWeightTrendSignals(points, signal(null), signal(new Date()), 7);
            expect(weightTrendCurrent()).toBe(78);
        });

        it('should fall back to latestWeight when series has no valid points', () => {
            const points = signal<{ dateFrom: string; dateTo: string; averageWeight: number }[]>([]);
            const { weightTrendCurrent } = createWeightTrendSignals(points, signal(72), signal(new Date()), 7);
            expect(weightTrendCurrent()).toBe(72);
        });
    });

    describe('createWaistTrendSignals', () => {
        it('should map waist trend points', () => {
            const points = signal([{ dateFrom: '2026-03-10', dateTo: '2026-03-10', averageCircumference: 85 }]);
            const { waistTrendSeries } = createWaistTrendSignals(points, signal(null), signal(new Date()), 7);
            expect(waistTrendSeries()[0].value).toBe(85);
        });

        it('should build fallback for empty waist points', () => {
            const points = signal<{ dateFrom: string; dateTo: string; averageCircumference: number }[]>([]);
            const { waistTrendSeries } = createWaistTrendSignals(points, signal(90), signal(new Date(2026, 2, 15)), 7);
            expect(waistTrendSeries()).toHaveLength(7);
            expect(waistTrendSeries().every(p => p.value === 90)).toBe(true);
        });
    });
});

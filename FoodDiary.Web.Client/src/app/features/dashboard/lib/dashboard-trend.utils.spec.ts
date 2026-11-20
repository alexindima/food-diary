import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { type CalendarDate, calendarDate } from '../../../shared/models/semantics/date-value';
import { createWaistTrendSignals, createWeightTrendSignals } from './dashboard-trend.utils';

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
                { startDate: calendarDate('2026-03-10'), endDate: calendarDate('2026-03-10'), averageWeightKg: WEIGHT_75 },
                { startDate: calendarDate('2026-03-11'), endDate: calendarDate('2026-03-11'), averageWeightKg: WEIGHT_74_5 },
            ]);
            const latestWeight = signal<number | null>(WEIGHT_75);
            const { weightTrendSeries } = createWeightTrendSignals(points, latestWeight);
            const series = weightTrendSeries();

            expect(series).toHaveLength(2);
            expect(series[0].date).toBe('2026-03-10');
            expect(series[0].value).toBe(WEIGHT_75);
            expect(series[1].value).toBe(WEIGHT_74_5);
        });

        it('should set value to null for zero averageWeightKg', () => {
            const points = signal([{ startDate: calendarDate('2026-03-10'), endDate: calendarDate('2026-03-10'), averageWeightKg: 0 }]);
            const { weightTrendSeries } = createWeightTrendSignals(points, signal(null));
            expect(weightTrendSeries()[0].value).toBeNull();
        });

        it('should not invent trend points from the latest measurement', () => {
            const points = signal<Array<{ startDate: CalendarDate; endDate: CalendarDate; averageWeightKg: number }>>([]);
            const latestWeight = signal<number | null>(WEIGHT_80);
            const { weightTrendSeries } = createWeightTrendSignals(points, latestWeight);

            expect(weightTrendSeries()).toHaveLength(0);
        });

        it('should return empty array when no points and no latest weight', () => {
            const points = signal<Array<{ startDate: CalendarDate; endDate: CalendarDate; averageWeightKg: number }>>([]);
            const { weightTrendSeries } = createWeightTrendSignals(points, signal(null));
            expect(weightTrendSeries()).toHaveLength(0);
        });

        it('should compute trend change correctly', () => {
            const points = signal([
                { startDate: calendarDate('2026-03-10'), endDate: calendarDate('2026-03-10'), averageWeightKg: WEIGHT_80 },
                { startDate: calendarDate('2026-03-11'), endDate: calendarDate('2026-03-11'), averageWeightKg: WEIGHT_79 },
            ]);
            const { weightTrendChange } = createWeightTrendSignals(points, signal(null));
            expect(weightTrendChange()).toBe(-1);
        });

        it('should return null for trend change when no valid points', () => {
            const points = signal([{ startDate: calendarDate('2026-03-10'), endDate: calendarDate('2026-03-10'), averageWeightKg: 0 }]);
            const { weightTrendChange } = createWeightTrendSignals(points, signal(null));
            expect(weightTrendChange()).toBeNull();
        });

        it('should compute current weight from latest series point', () => {
            const points = signal([
                { startDate: calendarDate('2026-03-10'), endDate: calendarDate('2026-03-10'), averageWeightKg: WEIGHT_80 },
                { startDate: calendarDate('2026-03-11'), endDate: calendarDate('2026-03-11'), averageWeightKg: WEIGHT_78 },
            ]);
            const { weightTrendCurrent } = createWeightTrendSignals(points, signal(null));
            expect(weightTrendCurrent()).toBe(WEIGHT_78);
        });

        it('should fall back to latestWeight when series has no valid points', () => {
            const points = signal<Array<{ startDate: CalendarDate; endDate: CalendarDate; averageWeightKg: number }>>([]);
            const { weightTrendCurrent } = createWeightTrendSignals(points, signal(WEIGHT_72));
            expect(weightTrendCurrent()).toBe(WEIGHT_72);
        });
    });
}

function registerWaistTrendTests(): void {
    describe('createWaistTrendSignals', () => {
        it('should map waist trend points', () => {
            const points = signal([
                { startDate: calendarDate('2026-03-10'), endDate: calendarDate('2026-03-10'), averageCircumferenceCm: WAIST_85 },
            ]);
            const { waistTrendSeries } = createWaistTrendSignals(points, signal(null));
            expect(waistTrendSeries()[0].value).toBe(WAIST_85);
        });

        it('keeps empty waist buckets as gaps and still reports the latest known measurement', () => {
            const first = calendarDate('2026-03-10');
            const second = calendarDate('2026-03-11');
            const third = calendarDate('2026-03-12');
            const emptyBucket = { startDate: third, endDate: third, averageCircumferenceCm: 0 };
            const points = signal([
                { startDate: first, endDate: first, averageCircumferenceCm: WAIST_90 },
                { startDate: second, endDate: second, averageCircumferenceCm: WAIST_85 },
                emptyBucket,
            ]);
            const latestMeasurement = signal<number | null>(WAIST_90);
            const { waistTrendSeries, waistTrendChange, waistTrendCurrent } = createWaistTrendSignals(points, latestMeasurement);

            expect(waistTrendSeries()).toEqual([
                { date: first, value: WAIST_90 },
                { date: second, value: WAIST_85 },
                { date: third, value: null },
            ]);
            expect(waistTrendChange()).toBe(WAIST_85 - WAIST_90);
            expect(waistTrendCurrent()).toBe(WAIST_85);

            points.set([emptyBucket]);
            expect(waistTrendSeries()).toEqual([{ date: third, value: null }]);
            expect(waistTrendChange()).toBeNull();
            expect(waistTrendCurrent()).toBe(WAIST_90);
        });
        it('should not invent waist trend points from the latest measurement', () => {
            const points = signal<Array<{ startDate: CalendarDate; endDate: CalendarDate; averageCircumferenceCm: number }>>([]);
            const { waistTrendSeries } = createWaistTrendSignals(points, signal(WAIST_90));
            expect(waistTrendSeries()).toHaveLength(0);
        });
    });
}

import { afterEach, describe, expect, it, vi } from 'vitest';

import {
    buildBodyChartPoints,
    buildCaloriesTrendPoints,
    buildMacroSparklinePoints,
    buildNutrientBarItems,
    buildNutrientPieSegments,
    buildNutrientTrendGroups,
    buildSummaryMetrics,
    buildSummarySparklinePoints,
    getCurrentDateRange,
    getDateRangeDayCount,
    normalizeEndOfDay,
    normalizeStartOfDay,
} from './statistics-data-mapper';

const TEST_YEAR = 2026;
const MAY_INDEX = 4;
const APRIL_INDEX = 3;
const CURRENT_DAY = 6;
const NOON_HOUR = 12;
const THIRD_DAY = 3;
const WEEK_START_DAY = 30;
const END_OF_DAY_HOUR = 23;
const END_OF_DAY_MINUTE = 59;
const END_OF_DAY_SECOND = 59;
const END_OF_DAY_MS = 999;
const HOURS_PER_DAY = 24;
const MINUTES_PER_HOUR = 60;
const SECONDS_PER_MINUTE = 60;
const MS_PER_SECOND = 1000;
const EXPECTED_WEEK_DAYS = 7;
const FIRST_CALORIES = 1800;
const SECOND_CALORIES = 2100;
const FIRST_PROTEINS = 120;
const SECOND_PROTEINS = 140;
const FIRST_FATS = 60;
const SECOND_FATS = 70;
const FIRST_CARBS = 220;
const SECOND_CARBS = 240;
const FIRST_FIBER = 18;
const SECOND_FIBER = 22;
const SUMMARY_DAY_COUNT = 4;
const EXPECTED_THREE_DAYS = 3;
const BODY_START_VALUE = 80;
const BODY_END_VALUE = 82;
const BODY_INTERPOLATED_VALUE = 81;
const NUTRIENT_GROUP_COUNT = 4;
const FIBER_GROUP_INDEX = 3;
const MS_PER_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR * SECONDS_PER_MINUTE * MS_PER_SECOND;

// eslint-disable-next-line max-lines-per-function -- Mapper coverage is intentionally grouped around one fixture shape.
describe('statistics-data-mapper', () => {
    afterEach(() => {
        vi.useRealTimers();
    });

    describe('getCurrentDateRange', () => {
        it('should return seven inclusive calendar days for week range', () => {
            vi.useFakeTimers();
            vi.setSystemTime(new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY, NOON_HOUR, 0, 0, 0));

            const range = getCurrentDateRange('week', null);
            const start = normalizeStartOfDay(range.start);
            const end = normalizeEndOfDay(range.end);
            const days = Math.round((end.getTime() - start.getTime()) / MS_PER_DAY);

            expect(start).toEqual(new Date(TEST_YEAR, APRIL_INDEX, WEEK_START_DAY, 0, 0, 0, 0));
            expect(end).toEqual(
                new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY, END_OF_DAY_HOUR, END_OF_DAY_MINUTE, END_OF_DAY_SECOND, END_OF_DAY_MS),
            );
            expect(days).toBe(EXPECTED_WEEK_DAYS);
        });

        it('should normalize reversed custom range boundaries', () => {
            const start = new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY);
            const end = new Date(TEST_YEAR, MAY_INDEX, THIRD_DAY);

            const range = getCurrentDateRange('custom', { start, end });

            expect(range.start).toBe(end);
            expect(range.end).toBe(start);
        });
    });

    it('builds summary sparkline points', () => {
        const points = buildSummarySparklinePoints(
            {
                date: [new Date(TEST_YEAR, MAY_INDEX, 1), new Date(TEST_YEAR, MAY_INDEX, 2)],
                calories: [FIRST_CALORIES, SECOND_CALORIES],
                nutrientsStatistic: { proteins: [], fats: [], carbs: [], fiber: [] },
                aggregatedNutrients: { proteins: 0, fats: 0, carbs: 0, fiber: 0 },
            },
            date => date.getDate().toString(),
        );

        expect(points).toEqual([
            { label: '1', value: FIRST_CALORIES },
            { label: '2', value: SECOND_CALORIES },
        ]);
    });

    it('fills missing summary sparkline points with zeroes', () => {
        const points = buildSummarySparklinePoints(
            {
                date: [new Date(TEST_YEAR, MAY_INDEX, 1), new Date(TEST_YEAR, MAY_INDEX, 2)],
                calories: [null as unknown as number, SECOND_CALORIES],
                nutrientsStatistic: { proteins: [], fats: [], carbs: [], fiber: [] },
                aggregatedNutrients: { proteins: 0, fats: 0, carbs: 0, fiber: 0 },
            },
            date => date.getDate().toString(),
        );

        expect(points).toEqual([
            { label: '1', value: 0 },
            { label: '2', value: SECOND_CALORIES },
        ]);
    });

    it('builds calories trend points', () => {
        const points = buildCaloriesTrendPoints(
            {
                date: [new Date(TEST_YEAR, MAY_INDEX, 1), new Date(TEST_YEAR, MAY_INDEX, 2)],
                calories: [FIRST_CALORIES, SECOND_CALORIES],
                nutrientsStatistic: { proteins: [], fats: [], carbs: [], fiber: [] },
                aggregatedNutrients: { proteins: 0, fats: 0, carbs: 0, fiber: 0 },
            },
            date => date.getDate().toString(),
        );

        expect(points).toEqual([
            { label: '1', value: FIRST_CALORIES },
            { label: '2', value: SECOND_CALORIES },
        ]);
    });

    it('builds summary average calories from period day count', () => {
        const metrics = buildSummaryMetrics(
            {
                date: [new Date(TEST_YEAR, MAY_INDEX, 1), new Date(TEST_YEAR, MAY_INDEX, THIRD_DAY)],
                calories: [FIRST_CALORIES, SECOND_CALORIES],
                nutrientsStatistic: { proteins: [], fats: [], carbs: [], fiber: [] },
                aggregatedNutrients: { proteins: 0, fats: 0, carbs: 0, fiber: 0 },
            },
            SUMMARY_DAY_COUNT,
        );

        expect(metrics?.totalCalories).toBe(FIRST_CALORIES + SECOND_CALORIES);
        expect(metrics?.averageCard.consumption).toBe((FIRST_CALORIES + SECOND_CALORIES) / SUMMARY_DAY_COUNT);
    });

    it('counts inclusive calendar days in a date range', () => {
        const dayCount = getDateRangeDayCount({
            start: new Date(TEST_YEAR, MAY_INDEX, 1, NOON_HOUR),
            end: new Date(TEST_YEAR, MAY_INDEX, THIRD_DAY, NOON_HOUR),
        });

        expect(dayCount).toBe(EXPECTED_THREE_DAYS);
    });

    it('builds nutrient trend groups', () => {
        const groups = buildNutrientTrendGroups(
            {
                date: [new Date(TEST_YEAR, MAY_INDEX, 1), new Date(TEST_YEAR, MAY_INDEX, 2)],
                calories: [],
                nutrientsStatistic: {
                    proteins: [FIRST_PROTEINS, SECOND_PROTEINS],
                    fats: [FIRST_FATS, SECOND_FATS],
                    carbs: [FIRST_CARBS, SECOND_CARBS],
                    fiber: [FIRST_FIBER, SECOND_FIBER],
                },
                aggregatedNutrients: { proteins: 0, fats: 0, carbs: 0, fiber: 0 },
            },
            date => date.getDate().toString(),
            key => key,
        );

        expect(groups).toHaveLength(NUTRIENT_GROUP_COUNT);
        expect(groups[0]).toMatchObject({
            key: 'proteins',
            label: 'NUTRIENTS.PROTEINS',
            points: [
                { label: '1', value: FIRST_PROTEINS },
                { label: '2', value: SECOND_PROTEINS },
            ],
        });
        expect(groups[FIBER_GROUP_INDEX]).toMatchObject({
            key: 'fiber',
            label: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
            points: [
                { label: '1', value: FIRST_FIBER },
                { label: '2', value: SECOND_FIBER },
            ],
        });
    });

    it('builds nutrient pie segments and bar items', () => {
        const stats = {
            date: [],
            calories: [],
            nutrientsStatistic: { proteins: [], fats: [], carbs: [], fiber: [] },
            aggregatedNutrients: {
                proteins: FIRST_PROTEINS,
                fats: FIRST_FATS,
                carbs: FIRST_CARBS,
                fiber: FIRST_FIBER,
            },
        };

        expect(buildNutrientPieSegments(stats, key => key)).toMatchObject([
            { label: 'NUTRIENTS.PROTEINS', value: FIRST_PROTEINS },
            { label: 'NUTRIENTS.FATS', value: FIRST_FATS },
            { label: 'NUTRIENTS.CARBS', value: FIRST_CARBS },
        ]);
        expect(buildNutrientBarItems(stats, key => key)).toMatchObject([
            { label: 'NUTRIENTS.PROTEINS', value: FIRST_PROTEINS },
            { label: 'NUTRIENTS.FATS', value: FIRST_FATS },
            { label: 'NUTRIENTS.CARBS', value: FIRST_CARBS },
            { label: 'SHARED.NUTRIENTS_SUMMARY.FIBER', value: FIRST_FIBER },
        ]);
    });

    it('builds macro sparkline points', () => {
        const points = buildMacroSparklinePoints(
            {
                date: [new Date(TEST_YEAR, MAY_INDEX, 1), new Date(TEST_YEAR, MAY_INDEX, 2)],
                calories: [],
                nutrientsStatistic: { proteins: [FIRST_PROTEINS, SECOND_PROTEINS], fats: [], carbs: [], fiber: [] },
                aggregatedNutrients: { proteins: 0, fats: 0, carbs: 0, fiber: 0 },
            },
            date => date.getDate().toString(),
        );

        expect(points.proteins).toEqual([
            { label: '1', value: FIRST_PROTEINS },
            { label: '2', value: SECOND_PROTEINS },
        ]);
        expect(points.fats).toEqual([
            { label: '1', value: 0 },
            { label: '2', value: 0 },
        ]);
    });

    it('builds body chart points with interpolated missing values', () => {
        const points = buildBodyChartPoints(
            [
                { startDate: '2026-05-01', value: BODY_START_VALUE },
                { startDate: '2026-05-02', value: 0 },
                { startDate: '2026-05-03', value: BODY_END_VALUE },
            ],
            point => point.value,
            date => date,
        );

        expect(points).toEqual([
            { label: '2026-05-01', value: BODY_START_VALUE },
            { label: '2026-05-02', value: BODY_INTERPOLATED_VALUE },
            { label: '2026-05-03', value: BODY_END_VALUE },
        ]);
    });
});

import { describe, expect, it } from 'vitest';

import type { NutrientBar } from './dashboard-summary-card.types';
import {
    buildDefaultDashboardNutrientBars,
    buildRingDasharray,
    calculateDashboardPercent,
    clampDashboardPercent,
    getDashboardColorForPercent,
    getDashboardNutrientBarColor,
    mixDashboardColorWithWhite,
    normalizeDailyGoal,
    normalizeWeeklyGoal,
} from './dashboard-summary-card.utils';

const DAILY_GOAL = 2000;
const WEEKLY_GOAL = 10000;
const NEGATIVE_GOAL = -100;
const DAILY_CONSUMED = 1500;
const OVER_CONSUMED = 3000;
const HALF_VALUE = 50;
const RADIUS = 10;
const EXPECTED_CIRCUMFERENCE = 2 * Math.PI * RADIUS;
const DAILY_PERCENT = 75;
const OVER_PERCENT = 150;
const CLAMPED_PERCENT = 120;
const WEEK_DAYS = 7;
const DEFAULT_NUTRIENT_COUNT = 4;
const FULL_COLOR = '#139f5b';

describe('dashboard summary card utils', () => {
    it('normalizes daily and weekly goals', () => {
        expect(normalizeDailyGoal(NEGATIVE_GOAL)).toBe(0);
        expect(normalizeDailyGoal(DAILY_GOAL)).toBe(DAILY_GOAL);
        expect(normalizeWeeklyGoal(null, DAILY_GOAL)).toBe(DAILY_GOAL * WEEK_DAYS);
        expect(normalizeWeeklyGoal(WEEKLY_GOAL, DAILY_GOAL)).toBe(WEEKLY_GOAL);
    });

    it('calculates dashboard percent safely', () => {
        expect(calculateDashboardPercent(DAILY_CONSUMED, DAILY_GOAL)).toBe(DAILY_PERCENT);
        expect(calculateDashboardPercent(OVER_CONSUMED, DAILY_GOAL)).toBe(OVER_PERCENT);
        expect(calculateDashboardPercent(DAILY_CONSUMED, 0)).toBe(0);
        expect(calculateDashboardPercent(NEGATIVE_GOAL, DAILY_GOAL)).toBe(0);
    });

    it('builds ring dasharray from clamped percent', () => {
        const [halfFilled, halfCircumference] = buildRingDasharray(HALF_VALUE, RADIUS).split(' ').map(Number);
        const [overFilled, overCircumference] = buildRingDasharray(OVER_PERCENT, RADIUS).split(' ').map(Number);

        expect(halfFilled).toBeCloseTo(EXPECTED_CIRCUMFERENCE / 2);
        expect(halfCircumference).toBeCloseTo(EXPECTED_CIRCUMFERENCE);
        expect(overFilled).toBeCloseTo(EXPECTED_CIRCUMFERENCE);
        expect(overCircumference).toBeCloseTo(EXPECTED_CIRCUMFERENCE);
    });

    it('clamps nutrient fill percentages', () => {
        expect(clampDashboardPercent(OVER_PERCENT)).toBe(CLAMPED_PERCENT);
        expect(clampDashboardPercent(NEGATIVE_GOAL)).toBe(0);
        expect(clampDashboardPercent(Number.NaN)).toBe(0);
    });

    it('resolves color stops, css variables, and white mixes', () => {
        expect(getDashboardColorForPercent(0)).toBe('var(--fd-color-sky-500)');
        expect(getDashboardColorForPercent(OVER_PERCENT)).toBe('var(--fd-color-danger)');
        expect(mixDashboardColorWithWhite('var(--fd-color-sky-500)', 0)).toBe('#0ea5e9');
    });

    it('builds default nutrient bars and resolves nutrient colors', () => {
        const defaults = buildDefaultDashboardNutrientBars();
        expect(defaults).toHaveLength(DEFAULT_NUTRIENT_COUNT);
        expect(getDashboardNutrientBarColor(createBar({ current: 0, target: 0 }))).toBe('var(--fd-color-gray-500-static)');
        expect(getDashboardNutrientBarColor(createBar({ current: HALF_VALUE, target: HALF_VALUE }))).toBe(FULL_COLOR);
    });
});

function createBar(overrides: Partial<NutrientBar> = {}): NutrientBar {
    return {
        id: 'protein',
        label: 'Protein',
        current: HALF_VALUE,
        target: HALF_VALUE,
        unit: 'g',
        colorStart: '#4dabff',
        colorEnd: '#2563eb',
        ...overrides,
    };
}

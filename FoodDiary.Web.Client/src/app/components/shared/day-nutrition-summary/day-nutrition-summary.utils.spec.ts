/* eslint-disable @typescript-eslint/no-magic-numbers -- Numeric fixtures make scale boundaries explicit. */
import { describe, expect, it } from 'vitest';

import type { NutrientBar } from '../nutrition-summary/nutrition-summary.types';
import {
    buildDayNutrientBarViewModels,
    calculateDaySummaryGoalPosition,
    calculateDaySummaryPercent,
    resolveDaySummaryScaleMax,
} from './day-nutrition-summary.utils';

describe('day nutrition summary utils', () => {
    it('keeps the common scale at 100 when no nutrient exceeds its goal', () => {
        expect(resolveDaySummaryScaleMax([createBar('carbs', 84, 115), createBar('fats', 22, 100)])).toBe(100);
    });

    it('expands the common scale through stable overflow steps', () => {
        expect(resolveDaySummaryScaleMax([createBar('carbs', 126, 115)])).toBe(110);
        expect(resolveDaySummaryScaleMax([createBar('carbs', 140, 115)])).toBe(125);
        expect(resolveDaySummaryScaleMax([createBar('carbs', 172, 115)])).toBe(150);
        expect(resolveDaySummaryScaleMax([createBar('carbs', 201, 115)])).toBe(175);
    });

    it('uses the same goal position and scale for every nutrient', () => {
        const bars = buildDayNutrientBarViewModels([createBar('proteins', 110, 220), createBar('carbs', 224, 115)], 200);

        expect(calculateDaySummaryGoalPosition(200)).toBe(50);
        expect(bars[0]?.fillWidth).toBe(25);
        expect(bars[1]?.fillWidth).toBe(97.5);
    });

    it('only marks risk-oriented nutrients as excess', () => {
        const bars = buildDayNutrientBarViewModels(
            [createBar('proteins', 242, 220), createBar('carbs', 127, 115), createBar('fiber', 22, 11)],
            200,
        );

        expect(bars.map(bar => bar.isExcess)).toEqual([false, true, false]);
        expect(calculateDaySummaryPercent(0, 0)).toBe(0);
    });
});

function createBar(id: string, current: number, target: number): NutrientBar {
    return { id, label: id, current, target, unit: 'g', colorStart: '#00aaff', colorEnd: '#00aaff' };
}

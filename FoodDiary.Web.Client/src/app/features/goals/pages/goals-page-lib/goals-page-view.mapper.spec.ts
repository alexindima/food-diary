import { describe, expect, it } from 'vitest';

import type { MacroPreset } from '../../lib/goals.facade';
import {
    buildBodyTargets,
    buildCyclingDayControls,
    buildMacroPresetOptions,
    calculateCaloriesFromRingPointer,
    calculateGoalRingDistances,
    isPointInsideGoalRing,
    withMacroProgressStyles,
} from './goals-page-view.mapper';

const RING_RECT = {
    left: 10,
    top: 20,
    width: 200,
    height: 200,
};
const MIN_CALORIES = 0;
const MAX_CALORIES = 4000;
const QUARTER_CALORIES = 1000;
const HALF_CALORIES = 2000;
const HALF_PROGRESS_PERCENT = 50;
const HALF_PROGRESS_RATIO = 0.5;

describe('goals page view mapper', () => {
    it('builds body targets from current target values', () => {
        const targets = buildBodyTargets({ weight: 72, waist: 80 });

        expect(targets).toEqual([
            expect.objectContaining({
                key: 'weight',
                titleKey: 'GOALS_PAGE.BODY_TARGET_WEIGHT',
                value: 72,
                unit: 'kg',
            }),
            expect.objectContaining({
                key: 'waist',
                titleKey: 'GOALS_PAGE.BODY_TARGET_WAIST',
                value: 80,
                unit: 'cm',
            }),
        ]);
    });

    it('builds stable cycling day input ids', () => {
        const controls = buildCyclingDayControls([{ key: 'mondayCalories', labelKey: 'MONDAY' }]);

        expect(controls).toEqual([
            {
                key: 'mondayCalories',
                labelKey: 'MONDAY',
                inputId: 'cycling-day-mondayCalories',
            },
        ]);
    });

    it('translates macro preset options', () => {
        const presets: MacroPreset[] = [
            { key: 'custom', labelKey: 'CUSTOM' },
            { key: 'classic', labelKey: 'CLASSIC', percent: { protein: 0.3, fats: 0.3, carbs: 0.4 } },
        ];

        const options = buildMacroPresetOptions(presets, key => `translated:${key}`);

        expect(options).toEqual([
            { value: 'custom', label: 'translated:CUSTOM' },
            { value: 'classic', label: 'translated:CLASSIC' },
        ]);
    });

    it('adds CSS progress fields to macro states', () => {
        const result = withMacroProgressStyles({ percent: HALF_PROGRESS_PERCENT, value: 10 });

        expect(result).toEqual({
            percent: HALF_PROGRESS_PERCENT,
            value: 10,
            progressOffset: '50%',
            progressRatio: HALF_PROGRESS_RATIO,
        });
    });

    it('detects whether a pointer is inside the ring band', () => {
        const bandPoint = { clientX: 110, clientY: 25 };
        const centerPoint = { clientX: 110, clientY: 120 };

        expect(isPointInsideGoalRing(bandPoint, RING_RECT)).toBe(true);
        expect(isPointInsideGoalRing(centerPoint, RING_RECT)).toBe(false);
    });

    it('calculates ring distances from pointer and rect', () => {
        const distances = calculateGoalRingDistances({ clientX: 110, clientY: 20 }, RING_RECT);

        expect(distances).toEqual({
            centerX: 110,
            centerY: 120,
            distanceFromCenter: 100,
            innerRadius: 70,
            outerRadius: 100,
        });
    });

    it('maps ring pointer position to calories', () => {
        expect(calculateCaloriesFromRingPointer({ clientX: 110, clientY: 20 }, RING_RECT, MIN_CALORIES, MAX_CALORIES)).toBe(MIN_CALORIES);
        expect(calculateCaloriesFromRingPointer({ clientX: 210, clientY: 120 }, RING_RECT, MIN_CALORIES, MAX_CALORIES)).toBe(
            QUARTER_CALORIES,
        );
        expect(calculateCaloriesFromRingPointer({ clientX: 110, clientY: 220 }, RING_RECT, MIN_CALORIES, MAX_CALORIES)).toBe(HALF_CALORIES);
    });
});

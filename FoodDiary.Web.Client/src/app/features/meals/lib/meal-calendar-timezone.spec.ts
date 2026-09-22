import { describe, expect, it } from 'vitest';

import { toLocalDayEndIso, toLocalDayStartIso } from '../../../shared/lib/local-date.utils';
import {
    buildMealDateTime,
    buildMealManageDto,
    buildMealManageFormPatchValue,
    createMealManageFormValue,
} from '../components/manage/meal-manage-lib/meal-manage-form.mapper';
import type { Meal } from '../models/meal.data';

const NO_NUTRIENTS = { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 };
const DAYS = ['2026-01-01', '2026-03-08', '2026-03-29', '2026-04-05', '2026-10-04', '2026-10-25', '2026-11-01', '2026-12-31'];
const TIMES = ['00:01', '01:00', '12:30', '23:59'];

describe('Meal calendar round trip in the process timezone', () => {
    it.each(DAYS.flatMap(date => TIMES.map(time => ({ date, time }))))(
        'preserves local $date $time through create, API and edit',
        ({ date, time }) => {
            const instant = buildMealDateTime(date, time, new Date('2000-01-01T00:00:00Z'));
            const form = { ...createMealManageFormValue(instant), date, time };
            const payload = buildMealManageDto(form, {
                aiSessions: [],
                buildDateTime: () => instant,
                convertRecipeGramsToServings: (_recipe, amount) => amount,
                manualTotals: NO_NUTRIENTS,
            });
            const stored: Meal = {
                id: 'meal',
                date: payload.date.toISOString(),
                items: [],
                isNutritionAutoCalculated: true,
                totalCalories: 0,
                totalProteins: 0,
                totalFats: 0,
                totalCarbs: 0,
                totalFiber: 0,
                totalAlcohol: 0,
            };
            expect(buildMealManageFormPatchValue(stored)).toMatchObject({ date, time });
            const start = toLocalDayStartIso(instant);
            const end = toLocalDayEndIso(instant);
            expect(start).toBeDefined();
            expect(end).toBeDefined();
            expect(stored.date >= (start ?? '') && stored.date <= (end ?? '')).toBe(true);
            expect(createMealManageFormValue(new Date(stored.date)).date).toBe(date);
        },
    );
});

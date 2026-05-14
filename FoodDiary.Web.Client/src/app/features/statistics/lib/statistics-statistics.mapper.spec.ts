import { describe, expect, it } from 'vitest';

import type { AggregatedStatistics } from '../models/statistics.data';
import { mapStatistics } from './statistics-statistics.mapper';

const FIRST_CALORIES = 1800;
const SECOND_CALORIES = 2200;
const FIRST_PROTEINS = 120;
const SECOND_PROTEINS = 140;
const FIRST_FATS = 70;
const SECOND_FATS = 80;
const FIRST_CARBS = 160;
const SECOND_CARBS = 180;
const FIRST_FIBER = 20;
const SECOND_FIBER = 25;

describe('statistics-statistics.mapper', () => {
    it('maps statistics series and aggregates nutrients', () => {
        const result = mapStatistics([createStat('2026-05-01'), createStat('2026-05-02', true)]);

        expect(result.calories).toEqual([FIRST_CALORIES, SECOND_CALORIES]);
        expect(result.nutrientsStatistic.proteins).toEqual([FIRST_PROTEINS, SECOND_PROTEINS]);
        expect(result.aggregatedNutrients).toEqual({
            proteins: FIRST_PROTEINS + SECOND_PROTEINS,
            fats: FIRST_FATS + SECOND_FATS,
            carbs: FIRST_CARBS + SECOND_CARBS,
            fiber: FIRST_FIBER + SECOND_FIBER,
        });
    });
});

function createStat(date: string, second = false): AggregatedStatistics {
    return {
        dateFrom: new Date(`${date}T00:00:00Z`),
        dateTo: new Date(`${date}T23:59:59Z`),
        totalCalories: second ? SECOND_CALORIES : FIRST_CALORIES,
        averageProteins: second ? SECOND_PROTEINS : FIRST_PROTEINS,
        averageFats: second ? SECOND_FATS : FIRST_FATS,
        averageCarbs: second ? SECOND_CARBS : FIRST_CARBS,
        averageFiber: second ? SECOND_FIBER : FIRST_FIBER,
    };
}

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
const FIRST_TOTAL_PROTEINS = 240;
const SECOND_TOTAL_PROTEINS = 420;
const FIRST_TOTAL_FATS = 140;
const SECOND_TOTAL_FATS = 240;
const FIRST_TOTAL_CARBS = 320;
const SECOND_TOTAL_CARBS = 540;
const FIRST_TOTAL_FIBER = 40;
const SECOND_TOTAL_FIBER = 75;

describe('statistics-statistics.mapper', () => {
    it('maps statistics series and aggregates nutrients', () => {
        const result = mapStatistics([createStat('2026-05-01'), createStat('2026-05-02', true)]);

        expect(result.calories).toEqual([FIRST_CALORIES, SECOND_CALORIES]);
        expect(result.nutrientsStatistic.proteins).toEqual([FIRST_PROTEINS, SECOND_PROTEINS]);
        expect(result.aggregatedNutrients).toEqual({
            proteins: FIRST_TOTAL_PROTEINS + SECOND_TOTAL_PROTEINS,
            fats: FIRST_TOTAL_FATS + SECOND_TOTAL_FATS,
            carbs: FIRST_TOTAL_CARBS + SECOND_TOTAL_CARBS,
            fiber: FIRST_TOTAL_FIBER + SECOND_TOTAL_FIBER,
        });
    });
});

function createStat(date: string, second = false): AggregatedStatistics {
    if (second) {
        return {
            dateFrom: new Date(`${date}T00:00:00Z`),
            dateTo: new Date(`${date}T23:59:59Z`),
            totalCalories: SECOND_CALORIES,
            averageProteins: SECOND_PROTEINS,
            averageFats: SECOND_FATS,
            averageCarbs: SECOND_CARBS,
            averageFiber: SECOND_FIBER,
            totalProteins: SECOND_TOTAL_PROTEINS,
            totalFats: SECOND_TOTAL_FATS,
            totalCarbs: SECOND_TOTAL_CARBS,
            totalFiber: SECOND_TOTAL_FIBER,
        };
    }

    return {
        dateFrom: new Date(`${date}T00:00:00Z`),
        dateTo: new Date(`${date}T23:59:59Z`),
        totalCalories: FIRST_CALORIES,
        averageProteins: FIRST_PROTEINS,
        averageFats: FIRST_FATS,
        averageCarbs: FIRST_CARBS,
        averageFiber: FIRST_FIBER,
        totalProteins: FIRST_TOTAL_PROTEINS,
        totalFats: FIRST_TOTAL_FATS,
        totalCarbs: FIRST_TOTAL_CARBS,
        totalFiber: FIRST_TOTAL_FIBER,
    };
}

import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import type { DailyMicronutrient, Micronutrient } from '../models/usda.data';
import { DAILY_MICRONUTRIENT_IDS, MINERAL_NUTRIENT_IDS, VITAMIN_NUTRIENT_IDS } from './usda-nutrient.constants';
import type { DailyMicronutrientView, MicronutrientView } from './usda-view.types';

const MIN_PERCENT_DAILY_VALUE = 0;

const VITAMIN_IDS = new Set<number>(VITAMIN_NUTRIENT_IDS);
const MINERAL_IDS = new Set<number>(MINERAL_NUTRIENT_IDS);
const DAILY_KEY_NUTRIENT_IDS = new Set<number>(DAILY_MICRONUTRIENT_IDS);

export function buildVitaminMicronutrientViews(nutrients: Micronutrient[]): MicronutrientView[] {
    return buildMicronutrientViews(nutrients, VITAMIN_IDS);
}

export function buildMineralMicronutrientViews(nutrients: Micronutrient[]): MicronutrientView[] {
    return buildMicronutrientViews(nutrients, MINERAL_IDS);
}

export function buildDailyMicronutrientViews(nutrients: DailyMicronutrient[]): DailyMicronutrientView[] {
    return nutrients
        .filter(nutrient => DAILY_KEY_NUTRIENT_IDS.has(nutrient.nutrientId))
        .map(withPercentDailyValueWidth)
        .sort((a, b) => a.name.localeCompare(b.name));
}

function buildMicronutrientViews(nutrients: Micronutrient[], nutrientIds: ReadonlySet<number>): MicronutrientView[] {
    return nutrients.filter(nutrient => nutrientIds.has(nutrient.nutrientId)).map(withPercentDailyValueWidth);
}

function withPercentDailyValueWidth<T extends { percentDailyValue: number | null }>(
    nutrient: T,
): T & { percentDailyValueWidth: number | null } {
    return {
        ...nutrient,
        percentDailyValueWidth: calculatePercentDailyValueWidth(nutrient.percentDailyValue),
    };
}

function calculatePercentDailyValueWidth(percentDailyValue: number | null): number | null {
    if (percentDailyValue === null) {
        return null;
    }

    return Math.min(Math.max(percentDailyValue, MIN_PERCENT_DAILY_VALUE), PERCENT_MULTIPLIER);
}

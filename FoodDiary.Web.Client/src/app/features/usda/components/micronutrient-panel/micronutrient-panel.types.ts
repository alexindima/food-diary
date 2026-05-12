import type { Micronutrient } from '../../models/usda.data';

export type MicronutrientView = Micronutrient & {
    percentDailyValueWidth: number | null;
};

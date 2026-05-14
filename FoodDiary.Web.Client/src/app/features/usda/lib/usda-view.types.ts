import type { DailyMicronutrient, HealthAreaGrade, Micronutrient } from '../models/usda.data';

export type MicronutrientView = Micronutrient & {
    percentDailyValueWidth: number | null;
};

export type DailyMicronutrientView = DailyMicronutrient & {
    percentDailyValueWidth: number | null;
};

export type HealthAreaDisplay = {
    key: string;
    labelKey: string;
    icon: string;
    score: number;
    grade: HealthAreaGrade;
    gradeKey: string;
    gradeClass: string;
    strokeDasharray: string;
};

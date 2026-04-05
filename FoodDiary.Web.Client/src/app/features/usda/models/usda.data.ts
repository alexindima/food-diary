export interface UsdaFood {
    fdcId: number;
    description: string;
    foodCategory: string | null;
}

export interface Micronutrient {
    nutrientId: number;
    name: string;
    unit: string;
    amountPer100g: number;
    dailyValue: number | null;
    percentDailyValue: number | null;
}

export interface UsdaFoodPortion {
    id: number;
    amount: number;
    measureUnitName: string;
    gramWeight: number;
    portionDescription: string | null;
    modifier: string | null;
}

export interface UsdaFoodDetail {
    fdcId: number;
    description: string;
    foodCategory: string | null;
    nutrients: Micronutrient[];
    portions: UsdaFoodPortion[];
    healthScores: HealthAreaScores | null;
}

export interface DailyMicronutrientSummary {
    date: string;
    linkedProductCount: number;
    totalProductCount: number;
    nutrients: DailyMicronutrient[];
    healthScores: HealthAreaScores | null;
}

export type HealthAreaGrade = 'unknown' | 'low' | 'fair' | 'good' | 'excellent';

export interface HealthAreaScore {
    score: number;
    grade: HealthAreaGrade;
}

export interface HealthAreaScores {
    heart: HealthAreaScore;
    bone: HealthAreaScore;
    immune: HealthAreaScore;
    energy: HealthAreaScore;
    antioxidant: HealthAreaScore;
}

export interface DailyMicronutrient {
    nutrientId: number;
    name: string;
    unit: string;
    totalAmount: number;
    dailyValue: number | null;
    percentDailyValue: number | null;
}

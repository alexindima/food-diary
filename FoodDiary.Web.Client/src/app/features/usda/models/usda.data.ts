export type UsdaFood = {
    fdcId: number;
    description: string;
    foodCategory: string | null;
};

export type Micronutrient = {
    nutrientId: number;
    name: string;
    unit: string;
    amountPer100g: number;
    dailyValue: number | null;
    percentDailyValue: number | null;
};

export type UsdaFoodPortion = {
    id: number;
    amount: number;
    measureUnitName: string;
    gramWeight: number;
    portionDescription: string | null;
    modifier: string | null;
};

export type UsdaFoodDetail = {
    fdcId: number;
    description: string;
    foodCategory: string | null;
    nutrients: Micronutrient[];
    portions: UsdaFoodPortion[];
    healthScores: HealthAreaScores | null;
};

export type DailyMicronutrientSummary = {
    date: string;
    linkedProductCount: number;
    totalProductCount: number;
    nutrients: DailyMicronutrient[];
    healthScores: HealthAreaScores | null;
};

export type HealthAreaGrade = 'unknown' | 'low' | 'fair' | 'good' | 'excellent';

export type HealthAreaScore = {
    score: number;
    grade: HealthAreaGrade;
};

export type HealthAreaScores = {
    heart: HealthAreaScore;
    bone: HealthAreaScore;
    immune: HealthAreaScore;
    energy: HealthAreaScore;
    antioxidant: HealthAreaScore;
};

export type DailyMicronutrient = {
    nutrientId: number;
    name: string;
    unit: string;
    totalAmount: number;
    dailyValue: number | null;
    percentDailyValue: number | null;
};

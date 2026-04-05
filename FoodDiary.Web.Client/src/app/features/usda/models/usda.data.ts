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
}

export interface DailyMicronutrientSummary {
    date: string;
    linkedProductCount: number;
    totalProductCount: number;
    nutrients: DailyMicronutrient[];
}

export interface DailyMicronutrient {
    nutrientId: number;
    name: string;
    unit: string;
    totalAmount: number;
    dailyValue: number | null;
    percentDailyValue: number | null;
}

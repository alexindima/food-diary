export interface TdeeInsight {
    estimatedTdee: number | null;
    adaptiveTdee: number | null;
    bmr: number | null;
    suggestedCalorieTarget: number | null;
    currentCalorieTarget: number | null;
    weightTrendPerWeek: number | null;
    confidence: 'none' | 'low' | 'medium' | 'high';
    dataDaysUsed: number;
    goalAdjustmentHint: string | null;
}

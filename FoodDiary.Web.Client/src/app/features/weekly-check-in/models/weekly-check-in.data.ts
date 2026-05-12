export type WeeklyCheckInData = {
    thisWeek: WeekSummary;
    lastWeek: WeekSummary;
    trends: WeekTrend;
    suggestions: string[];
};

export type WeekSummary = {
    totalCalories: number;
    avgDailyCalories: number;
    avgProteins: number;
    avgFats: number;
    avgCarbs: number;
    mealsLogged: number;
    daysLogged: number;
    weightStart: number | null;
    weightEnd: number | null;
    waistStart: number | null;
    waistEnd: number | null;
    totalHydrationMl: number;
    avgDailyHydrationMl: number;
};

export type WeekTrend = {
    calorieChange: number;
    proteinChange: number;
    fatChange: number;
    carbChange: number;
    weightChange: number | null;
    waistChange: number | null;
    hydrationChange: number;
    mealsLoggedChange: number;
};

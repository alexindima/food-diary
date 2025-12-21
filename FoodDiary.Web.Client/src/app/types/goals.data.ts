export interface GoalsResponse {
    dailyCalorieTarget?: number | null;
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    fiberTarget?: number | null;
    waterGoal?: number | null;
    desiredWeight?: number | null;
    desiredWaist?: number | null;
}

export interface UpdateGoalsRequest {
    dailyCalorieTarget?: number | null;
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    fiberTarget?: number | null;
    waterGoal?: number | null;
    desiredWeight?: number | null;
    desiredWaist?: number | null;
}

export type GoalsResponse = {
    dailyCalorieTarget?: number | null;
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    fiberTarget?: number | null;
    waterGoal?: number | null;
    desiredWeight?: number | null;
    desiredWaist?: number | null;
    calorieCyclingEnabled: boolean;
    mondayCalories?: number | null;
    tuesdayCalories?: number | null;
    wednesdayCalories?: number | null;
    thursdayCalories?: number | null;
    fridayCalories?: number | null;
    saturdayCalories?: number | null;
    sundayCalories?: number | null;
};

export type UpdateGoalsRequest = {
    dailyCalorieTarget?: number | null;
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    fiberTarget?: number | null;
    waterGoal?: number | null;
    desiredWeight?: number | null;
    desiredWaist?: number | null;
    calorieCyclingEnabled?: boolean | null;
    mondayCalories?: number | null;
    tuesdayCalories?: number | null;
    wednesdayCalories?: number | null;
    thursdayCalories?: number | null;
    fridayCalories?: number | null;
    saturdayCalories?: number | null;
    sundayCalories?: number | null;
};

export const DAYS_OF_WEEK = [
    { key: 'mondayCalories' as const, labelKey: 'GOALS_PAGE.DAY_MONDAY' },
    { key: 'tuesdayCalories' as const, labelKey: 'GOALS_PAGE.DAY_TUESDAY' },
    { key: 'wednesdayCalories' as const, labelKey: 'GOALS_PAGE.DAY_WEDNESDAY' },
    { key: 'thursdayCalories' as const, labelKey: 'GOALS_PAGE.DAY_THURSDAY' },
    { key: 'fridayCalories' as const, labelKey: 'GOALS_PAGE.DAY_FRIDAY' },
    { key: 'saturdayCalories' as const, labelKey: 'GOALS_PAGE.DAY_SATURDAY' },
    { key: 'sundayCalories' as const, labelKey: 'GOALS_PAGE.DAY_SUNDAY' },
] as const;

export type DayCalorieKey = (typeof DAYS_OF_WEEK)[number]['key'];

export type AdminRetentionCohort = {
    date: string;
    registered: number;
    activatedWithinSevenDays: number | null;
    day1: number | null;
    day7: number | null;
    day30: number | null;
};

export type AdminRetentionReport = {
    fromUtc: string;
    toUtc: string;
    asOfUtc: string;
    cohortFromUtc: string;
    cohortToUtc: string;
    mealEntriesInPeriod: number;
    activeUsersInPeriod: number;
    cohorts: AdminRetentionCohort[];
    activityByDay: Array<{ date: string; activeUsers: number; mealEntries: number }>;
};

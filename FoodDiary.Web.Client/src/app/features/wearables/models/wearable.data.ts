export type WearableProvider = 'Fitbit' | 'GoogleFit' | 'Garmin' | 'AppleHealth';

export type WearableConnection = {
    provider: string;
    externalUserId: string;
    isActive: boolean;
    lastSyncedAtUtc: string | null;
    connectedAtUtc: string;
};

export type WearableDailySummary = {
    date: string;
    steps: number | null;
    heartRate: number | null;
    caloriesBurned: number | null;
    activeMinutes: number | null;
    sleepMinutes: number | null;
};

export type WearableAuthUrl = {
    authorizationUrl: string;
};

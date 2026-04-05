export type WearableProvider = 'Fitbit' | 'GoogleFit' | 'Garmin' | 'AppleHealth';

export interface WearableConnection {
    provider: string;
    externalUserId: string;
    isActive: boolean;
    lastSyncedAtUtc: string | null;
    connectedAtUtc: string;
}

export interface WearableDailySummary {
    date: string;
    steps: number | null;
    heartRate: number | null;
    caloriesBurned: number | null;
    activeMinutes: number | null;
    sleepMinutes: number | null;
}

export interface WearableAuthUrl {
    authorizationUrl: string;
}

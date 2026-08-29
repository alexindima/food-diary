export type ActiveSession = {
    id: string;
    isCurrent: boolean;
    authProvider: string | null;
    browser: string | null;
    operatingSystem: string | null;
    deviceType: string | null;
    createdAtUtc: string;
    lastActiveAtUtc: string;
};

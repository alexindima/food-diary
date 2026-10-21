export const PWA_PROMOTION_STORAGE_KEY = 'fd-pwa-install-promotion-v1';
export const PWA_PROMOTION_VISITS_STORAGE_KEY = 'fd-pwa-install-visits-v1';
export const PWA_PROMOTION_MIN_VISITS = 3;
export const PWA_PROMOTION_SESSION_IDLE_MS = 1_800_000;
export const PWA_PROMOTION_SNOOZE_MS = 2_592_000_000;

export type PwaInstallPromotionState = {
    visits: number;
    lastSeenAt: number | null;
    snoozedUntil: number | null;
    never: boolean;
};

export function parseInstallPromotionState(value: unknown): PwaInstallPromotionState {
    const data = isRecord(value) ? value : {};
    const visits = data['visits'];
    return {
        visits: typeof visits === 'number' && Number.isSafeInteger(visits) && visits >= 0 ? Math.min(visits, PWA_PROMOTION_MIN_VISITS) : 0,
        lastSeenAt: parseTimestamp(data['lastSeenAt']),
        snoozedUntil: parseTimestamp(data['snoozedUntil']),
        never: data['never'] === true,
    };
}

export function recordInstallPromotionVisit(state: PwaInstallPromotionState, now: number): PwaInstallPromotionState {
    const startsVisit = state.visits === 0 || state.lastSeenAt === null || now - state.lastSeenAt >= PWA_PROMOTION_SESSION_IDLE_MS;
    return {
        ...state,
        visits: Math.min(state.visits + (startsVisit ? 1 : 0), PWA_PROMOTION_MIN_VISITS),
        lastSeenAt: now,
    };
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function parseTimestamp(value: unknown): number | null {
    return typeof value === 'number' && Number.isSafeInteger(value) && value >= 0 ? value : null;
}

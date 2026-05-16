import { InjectionToken } from '@angular/core';

const DEFAULT_FASTING_HISTORY_PAGE_SIZE = 10;

export type FastingApiLimitsConfig = {
    historyPageSize: number;
};

export const FASTING_API_LIMITS = new InjectionToken<FastingApiLimitsConfig>('FASTING_API_LIMITS', {
    providedIn: 'root',
    factory: (): FastingApiLimitsConfig => ({
        historyPageSize: DEFAULT_FASTING_HISTORY_PAGE_SIZE,
    }),
});

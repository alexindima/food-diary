import { InjectionToken } from '@angular/core';

const DEFAULT_REPORT_REASON_MAX_LENGTH = 1_000;

export const REPORT_REASON_MAX_LENGTH = new InjectionToken<number>('REPORT_REASON_MAX_LENGTH', {
    providedIn: 'root',
    factory: (): number => DEFAULT_REPORT_REASON_MAX_LENGTH,
});

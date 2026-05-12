import { InjectionToken } from '@angular/core';

const AUTOFILL_SYNC_FAST_DELAY_MS = 100;
const AUTOFILL_SYNC_SHORT_DELAY_MS = 500;
const AUTOFILL_SYNC_MEDIUM_DELAY_MS = 1000;
const AUTOFILL_SYNC_LONG_DELAY_MS = 2500;
const AUTOFILL_SYNC_FINAL_DELAY_MS = 5000;

export const FD_UI_INPUT_AUTOFILL_SYNC_DELAYS_MS = new InjectionToken<readonly number[]>('FD_UI_INPUT_AUTOFILL_SYNC_DELAYS_MS', {
    providedIn: 'root',
    factory: (): readonly number[] => [
        AUTOFILL_SYNC_FAST_DELAY_MS,
        AUTOFILL_SYNC_SHORT_DELAY_MS,
        AUTOFILL_SYNC_MEDIUM_DELAY_MS,
        AUTOFILL_SYNC_LONG_DELAY_MS,
        AUTOFILL_SYNC_FINAL_DELAY_MS,
    ],
});

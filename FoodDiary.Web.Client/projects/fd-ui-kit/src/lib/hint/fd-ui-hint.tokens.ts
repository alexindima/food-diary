import { InjectionToken } from '@angular/core';

const DEFAULT_HINT_SHOW_DELAY_MS = 500;

export const FD_UI_HINT_SHOW_DELAY_MS = new InjectionToken<number>('FD_UI_HINT_SHOW_DELAY_MS', {
    providedIn: 'root',
    factory: (): number => DEFAULT_HINT_SHOW_DELAY_MS,
});

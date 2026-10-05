import { InjectionToken } from '@angular/core';

export const FD_UI_DIALOG_DISMISSAL_LOCK = new InjectionToken<() => () => void>('FD_UI_DIALOG_DISMISSAL_LOCK');

export const FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY = new InjectionToken<string>('FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY', {
    providedIn: 'root',
    factory: (): string => '(max-width: 768px)',
});

import { InjectionToken } from '@angular/core';

export const FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY = new InjectionToken<string>('FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY', {
    providedIn: 'root',
    factory: (): string => '(max-width: 420px)',
});

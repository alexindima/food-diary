import { DestroyRef, inject, InjectionToken } from '@angular/core';

/** Optional capabilities published by the live page/dialog. No persistence capability. */
export type DevelopmentScreen = {
    kind: string;
    read: () => unknown;
    isDirty?: () => boolean;
    draftSchema?: Record<string, unknown>;
    fillDraft?: (draft: unknown) => void;
    clearDraft?: () => void;
    setFiltersAsync?: (filters: unknown) => void | Promise<void>;
    setFilters?: (filters: unknown) => void;
    openEntityAsync?: (id: string) => void | Promise<void>;
    openDraft?: () => void;
    beforeNavigate?: () => void;
};

export const DEVELOPMENT_SCREENS = new InjectionToken<{ attach: (screen: DevelopmentScreen) => () => void }>('DEVELOPMENT_SCREENS');
export type DevelopmentResponseMetadata = { apiBuild: string | null; apiInstance: string | null };
export type DevelopmentRequestCompletion = ((status?: number, metadata?: DevelopmentResponseMetadata) => void) & { requestId?: number };

export const DEVELOPMENT_REQUESTS = new InjectionToken<{
    observe: (method: string, url: string) => DevelopmentRequestCompletion;
}>('DEVELOPMENT_REQUESTS');

export function connectDevelopmentScreen(screen: DevelopmentScreen): void {
    const screens = inject(DEVELOPMENT_SCREENS, { optional: true });
    if (screens !== null) {
        inject(DestroyRef).onDestroy(screens.attach(screen));
    }
}

export class DevelopmentToolError extends Error {
    public override readonly name = 'DevelopmentToolError';
}

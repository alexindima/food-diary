import { DestroyRef, inject, type InjectionToken } from '@angular/core';

export type WebmcpScreenState = {
    kind: string;
    read: () => unknown;
    isDirty?: () => boolean;
    beforeNavigate?: () => void;
};

/** Optional capabilities, deliberately excluding persistence and permission changes. */
export type WebmcpScreen<TFilters = unknown> = WebmcpScreenState & {
    filterFields?: readonly string[];
    draftSchema?: Record<string, unknown>;
    fillDraft?: (draft: unknown) => void;
    clearDraft?: () => void;
    setFiltersAsync?: (filters: TFilters) => void | Promise<void>;
    setFilters?: (filters: TFilters) => void;
    openEntityAsync?: (id: string) => void | Promise<void>;
    openDraft?: () => void;
};

export type WebmcpScreenHost<TScreen extends WebmcpScreenState> = { attach: (screen: TScreen) => () => void };

export type WebmcpScreenContext = {
    kind: string;
    revision: number;
    data: unknown;
    dirty: boolean;
    draftSchema: Record<string, unknown> | null;
    actions: {
        fillDraft: boolean;
        clearDraft: boolean;
        setFiltersAsync: boolean;
        openEntityAsync: boolean;
        openDraft: boolean;
    };
};

export function connectWebmcpScreen<TScreen extends WebmcpScreenState>(
    token: InjectionToken<WebmcpScreenHost<TScreen>>,
    screen: TScreen,
): void {
    const screens = inject(token, { optional: true });
    if (screens !== null) {
        inject(DestroyRef).onDestroy(screens.attach(screen));
    }
}

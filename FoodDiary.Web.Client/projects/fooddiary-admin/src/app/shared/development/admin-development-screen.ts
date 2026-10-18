import { DestroyRef, inject, InjectionToken } from '@angular/core';

export type AdminDevelopmentScreen = {
    kind: string;
    read: () => unknown;
    filterFields?: readonly string[];
    setFiltersAsync?: (params: Record<string, string | null>) => Promise<void>;
    openEntityAsync?: (id: string) => Promise<void>;
    isDirty?: () => boolean;
};

export const ADMIN_DEVELOPMENT_SCREENS = new InjectionToken<{ attach: (screen: AdminDevelopmentScreen) => () => void }>(
    'ADMIN_DEVELOPMENT_SCREENS',
);

export function connectAdminDevelopmentScreen(screen: AdminDevelopmentScreen): void {
    const screens = inject(ADMIN_DEVELOPMENT_SCREENS, { optional: true });
    if (screens !== null) {
        inject(DestroyRef).onDestroy(screens.attach(screen));
    }
}

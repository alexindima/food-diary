import { InjectionToken } from '@angular/core';
import { connectWebmcpScreen, type WebmcpScreen } from 'fd-webmcp';

export type AdminDevelopmentScreen = WebmcpScreen<Record<string, string | null>>;

export const ADMIN_DEVELOPMENT_SCREENS = new InjectionToken<{ attach: (screen: AdminDevelopmentScreen) => () => void }>(
    'ADMIN_DEVELOPMENT_SCREENS',
);

export function connectAdminDevelopmentScreen(screen: AdminDevelopmentScreen): void {
    connectWebmcpScreen(ADMIN_DEVELOPMENT_SCREENS, screen);
}

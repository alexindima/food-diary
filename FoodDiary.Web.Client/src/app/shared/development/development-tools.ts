import { InjectionToken } from '@angular/core';
import { connectWebmcpScreen, type WebmcpScreen as DevelopmentScreen } from 'fd-webmcp';

export type { WebmcpScreen as DevelopmentScreen } from 'fd-webmcp';
export const DEVELOPMENT_SCREENS = new InjectionToken<{ attach: (screen: DevelopmentScreen) => () => void }>('DEVELOPMENT_SCREENS');
export type DevelopmentResponseMetadata = { apiBuild: string | null; apiInstance: string | null };
export type DevelopmentRequestCompletion = ((status?: number, metadata?: DevelopmentResponseMetadata) => void) & { requestId?: number };

export const DEVELOPMENT_REQUESTS = new InjectionToken<{
    observe: (method: string, url: string) => DevelopmentRequestCompletion;
}>('DEVELOPMENT_REQUESTS');

export function connectDevelopmentScreen(screen: DevelopmentScreen): void {
    connectWebmcpScreen(DEVELOPMENT_SCREENS, screen);
}

export class DevelopmentToolError extends Error {
    public override readonly name = 'DevelopmentToolError';
}

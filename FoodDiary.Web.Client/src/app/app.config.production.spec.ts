import { createEnvironmentInjector, EnvironmentInjector, ErrorHandler } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SwRegistrationOptions } from '@angular/service-worker';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { environment as productionEnvironment } from '../environments/environment.prod';

describe('Production client application providers', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it.each([
        { surface: 'browser', capacitor: undefined, serviceWorkerEnabled: true },
        { surface: 'native shell', capacitor: { getPlatform: (): string => 'android' }, serviceWorkerEnabled: false },
    ])('registers the expected service worker policy and reports errors in the $surface', async ({ capacitor, serviceWorkerEnabled }) => {
        const parent = TestBed.inject(EnvironmentInjector);
        vi.resetModules();
        vi.stubGlobal('ngDevMode', false);
        vi.stubGlobal('Capacitor', capacitor);
        const { environment } = await import('../environments/environment');
        const originalErrorHandler = environment.enableGlobalErrorHandler;
        environment.enableGlobalErrorHandler = productionEnvironment.enableGlobalErrorHandler;
        let injector: EnvironmentInjector | undefined;

        try {
            const [{ appConfig }, { FrontendObservabilityService }] = await Promise.all([
                import('./app.config'),
                import('./services/frontend-observability.service'),
            ]);
            const recordClientError = vi.fn();
            injector = createEnvironmentInjector(
                [...appConfig.providers, { provide: FrontendObservabilityService, useValue: { recordClientError } }],
                parent,
            );
            expect(injector.get(SwRegistrationOptions)).toMatchObject({
                enabled: serviceWorkerEnabled,
                registrationStrategy: 'registerWhenStable:30000',
            });
            const error = new Error('Production navigation failed');
            injector.get(ErrorHandler).handleError(error);
            expect(recordClientError).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({ message: error.message, location: window.location.href }),
            );
        } finally {
            injector?.destroy();
            environment.enableGlobalErrorHandler = originalErrorHandler;
        }
    });
});

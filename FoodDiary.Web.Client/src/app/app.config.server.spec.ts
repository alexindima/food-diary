// @vitest-environment node
import { createEnvironmentInjector, createNgModule, CSP_NONCE, NgModule, PLATFORM_ID, REQUEST_CONTEXT } from '@angular/core';
import { SwRegistrationOptions } from '@angular/service-worker';
import { afterEach, describe, expect, it, vi } from 'vitest';

declare global {
    const process: { readonly env: Readonly<Record<string, string | undefined>> };
}

@NgModule({})
class ServerInjectorFixturesModule {}

describe('Server application providers', () => {
    afterEach(() => {
        vi.unstubAllEnvs();
        vi.unstubAllGlobals();
    });

    it.each([
        { origin: undefined, expectedOrigin: 'http://localhost:5300', context: null, nonce: null },
        {
            origin: 'https://fooddiary-ssr.example.test',
            expectedOrigin: 'https://fooddiary-ssr.example.test',
            context: { cspNonce: 'ssr-test-nonce' },
            nonce: 'ssr-test-nonce',
        },
    ])('uses the public recipes origin and request nonce for $expectedOrigin', async ({ origin, expectedOrigin, context, nonce }) => {
        vi.resetModules();
        vi.stubEnv('SSR_API_ORIGIN', origin);
        vi.stubGlobal('ngServerMode', true);
        const [{ config }, { PUBLIC_RECIPE_API_URL }, { SERVER_TRANSLATIONS }] = await Promise.all([
            import('./app.config.server'),
            import('./features/recipes/api/public-recipe-api.token'),
            import('./shared/i18n/server-translations.token'),
        ]);
        const parent = createNgModule(ServerInjectorFixturesModule);
        const injector = createEnvironmentInjector(
            [...config.providers, { provide: PLATFORM_ID, useValue: 'server' }, { provide: REQUEST_CONTEXT, useValue: context }],
            parent.injector,
        );
        try {
            expect(injector.get(CSP_NONCE)).toBe(nonce);
            expect(injector.get(PUBLIC_RECIPE_API_URL)).toBe(`${expectedOrigin}/api/v1/recipes/public`);
            expect(injector.get(SwRegistrationOptions, null)).toBeNull();
            const bundles = injector.get(SERVER_TRANSLATIONS);
            expect(bundles['en'].core).toHaveProperty(['COMMON', 'SAVE'], 'Save');
            expect(bundles['ru'].core).toHaveProperty(['COMMON', 'SAVE'], 'Сохранить');
        } finally {
            injector.destroy();
            parent.destroy();
        }
    });
});

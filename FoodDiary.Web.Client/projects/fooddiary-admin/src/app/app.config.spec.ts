import { createApplication } from '@angular/platform-browser';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment as production } from '../environments/environment.prod';

describe('Admin browser application configuration', () => {
    it('adds production global error listeners even after development configuration is loaded', async () => {
        vi.resetModules();
        const errorEvents = new Set(['error', 'unhandledrejection']);
        const add = vi.spyOn(window, 'addEventListener');
        const remove = vi.spyOn(window, 'removeEventListener');
        onTestFinished(() => {
            add.mockRestore();
            remove.mockRestore();
            vi.resetModules();
        });

        const { appConfig: developmentConfig } = await import('./app.config');
        const developmentApp = await createApplication(developmentConfig);
        try {
            expect(add.mock.calls.filter(([name]) => errorEvents.has(name))).toEqual([]);
        } finally {
            developmentApp.destroy();
        }
        add.mockClear();
        remove.mockClear();

        // The application config reads its environment when the module is evaluated.
        vi.resetModules();
        const { environment } = await import('../environments/environment');
        const previous = environment.enableGlobalErrorHandler;
        environment.enableGlobalErrorHandler = production.enableGlobalErrorHandler;
        try {
            const { appConfig } = await import('./app.config');
            const app = await createApplication(appConfig);
            const listeners = add.mock.calls.filter(([name]) => errorEvents.has(name));
            try {
                expect(listeners.map(([name]) => name).sort()).toEqual([...errorEvents]);
            } finally {
                app.destroy();
            }

            for (const [name, listener] of listeners) {
                expect(remove).toHaveBeenCalledWith(name, listener);
            }
        } finally {
            environment.enableGlobalErrorHandler = previous;
        }
    });
});

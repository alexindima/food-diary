import { createApplication } from '@angular/platform-browser';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../environments/environment';
import { environment as production } from '../environments/environment.prod';

describe('Admin production application configuration', () => {
    it('adds global browser error listeners for the application lifetime', async () => {
        const previous = environment.enableGlobalErrorHandler;
        environment.enableGlobalErrorHandler = production.enableGlobalErrorHandler;
        const add = vi.spyOn(window, 'addEventListener');
        const remove = vi.spyOn(window, 'removeEventListener');
        onTestFinished(() => {
            environment.enableGlobalErrorHandler = previous;
            add.mockRestore();
            remove.mockRestore();
        });
        const { appConfig } = await import('./app.config');
        const app = await createApplication(appConfig);
        try {
            expect(add.mock.calls.some(([name]) => name === 'error')).toBe(true);
            expect(add.mock.calls.some(([name]) => name === 'unhandledrejection')).toBe(true);
        } finally {
            app.destroy();
        }
        expect(remove.mock.calls.some(([name]) => name === 'error')).toBe(true);
        expect(remove.mock.calls.some(([name]) => name === 'unhandledrejection')).toBe(true);
    });
});

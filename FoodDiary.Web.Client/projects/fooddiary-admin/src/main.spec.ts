import type * as platformBrowser from '@angular/platform-browser';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

const { bootstrap } = vi.hoisted(() => ({
    bootstrap: vi.fn<typeof platformBrowser.bootstrapApplication>(),
}));

vi.mock('@angular/platform-browser', async importOriginal => ({
    ...(await importOriginal<typeof platformBrowser>()),
    bootstrapApplication: bootstrap,
}));

describe('Admin browser entry point', () => {
    it('reports the startup failure when Angular cannot bootstrap the admin shell', async () => {
        const failure = new Error('Admin shell initialization failed');
        const report = vi.spyOn(console, 'error').mockImplementation(() => {});
        onTestFinished(() => {
            report.mockRestore();
        });
        bootstrap.mockRejectedValueOnce(failure);

        await import('./main');
        const { AppComponent } = await import('./app/app');
        const { appConfig } = await import('./app/app.config');

        expect(bootstrap).toHaveBeenCalledExactlyOnceWith(AppComponent, appConfig);
        expect(report).toHaveBeenCalledExactlyOnceWith(failure);
    });
});

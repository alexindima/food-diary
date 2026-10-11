import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { LocalizationService } from './localization.service';
import { publicSiteLocaleGuard } from './public-site-locale.guard';

const locale = {
    applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined),
    loadRouteTranslationsAsync: vi.fn().mockResolvedValue(undefined),
};
const auth = { isAuthenticated: vi.fn(() => false) };

beforeEach(() => {
    vi.clearAllMocks();
    auth.isAuthenticated.mockReturnValue(false);
    TestBed.configureTestingModule({
        providers: [provideRouter([]), { provide: LocalizationService, useValue: locale }, { provide: AuthService, useValue: auth }],
    });
});

async function activateAsync(url: string): Promise<void> {
    const state = TestBed.inject(Router).routerState.snapshot;
    state.url = url;
    await TestBed.runInInjectionContext(async () => publicSiteLocaleGuard(state.root, state));
}

describe('public page language', () => {
    it('loads the URL language before rendering a guest page', async () => {
        await activateAsync('/ru/calorie-counter');
        expect(locale.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
        expect(locale.loadRouteTranslationsAsync).toHaveBeenCalledWith('/ru/calorie-counter');
    });

    it('preserves the signed-in account language during a landing redirect', async () => {
        auth.isAuthenticated.mockReturnValue(true);
        await activateAsync('/?source=public');
        expect(locale.applyLanguagePreferenceAsync).not.toHaveBeenCalled();
        expect(locale.loadRouteTranslationsAsync).not.toHaveBeenCalled();
    });

    it('still honors an explicit public page language for a signed-in visitor', async () => {
        auth.isAuthenticated.mockReturnValue(true);
        await activateAsync('/ru/pricing');
        expect(locale.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
    });
});

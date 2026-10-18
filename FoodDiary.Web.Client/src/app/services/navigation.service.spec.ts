import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { AuthMode } from '../shared/auth/auth.data';
import { LocalizationService } from '../shared/i18n/localization.service';
import { NavigationService } from './navigation.service';

type RouterMock = {
    navigate: ReturnType<typeof vi.fn<Router['navigate']>>;
    navigateByUrl: ReturnType<typeof vi.fn<Router['navigateByUrl']>>;
};

function createNavigationContext(): {
    service: NavigationService;
    routerSpy: RouterMock;
    currentLanguage: ReturnType<typeof vi.fn<LocalizationService['getCurrentLanguage']>>;
} {
    const routerSpy: RouterMock = { navigate: vi.fn(), navigateByUrl: vi.fn() };
    routerSpy.navigate.mockResolvedValue(true);
    routerSpy.navigateByUrl.mockResolvedValue(true);
    const currentLanguage = vi.fn<LocalizationService['getCurrentLanguage']>().mockReturnValue('en');
    TestBed.configureTestingModule({
        providers: [
            NavigationService,
            { provide: Router, useValue: routerSpy },
            { provide: LocalizationService, useValue: { getCurrentLanguage: currentLanguage } },
        ],
    });
    return { service: TestBed.inject(NavigationService), routerSpy, currentLanguage };
}

describe('NavigationService', () => {
    let service: NavigationService;
    let routerSpy: RouterMock;

    beforeEach(() => {
        ({ service, routerSpy } = createNavigationContext());
    });

    it('should navigate to home', async () => {
        await service.navigateToHomeAsync();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should navigate to landing', async () => {
        await service.navigateToLandingAsync();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should navigate to dashboard when returnUrl is empty', async () => {
        await service.navigateToReturnUrlAsync(null);
        expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/dashboard');
    });

    it('should navigate to auth with mode and returnUrl', async () => {
        const mode: AuthMode = 'login';
        const returnUrl = '/products';

        await service.navigateToAuthAsync(mode, returnUrl);
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/'], {
            queryParams: { auth: 'login', returnUrl: '/products' },
        });
    });

    it('should navigate to auth without returnUrl', async () => {
        const mode: AuthMode = 'register';

        await service.navigateToAuthAsync(mode);
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/'], { queryParams: { auth: 'register' } });
    });

    it('should navigate to email verification pending without auto resend', async () => {
        await service.navigateToEmailVerificationPendingAsync();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/verify-pending'], { queryParams: {} });
    });

    it('should navigate to email verification pending with auto resend', async () => {
        await service.navigateToEmailVerificationPendingAsync({ autoResend: true });
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/verify-pending'], { queryParams: { autoResend: 'true' } });
    });

    it('should navigate to product edit with id', async () => {
        await service.navigateToProductEditAsync('abc-123');
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/products/abc-123/edit']);
    });

    it('should navigate to meal add with mealType', async () => {
        await service.navigateToMealAddAsync('breakfast');
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/meals/add'], {
            state: { mealType: 'breakfast' },
            queryParams: { mealType: 'breakfast' },
        });
    });

    it('opens the manual picker directly for an explicit manual meal addition', async () => {
        await service.navigateToMealAddAsync('breakfast', { manual: true });
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/meals/add'], {
            state: { mealType: 'breakfast' },
            queryParams: { mealType: 'breakfast', manual: 1 },
        });
    });

    it('should navigate to profile', async () => {
        await service.navigateToProfileAsync();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/profile']);
    });

    it('should navigate to profile with Google link result', async () => {
        await service.navigateToProfileAsync({ googleLink: 'success' });
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/profile'], {
            queryParams: { googleLink: 'success' },
        });
    });
});

describe('NavigationService localized public entry', () => {
    let context: ReturnType<typeof createNavigationContext>;

    beforeEach(() => {
        context = createNavigationContext();
    });

    it.each<AuthMode>(['login', 'register'])('preserves Russian locale and the return URL for %s entry', async mode => {
        context.currentLanguage.mockReturnValue('ru');
        const returnUrl = '/meals?dateFrom=2026-10-18&dateTo=2026-10-18';

        await context.service.navigateToAuthAsync(mode, returnUrl);

        expect(context.routerSpy.navigate).toHaveBeenCalledWith(['/ru'], { queryParams: { auth: mode, returnUrl } });
    });

    it('returns to the Russian public landing after leaving the application', async () => {
        context.currentLanguage.mockReturnValue('ru');

        await context.service.navigateToLandingAsync();

        expect(context.routerSpy.navigate).toHaveBeenCalledWith(['/ru']);
    });

    it('uses the current language when the preference changes after service creation', async () => {
        await context.service.navigateToAuthAsync('login');
        context.currentLanguage.mockReturnValue('ru');

        await context.service.navigateToAuthAsync('register');

        expect(context.routerSpy.navigate).toHaveBeenLastCalledWith(['/ru'], { queryParams: { auth: 'register' } });
    });

    it('keeps unsupported locale values within the configured public roots', async () => {
        context.currentLanguage.mockReturnValue('https://outside.audit-r5.test');

        await context.service.navigateToAuthAsync('login');

        expect(context.routerSpy.navigate).toHaveBeenCalledWith(['/'], { queryParams: { auth: 'login' } });
    });
});

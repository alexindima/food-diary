import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal, type WritableSignal } from '@angular/core';
import { DeferBlockBehavior, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../testing/translate-testing.module';
import { routes } from './app.routes';
import type { PublicSiteLocale } from './config/public-site-paths';
import { AuthService } from './services/auth.service';
import { NavigationService } from './services/navigation.service';
import { AuthRecoveryStateService } from './shared/auth/auth-recovery-state.service';
import { LocalizationService } from './shared/i18n/localization.service';
import { PrivacyConsentFacade } from './shared/privacy/privacy-consent.facade';

const privateDeepLinks = [
    '/dashboard',
    '/products',
    '/meals',
    '/recipes',
    '/shopping-lists',
    '/goals',
    '/statistics',
    '/weight-history',
    '/waist-history',
    '/cycle-tracking',
    '/meal-plans',
    '/weekly-check-in',
    '/lessons',
    '/gamification',
    '/fasting',
    '/premium',
    '/profile',
    '/dietologist/clients/client-a',
    '/dietologist-invitations/invitation-a',
    '/recommendations',
    '/privacy-settings',
] as const;

describe('Application router access', () => {
    it.each(privateDeepLinks)('preserves %s as the return URL when a guest opens a protected page', async path => {
        const { navigation, localization, privacy } = setupRouting();
        const router = TestBed.inject(Router);
        const target = `${path}?from=bookmark`;

        await expect(router.navigateByUrl(target)).resolves.toBe(false);

        expect(navigation.navigateToAuthAsync).toHaveBeenCalledExactlyOnceWith('login', target);
        expect(localization.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
        expect(privacy.ensureHealthConsentAsync).not.toHaveBeenCalled();
        expect(router.url).toBe('/');
    });

    it('keeps a deep link during session restoration failure without opening another login flow', async () => {
        const { navigation, recovery } = setupRouting();
        recovery.issue.set('restore');
        const target = '/products?search=tea';

        await expect(TestBed.inject(Router).navigateByUrl(target)).resolves.toBe(false);

        expect(recovery.returnUrl).toBe(target);
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
    });

    it('takes an authenticated user without health consent to the actual privacy settings page', async () => {
        const { auth, privacy, navigation } = setupRouting();
        auth.isAuthenticated.set(true);
        const harness = await RouterTestingHarness.create();

        await harness.navigateByUrl('/statistics');

        expect(privacy.ensureHealthConsentAsync).toHaveBeenCalledOnce();
        expect(TestBed.inject(Router).url).toBe('/privacy-settings');
        expect(harness.routeNativeElement?.matches('fd-privacy-settings')).toBe(true);
        expect(harness.routeNativeElement?.textContent).toContain('PRIVACY_CHOICES.HEALTH_REQUIRED');
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
    });
});

const publicDeepLinks = [
    ['/', 'en', 'fd-main', 'LANDING_HERO.HEADLINE'],
    ['/ru', 'ru', 'fd-main', 'LANDING_HERO.HEADLINE'],
    ['/food-diary', 'en', 'fd-seo-landing-page', 'NUTRITION_DIARY_PAGE.TITLE'],
    ['/ru/food-diary', 'ru', 'fd-seo-landing-page', 'NUTRITION_DIARY_PAGE.TITLE'],
    ['/about', 'en', 'fd-public-article-page', 'PUBLIC_PAGES.ABOUT.TITLE'],
    ['/ru/contact', 'ru', 'fd-public-article-page', 'PUBLIC_PAGES.CONTACT.TITLE'],
    ['/privacy-policy', 'en', 'fd-privacy-policy', 'PRIVACY_POLICY.TITLE'],
    ['/ru/privacy-policy', 'ru', 'fd-privacy-policy', 'PRIVACY_POLICY.TITLE'],
    ['/terms-of-service', 'en', 'fd-terms-of-service', 'TERMS_OF_SERVICE.TITLE'],
    ['/ru/terms-of-service', 'ru', 'fd-terms-of-service', 'TERMS_OF_SERVICE.TITLE'],
    ['/ru/no-longer-available', 'ru', 'fd-not-found', 'NOT_FOUND.TITLE'],
] as const;

describe('Application router public pages', () => {
    it.each(publicDeepLinks)('loads %s with its localized public component and heading', async (path, language, selector, heading) => {
        const { localization, navigation } = setupRouting();
        const harness = await RouterTestingHarness.create();

        await harness.navigateByUrl(path);

        expect(harness.routeNativeElement?.matches(selector)).toBe(true);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent.trim()).toBe(heading);
        expect(localization.applyLanguagePreferenceAsync).toHaveBeenCalledExactlyOnceWith(language);
        expect(localization.loadRouteTranslationsAsync).toHaveBeenCalledExactlyOnceWith(path);
        expect(TestBed.inject(TranslateService).getCurrentLang()).toBe(language);
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
    });
});

type RoutingFixture = {
    auth: { isAuthenticated: WritableSignal<boolean> };
    recovery: { issue: WritableSignal<'restore' | 'logout' | null>; returnUrl: string | null };
    navigation: { navigateToAuthAsync: Mock<NavigationService['navigateToAuthAsync']> };
    localization: {
        applyLanguagePreferenceAsync: Mock<(language: PublicSiteLocale) => Promise<void>>;
        loadRouteTranslationsAsync: Mock<LocalizationService['loadRouteTranslationsAsync']>;
        loadApplicationTranslationsAsync: Mock<LocalizationService['loadApplicationTranslationsAsync']>;
    };
    privacy: { ensureHealthConsentAsync: Mock<PrivacyConsentFacade['ensureHealthConsentAsync']> };
};

function setupRouting(): RoutingFixture {
    const auth = {
        isAuthenticated: signal(false),
        isAuthReady: signal(true),
        ensureSessionReadyAsync: vi.fn().mockResolvedValue(undefined),
        mustChangePassword: vi.fn(() => false),
        requiresEmailVerification: vi.fn(() => false),
        isDietologist: vi.fn(() => false),
    };
    const recovery = { issue: signal<'restore' | 'logout' | null>(null), returnUrl: null as string | null };
    const navigation = { navigateToAuthAsync: vi.fn<NavigationService['navigateToAuthAsync']>().mockResolvedValue(undefined) };
    const localization = {
        applyLanguagePreferenceAsync: vi.fn(async (language: PublicSiteLocale): Promise<void> => {
            await firstValueFrom(TestBed.inject(TranslateService).use(language));
        }),
        loadRouteTranslationsAsync: vi.fn().mockResolvedValue(undefined),
        loadApplicationTranslationsAsync: vi.fn().mockResolvedValue(undefined),
    };
    const privacy = {
        state: signal(null),
        busy: signal(false),
        failed: signal(false),
        needsReload: signal(false),
        loadAsync: vi.fn().mockResolvedValue(undefined),
        ensureHealthConsentAsync: vi.fn().mockResolvedValue(false),
    };

    TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        providers: [
            provideRouter(routes),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: auth },
            { provide: NavigationService, useValue: navigation },
            { provide: AuthRecoveryStateService, useValue: recovery },
            { provide: LocalizationService, useValue: localization },
            { provide: PrivacyConsentFacade, useValue: privacy },
        ],
    });

    return { auth, navigation, recovery, localization, privacy };
}

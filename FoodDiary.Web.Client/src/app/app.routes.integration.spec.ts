import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal, type WritableSignal } from '@angular/core';
import { DeferBlockBehavior, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { environment } from '../environments/environment';
import { provideTranslateTesting } from '../testing/translate-testing.module';
import { routes } from './app.routes';
import type { PublicSiteLocale } from './config/public-site-paths';
import { PublicAuthDialogService } from './features/public/lib/public-auth-dialog.service';
import { AuthService } from './services/auth.service';
import { NavigationService } from './services/navigation.service';
import type { DietologistInvitationForCurrentUserHttpResponse } from './shared/api/sdk/generated/model/dietologist-invitation-for-current-user-http-response';
import type { PublicRecipeHttpResponsePagedHttpResponse } from './shared/api/sdk/generated/model/public-recipe-http-response-paged-http-response';
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

    it('loads an expired Dietologist invitation bookmark for the authenticated recipient', async () => {
        const { auth, navigation, localization } = setupRouting();
        auth.isAuthenticated.set(true);
        const http = TestBed.inject(HttpTestingController);
        const invitationId = 'a82ef458-7aec-4945-b32e-faebb9d4809e';
        const bookmark = `/dietologist-invitations/${invitationId}?from=bookmark`;
        const harness = await RouterTestingHarness.create(bookmark);
        const invitation = http.expectOne({
            method: 'GET',
            url: `${environment.apiUrls.dietologist}/invitations/${invitationId}/current-user`,
        });
        invitation.flush({
            invitationId,
            clientUserId: 'fixture-client',
            clientEmail: 'client@example.test',
            clientFirstName: 'Alex',
            clientLastName: null,
            status: 'Expired',
            createdAtUtc: '2026-05-01T10:00:00.000Z',
            expiresAtUtc: '2026-05-08T10:00:00.000Z',
        } satisfies DietologistInvitationForCurrentUserHttpResponse);
        harness.detectChanges();

        const root = renderedRoute(harness);
        expect(root.querySelector('h1')?.textContent.trim()).toBe('DIETOLOGIST_INVITATION.TITLE');
        expect(root.textContent).toContain('DIETOLOGIST_INVITATION.EXPIRED');
        expect(root.textContent).not.toContain('DIETOLOGIST_INVITATION.ACCEPT');
        expect(TestBed.inject(Router).url).toBe(bookmark);
        expect(localization.loadApplicationTranslationsAsync).toHaveBeenCalledOnce();
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
        http.verify();
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

    it.each([
        { language: 'en', diary: '/food-diary', counter: '/calorie-counter', signup: '/?auth=register' },
        { language: 'ru', diary: '/ru/food-diary', counter: '/ru/calorie-counter', signup: '/ru?auth=register' },
    ])('keeps $language locale through deferred related links and signup', async ({ language, diary, counter, signup }) => {
        const { authDialog } = setupRouting(DeferBlockBehavior.Playthrough);
        const router = TestBed.inject(Router);
        const harness = await RouterTestingHarness.create(diary);
        const root = renderedRoute(harness);

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(root.querySelector('fd-seo-landing-content-sections details summary')?.textContent).toContain(
                'NUTRITION_DIARY_PAGE.FAQ.ITEMS.APP_SCOPE.QUESTION',
            );
        });

        const related = await vi.waitFor(() => {
            harness.detectChanges();
            const link = root.querySelector<HTMLAnchorElement>(`fd-seo-landing-footer-sections a[href="${counter}"]`);
            expect(link).toBeInstanceOf(HTMLAnchorElement);
            return link;
        });
        related?.click();

        const register = await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe(counter);
            expect(renderedRoute(harness).querySelector('h1')?.textContent.trim()).toBe('CALORIE_COUNTER_PAGE.TITLE');
            const buttons = renderedRoute(harness).querySelectorAll<HTMLButtonElement>('fd-seo-landing-footer-sections button');
            const action = Array.from(buttons).find(button => button.textContent.trim() === 'CALORIE_COUNTER_PAGE.CTA.PRIMARY');
            expect(action).toBeInstanceOf(HTMLButtonElement);
            return action;
        });
        register?.click();

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe(signup);
            expect(authDialog.openAsync).toHaveBeenCalledOnce();
        });
        expect(TestBed.inject(TranslateService).getCurrentLang()).toBe(language);
        expect(authDialog.openAsync.mock.calls[0][0].mode).toBe('register');
    });
});

describe('Application router guest recipe catalog', () => {
    it('renders a missing recipe bookmark and returns to the filtered catalog through Back', async () => {
        const { navigation, privacy } = setupRouting();
        const router = TestBed.inject(Router);
        const http = TestBed.inject(HttpTestingController);
        const harness = await RouterTestingHarness.create();
        const catalogApi = `${environment.apiUrls.recipes}/public`;
        const filters = 'search=tomato&maxTotalTime=30';
        const catalog = `/explore?${filters}`;
        const missingId = '84fd02e3-9de4-401e-bb9c-781b98578d4e';
        const recipe = {
            id: '8181a5a2-8ded-4960-92be-0d51cb4f37df',
            name: 'Roasted tomato soup',
            images: [],
            servings: 2,
            missingIngredientCount: 0,
            steps: [],
        };
        const results = {
            data: [recipe],
            page: 1,
            limit: 20,
            totalItems: 1,
            totalPages: 1,
        } satisfies PublicRecipeHttpResponsePagedHttpResponse;
        const respondWithCatalogAsync = async (): Promise<void> => {
            const catalogRequest = await vi.waitFor(() =>
                http.expectOne(
                    request =>
                        request.method === 'GET' &&
                        request.url === catalogApi &&
                        request.params.get('search') === 'tomato' &&
                        request.params.get('maxTotalTime') === '30',
                ),
            );
            catalogRequest.flush(results);
        };

        await harness.navigateByUrl(catalog);
        await respondWithCatalogAsync();
        harness.detectChanges();
        expect(renderedRoute(harness).querySelector('h1')?.textContent.trim()).toBe('PUBLIC_RECIPES.CATALOG');
        expect(renderedRoute(harness).querySelector('fd-public-recipe-card')?.textContent).toContain(recipe.name);
        expect(router.routerState.snapshot.root.firstChild?.data['seo']).toMatchObject({
            titleKey: 'PUBLIC_RECIPES.CATALOG',
            descriptionKey: 'PUBLIC_RECIPES.SUBTITLE',
        });

        const bookmark = `/explore/${missingId}?${filters}`;
        const pending = harness.navigateByUrl(bookmark);
        const missing = await vi.waitFor(() => http.expectOne({ method: 'GET', url: `${catalogApi}/${missingId}` }));
        missing.flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
        await pending;
        expect(router.url).toBe(bookmark);
        const unavailable = renderedRoute(harness);
        expect(unavailable.querySelector('h1')?.textContent.trim()).toBe('PUBLIC_RECIPES.NOT_FOUND');
        expect(router.routerState.snapshot.root.firstChild?.firstChild?.data['seo']).toMatchObject({
            error: 'not-found',
            noIndex: true,
        });

        const back = unavailable.querySelector<HTMLButtonElement>('button[aria-label="PUBLIC_RECIPES.CATALOG"]');
        expect(back).toBeInstanceOf(HTMLButtonElement);
        back?.click();
        await respondWithCatalogAsync();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(router.url).toBe(catalog);
        expect(renderedRoute(harness).querySelector('fd-public-recipe-card')?.textContent).toContain(recipe.name);
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
        expect(privacy.ensureHealthConsentAsync).not.toHaveBeenCalled();
        http.verify();
    });
});

function renderedRoute(harness: RouterTestingHarness): HTMLElement {
    const root = harness.routeNativeElement;
    if (!(root instanceof HTMLElement)) {
        throw new Error('Expected a rendered application route');
    }
    return root;
}

type RoutingFixture = {
    auth: { isAuthenticated: WritableSignal<boolean> };
    authDialog: { openAsync: Mock<PublicAuthDialogService['openAsync']> };
    recovery: { issue: WritableSignal<'restore' | 'logout' | null>; returnUrl: string | null };
    navigation: { navigateToAuthAsync: Mock<NavigationService['navigateToAuthAsync']> };
    localization: {
        applyLanguagePreferenceAsync: Mock<(language: PublicSiteLocale) => Promise<void>>;
        loadRouteTranslationsAsync: Mock<LocalizationService['loadRouteTranslationsAsync']>;
        loadApplicationTranslationsAsync: Mock<LocalizationService['loadApplicationTranslationsAsync']>;
    };
    privacy: { ensureHealthConsentAsync: Mock<PrivacyConsentFacade['ensureHealthConsentAsync']> };
};

function setupRouting(deferBlockBehavior = DeferBlockBehavior.Manual): RoutingFixture {
    const auth = {
        isAuthenticated: signal(false),
        isAuthReady: signal(true),
        getUserId: vi.fn(() => null),
        ensureSessionReadyAsync: vi.fn().mockResolvedValue(undefined),
        mustChangePassword: vi.fn(() => false),
        requiresEmailVerification: vi.fn(() => false),
        isDietologist: vi.fn(() => false),
    };
    const recovery = { issue: signal<'restore' | 'logout' | null>(null), returnUrl: null as string | null };
    const navigation = { navigateToAuthAsync: vi.fn<NavigationService['navigateToAuthAsync']>().mockResolvedValue(undefined) };
    const authDialog = { openAsync: vi.fn<PublicAuthDialogService['openAsync']>().mockResolvedValue(null) };
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
        deferBlockBehavior,
        providers: [
            provideRouter(routes),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: auth },
            { provide: PublicAuthDialogService, useValue: authDialog },
            { provide: NavigationService, useValue: navigation },
            { provide: AuthRecoveryStateService, useValue: recovery },
            { provide: LocalizationService, useValue: localization },
            { provide: PrivacyConsentFacade, useValue: privacy },
        ],
    });

    return { auth, authDialog, navigation, recovery, localization, privacy };
}

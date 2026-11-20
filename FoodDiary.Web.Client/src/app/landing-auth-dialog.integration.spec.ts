import { type HttpInterceptorFn, HttpResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { DeferBlockBehavior, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { stubScrollIntoView } from '../testing/dom-testing';
import { provideTranslateTesting } from '../testing/translate-testing.module';
import { TelegramAuthService } from './features/auth/api/telegram-auth.service';
import { AuthComponent } from './features/auth/components/auth/auth';
import { AuthGoogleManager } from './features/auth/components/auth/auth-lib/auth-google.manager';
import { MainComponent } from './features/public/pages/landing/main';
import { AuthService } from './services/auth.service';
import { publicSiteLocaleGuard } from './shared/i18n/public-site-locale.guard';

const publicTranslationAssets: HttpInterceptorFn = (request, next) =>
    request.method === 'GET' && request.url.startsWith('./assets/i18n/') ? of(new HttpResponse({ body: {} })) : next(request);

describe('Public landing account dialog ownership', () => {
    it('leaves the old sign-in draft behind when navigation mounts another locale', async () => {
        const harness = await renderLandingAsync();
        const router = TestBed.inject(Router);
        click(landing(harness), '.hero__cta--secondary button');
        const login = await openedDialogAsync(harness, '#auth-login-email');
        const email = login.querySelector<HTMLInputElement>('#auth-login-email');
        if (email === null) {
            throw new Error('Expected the sign-in email field');
        }
        email.value = 'old-dialog@example.test';
        email.dispatchEvent(new Event('input', { bubbles: true }));
        expect(router.url).toBe('/?auth=login');

        await harness.navigateByUrl('/ru', MainComponent);
        harness.detectChanges();
        expect(router.url).toBe('/ru');
        expect(TestBed.inject(TranslateService).getCurrentLang()).toBe('ru');
        expect(login.isConnected).toBe(false);
        expect(document.body.querySelector('fd-auth-dialog')).toBeNull();

        click(landing(harness), '.hero__cta--primary button');
        const registration = await openedDialogAsync(harness, '#auth-register-email');
        expect(registration).not.toBe(login);
        expect(registration.querySelector<HTMLInputElement>('#auth-register-email')?.value).toBe('');
        expect(router.url).toBe('/ru?auth=register');
        click(registration, 'button[aria-label="COMMON.CLOSE"]');
        await vi.waitFor(() => {
            expect(router.url).toBe('/ru');
            expect(document.body.querySelector('fd-auth-dialog')).toBeNull();
        });
        TestBed.inject(HttpTestingController).verify();
    });
});

async function renderLandingAsync(): Promise<RouterTestingHarness> {
    stubScrollIntoView();
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    // The Router test owns its location. Keep the overlay's separate native history entry out of jsdom.
    const pushHistory = vi.spyOn(window.history, 'pushState').mockImplementation(() => {});
    onTestFinished(() => {
        pushHistory.mockRestore();
        vi.unstubAllGlobals();
    });
    TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        providers: [
            provideRouter([
                { path: '', pathMatch: 'full', component: MainComponent, canActivate: [publicSiteLocaleGuard] },
                { path: 'ru', component: MainComponent, canActivate: [publicSiteLocaleGuard] },
            ]),
            provideHttpClient(withInterceptors([publicTranslationAssets])),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isAuthenticated: signal(false), isAuthReady: signal(true) } },
            {
                provide: TelegramAuthService,
                useValue: {
                    configuration: vi
                        .fn<TelegramAuthService['configuration']>()
                        .mockReturnValue(of({ loginEnabled: false, registrationEnabled: false, oidcEnabled: false })),
                },
            },
        ],
    }).overrideComponent(AuthComponent, {
        remove: { providers: [AuthGoogleManager] },
        add: {
            providers: [
                {
                    provide: AuthGoogleManager,
                    useValue: { ready: signal(false), initializeAsync: vi.fn().mockResolvedValue(undefined), renderButton: vi.fn() },
                },
            ],
        },
    });
    return RouterTestingHarness.create('/');
}

function landing(harness: RouterTestingHarness): HTMLElement {
    const host = harness.routeNativeElement;
    if (!(host instanceof HTMLElement)) {
        throw new Error('Expected the rendered public landing page');
    }
    return host;
}

async function openedDialogAsync(harness: RouterTestingHarness, selector: string): Promise<HTMLElement> {
    return vi.waitFor(() => {
        harness.detectChanges();
        const dialog = document.body.querySelector<HTMLElement>('fd-auth-dialog');
        if (dialog === null) {
            throw new Error(`Expected the account dialog with ${selector}`);
        }
        expect(dialog.querySelector(selector)).not.toBeNull();
        return dialog;
    });
}

function click(root: HTMLElement, selector: string): void {
    const button = root.querySelector<HTMLButtonElement>(selector);
    if (button === null) {
        throw new Error(`Expected a rendered button for ${selector}`);
    }
    button.click();
}

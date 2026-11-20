import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, firstValueFrom, type Observable, of, Subject } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import type { AuthResponse } from '../../shared/auth/auth.data';
import { TelegramBackupEmailFlowService } from '../../shared/auth/telegram-backup-email-flow.service';
import { telegramLoginTicket } from '../../shared/auth/telegram-oidc-values';
import { TelegramUnlinkFlowService } from '../../shared/auth/telegram-unlink-flow.service';
import { TelegramWebAppService } from '../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../shared/i18n/localization.service';
import { UserFacade } from '../../shared/lib/user.facade';
import { entityId } from '../../shared/models/semantics/entity-id';
import type { User } from '../../shared/models/user.data';
import { BrowserStorageService } from '../../shared/platform/browser-storage.service';
import { TelegramAuthService } from './api/telegram-auth.service';
import { authRoutes } from './auth.routes';
import type { TelegramConfiguration, TelegramIntent } from './models/telegram-auth.data';
import { TelegramAuthComponent } from './pages/telegram-auth/telegram-auth';

const PRIOR_URL = '/explore';
const AUTH_RETURN_URL = '/profile';
const signInIntent: TelegramIntent = {
    ticket: telegramLoginTicket('abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG'),
    nextAction: 'login',
    expiresAtUtc: '2099-01-01T00:00:00Z',
};
const sessionResponse: AuthResponse = {
    sessionId: 'telegram-test-session',
    user: {
        id: entityId<'user'>('telegram-test-account'),
        email: null,
        hasPassword: false,
        isActive: true,
        isEmailConfirmed: false,
        pushNotificationsEnabled: false,
        fastingPushNotificationsEnabled: false,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: 12,
        fastingCheckInFollowUpReminderHours: 20,
    },
};

@Component({
    selector: 'fd-telegram-previous-destination',
    template: '<h1>Explore recipes</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class RecipesDestination {}

@Component({
    selector: 'fd-telegram-sign-in-destination',
    template: '<h1>Account profile</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class AuthReturnDestination {}

async function waitForSignInButtonAsync(harness: RouterTestingHarness): Promise<HTMLButtonElement> {
    return vi.waitFor(() => {
        harness.detectChanges();
        const buttons = Array.from(harness.routeNativeElement?.querySelectorAll('button') ?? []);
        const button = buttons.find(control => control.textContent.includes('AUTH.TELEGRAM.SIGN_IN'));
        if (button === undefined) {
            throw new Error(`Expected Telegram sign-in action, got ${harness.routeNativeElement?.textContent ?? ''}`);
        }
        return button;
    });
}

it('keeps the Back destination when a pending Telegram sign-in accepts a session after leaving', async () => {
    const pending = new Subject<AuthResponse>();
    const complete = vi.fn<TelegramAuthService['complete']>().mockReturnValue(pending);
    const isAuthenticated = signal(false);
    const acceptExternalAuthentication = vi.fn<AuthService['acceptExternalAuthentication']>(() => {
        isAuthenticated.set(true);
    });
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                ...authRoutes,
                { path: 'explore', component: RecipesDestination },
                { path: 'profile', component: AuthReturnDestination },
            ]),
            provideLocationMocks(),
            provideTranslateTesting(),
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated,
                    acceptExternalAuthentication,
                    requiresEmailVerification: signal(false),
                    mustChangePassword: signal(false),
                },
            },
            {
                provide: TelegramAuthService,
                useValue: {
                    configuration: (): Observable<TelegramConfiguration> =>
                        of({ loginEnabled: true, registrationEnabled: true, oidcEnabled: true }),
                    complete,
                },
            },
            { provide: BrowserStorageService, useValue: { getJson: (): TelegramIntent => signInIntent, removeItem: vi.fn() } },
            { provide: TelegramWebAppService, useValue: { claimAutomaticSignInAsync: vi.fn().mockResolvedValue(false) } },
            { provide: TelegramUnlinkFlowService, useValue: { handleCallbackAsync: vi.fn().mockResolvedValue('unhandled') } },
            { provide: TelegramBackupEmailFlowService, useValue: { handleCallbackAsync: vi.fn().mockResolvedValue(false) } },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            {
                provide: UserFacade,
                useValue: { user: signal<User | null>(null), getInfo: (): Observable<User> => of(sessionResponse.user) },
            },
        ],
    });
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const returnNavigation = vi.spyOn(TestBed.inject(NavigationService), 'navigateToReturnUrlAsync');
    const harness = await RouterTestingHarness.create(PRIOR_URL);
    const query = new URLSearchParams({ returnUrl: AUTH_RETURN_URL });
    await harness.navigateByUrl(`/auth/telegram?${query.toString()}`, TelegramAuthComponent);
    const signIn = await waitForSignInButtonAsync(harness);
    signIn.click();
    await vi.waitFor(() => {
        expect(complete).toHaveBeenCalledExactlyOnceWith(signInIntent.ticket, 'login', 'en', expect.any(String));
    });

    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === PRIOR_URL)),
    );
    TestBed.inject(Location).back();
    await returned;
    expect(router.url).toBe(PRIOR_URL);

    pending.next(sessionResponse);
    pending.complete();
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(acceptExternalAuthentication).toHaveBeenCalledExactlyOnceWith(sessionResponse);
    expect(isAuthenticated()).toBe(true);
    expect(returnNavigation).not.toHaveBeenCalled();
    expect(router.url).toBe(PRIOR_URL);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Explore recipes');
});

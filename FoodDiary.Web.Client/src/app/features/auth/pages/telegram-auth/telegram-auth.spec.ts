import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { TelegramBackupEmailFlowService } from '../../../../shared/auth/telegram-backup-email-flow.service';
import { telegramLoginTicket } from '../../../../shared/auth/telegram-oidc-values';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { User } from '../../../../shared/models/user.data';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { AuthComponent } from '../../components/auth/auth';
import { TelegramAuthFacade } from '../../lib/telegram-auth.facade';
import type { TelegramConfiguration, TelegramIntent } from '../../models/telegram-auth.data';
import { TelegramAuthComponent } from './telegram-auth';

const CONFIGURATION: TelegramConfiguration = { loginEnabled: true, registrationEnabled: true, oidcEnabled: true };
const INTENT: TelegramIntent = {
    ticket: telegramLoginTicket('abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG'),
    nextAction: 'onboarding',
    expiresAtUtc: '2099-01-01T00:00:00Z',
};
const USER: User = {
    id: entityId<'user'>('user-1'),
    email: 'user@example.com',
    firstName: 'Alex',
    lastName: 'Smith',
    hasPassword: true,
    isActive: true,
    isEmailConfirmed: true,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 1,
    fastingCheckInFollowUpReminderHours: 1,
};

@Component({
    selector: 'fd-auth',
    template: '<p>Existing account sign-in</p>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ExistingAuthStubComponent {
    public readonly initialReturnUrl = input('');
    public readonly useRouting = input(true);
    public readonly hideTelegram = input(false);
}

class TelegramAuthDoubles {
    public readonly facade = {
        configuration: signal<TelegramConfiguration | null>(CONFIGURATION),
        intent: signal<TelegramIntent | null>(null),
        busy: signal(false),
        errorKey: signal<string | null>(null),
        loadConfigurationAsync: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
        restoreIntent: vi.fn(),
        clearIntent: vi.fn(),
        exchangeAsync: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
        beginAsync: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
        completeAsync: vi.fn<() => Promise<boolean>>().mockResolvedValue(true),
    };
    public readonly auth = {
        isAuthenticated: signal(false),
        requiresEmailVerification: signal(false),
        mustChangePassword: signal(false),
    };
    public readonly users = { user: signal<User | null>(null), getInfo: vi.fn().mockReturnValue(of(USER)) };
    public readonly navigation = {
        navigateToAuthAsync: vi.fn().mockResolvedValue(undefined),
        navigateToProfileAsync: vi.fn().mockResolvedValue(undefined),
        navigateToHomeAsync: vi.fn().mockResolvedValue(undefined),
        navigateToEmailVerificationPendingAsync: vi.fn().mockResolvedValue(undefined),
        navigateToRequiredPasswordChangeAsync: vi.fn().mockResolvedValue(undefined),
    };
    public readonly browser = { replaceCurrentUrl: vi.fn() };
    public readonly backupEmail = { handleCallbackAsync: vi.fn<() => Promise<boolean>>().mockResolvedValue(false) };
}

type TelegramAuthHarness = TelegramAuthDoubles & {
    fixture: ComponentFixture<TelegramAuthComponent>;
    root: HTMLElement;
};

function createHarness(query: Record<string, string | undefined> = {}, doubles = new TelegramAuthDoubles()): TelegramAuthHarness {
    doubles.facade.clearIntent.mockImplementation(() => {
        doubles.facade.intent.set(null);
    });
    TestBed.configureTestingModule({
        imports: [TelegramAuthComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AuthService, useValue: doubles.auth },
            { provide: UserFacade, useValue: doubles.users },
            { provide: NavigationService, useValue: doubles.navigation },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'ru' } },
            { provide: BrowserWindowService, useValue: doubles.browser },
            { provide: TelegramBackupEmailFlowService, useValue: doubles.backupEmail },
            { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(query) } } },
        ],
    }).overrideComponent(TelegramAuthComponent, {
        remove: { imports: [AuthComponent], providers: [TelegramAuthFacade] },
        add: { imports: [ExistingAuthStubComponent], providers: [{ provide: TelegramAuthFacade, useValue: doubles.facade }] },
    });
    const fixture = TestBed.createComponent(TelegramAuthComponent);
    fixture.detectChanges();
    return Object.assign(doubles, { fixture, root: fixture.nativeElement as HTMLElement });
}

async function settleAsync(harness: TelegramAuthHarness): Promise<void> {
    await waitForAsyncTasksAsync();
    harness.fixture.detectChanges();
    await harness.fixture.whenStable();
    harness.fixture.detectChanges();
}

function button(harness: TelegramAuthHarness, key: string): HTMLButtonElement {
    const result = [...harness.root.querySelectorAll('button')].find(item => item.textContent.includes(key));
    if (result === undefined) {
        throw new Error(`Missing Telegram action: ${key}`);
    }
    return result;
}

async function clickAsync(harness: TelegramAuthHarness, key: string): Promise<void> {
    button(harness, key).click();
    await settleAsync(harness);
}

async function createIntentHarnessAsync(nextAction: TelegramIntent['nextAction'] = 'onboarding'): Promise<TelegramAuthHarness> {
    const doubles = new TelegramAuthDoubles();
    doubles.facade.intent.set({ ...INTENT, nextAction });
    const harness = createHarness({}, doubles);
    await settleAsync(harness);
    return harness;
}

describe('Telegram authentication initialization', () => {
    it('keeps loading visible until configuration is ready after restoring the intent', async () => {
        const doubles = new TelegramAuthDoubles();
        let finishLoading: (() => void) | undefined;
        doubles.facade.loadConfigurationAsync.mockReturnValue(
            new Promise<void>(resolve => {
                finishLoading = resolve;
            }),
        );
        const harness = createHarness({}, doubles);
        expect(harness.root.querySelector('[role="status"]')?.textContent).toContain('COMMON.LOADING');
        expect(doubles.facade.restoreIntent).toHaveBeenCalledOnce();
        expect(doubles.backupEmail.handleCallbackAsync).not.toHaveBeenCalled();
        expect(doubles.browser.replaceCurrentUrl).not.toHaveBeenCalled();
        expect(harness.root.textContent).not.toContain('AUTH.TELEGRAM.CONTINUE');
        finishLoading?.();
        await settleAsync(harness);
        expect(harness.root.querySelector('[role="status"]')).toBeNull();
        expect(button(harness, 'AUTH.TELEGRAM.CONTINUE').disabled).toBe(false);
    });

    it.each([null, { ...CONFIGURATION, loginEnabled: false }])(
        'offers other sign-in methods for unavailable configuration %s',
        async config => {
            const doubles = new TelegramAuthDoubles();
            doubles.facade.configuration.set(config);
            const harness = createHarness({}, doubles);
            await settleAsync(harness);
            expect(harness.root.textContent).toContain('AUTH.TELEGRAM.UNAVAILABLE');
            await clickAsync(harness, 'AUTH.TELEGRAM.OTHER_METHODS');
            expect(doubles.navigation.navigateToAuthAsync).toHaveBeenCalledWith('login');
            expect(doubles.facade.beginAsync).not.toHaveBeenCalled();
        },
    );

    it.each([false, true])('starts the Telegram flow with the current authenticated state %s', async authenticated => {
        const doubles = new TelegramAuthDoubles();
        doubles.auth.isAuthenticated.set(authenticated);
        const harness = createHarness({}, doubles);
        await settleAsync(harness);
        await clickAsync(harness, 'AUTH.TELEGRAM.CONTINUE');
        expect(doubles.facade.beginAsync).toHaveBeenCalledWith(authenticated);
        expect(doubles.users.getInfo).toHaveBeenCalledTimes(authenticated ? 1 : 0);
    });

    it('replaces actions with loading feedback while the facade is busy', async () => {
        const harness = await createIntentHarnessAsync('login');
        harness.facade.busy.set(true);
        harness.fixture.detectChanges();
        expect(harness.root.querySelector('[role="status"]')?.textContent).toContain('COMMON.LOADING');
        expect(harness.root.textContent).not.toContain('AUTH.TELEGRAM.SIGN_IN');
        expect(harness.facade.completeAsync).not.toHaveBeenCalled();
    });
});

describe('Telegram OAuth callbacks', () => {
    it('cleans callback parameters before exchanging the code and loads configuration after exchange', async () => {
        const harness = createHarness({ code: 'oauth-code', state: 'oauth-state' });
        await settleAsync(harness);
        expect(harness.browser.replaceCurrentUrl).toHaveBeenCalledExactlyOnceWith('/auth/telegram');
        expect(harness.backupEmail.handleCallbackAsync).toHaveBeenCalledWith('oauth-code', 'oauth-state', false);
        expect(harness.facade.exchangeAsync).toHaveBeenCalledWith('oauth-code', 'oauth-state');
        expect(harness.browser.replaceCurrentUrl.mock.invocationCallOrder[0]).toBeLessThan(
            harness.backupEmail.handleCallbackAsync.mock.invocationCallOrder[0],
        );
        expect(harness.facade.exchangeAsync.mock.invocationCallOrder[0]).toBeLessThan(
            harness.facade.loadConfigurationAsync.mock.invocationCallOrder[0],
        );
        expect(harness.facade.restoreIntent).not.toHaveBeenCalled();
        expect(harness.root.querySelector('[role="status"]')).toBeNull();
    });

    it.each([
        { query: { code: 'oauth-code' }, code: 'oauth-code', state: null, denied: false },
        { query: { state: 'oauth-state' }, code: null, state: 'oauth-state', denied: false },
        {
            query: { code: 'oauth-code', state: 'oauth-state', error: 'access_denied' },
            code: 'oauth-code',
            state: 'oauth-state',
            denied: true,
        },
        { query: { error: '' }, code: null, state: null, denied: true },
    ])('renders cancellation and cleans the URL for an incomplete or denied callback: $query', async ({ query, code, state, denied }) => {
        const harness = createHarness(query);
        await settleAsync(harness);
        expect(harness.backupEmail.handleCallbackAsync).toHaveBeenCalledWith(code, state, denied);
        expect(harness.browser.replaceCurrentUrl).toHaveBeenCalledWith('/auth/telegram');
        expect(harness.root.querySelector('[role="alert"]')?.textContent).toContain('AUTH.TELEGRAM.CANCELLED');
        expect(harness.facade.exchangeAsync).not.toHaveBeenCalled();
        expect(harness.facade.restoreIntent).not.toHaveBeenCalled();
        expect(harness.facade.loadConfigurationAsync).toHaveBeenCalledOnce();
        expect(button(harness, 'AUTH.TELEGRAM.CONTINUE').disabled).toBe(false);
    });

    it.each([false, true])('returns a handled backup-email callback to the profile, denied=%s', async denied => {
        const doubles = new TelegramAuthDoubles();
        doubles.backupEmail.handleCallbackAsync.mockResolvedValue(true);
        const query = denied ? { error: 'access_denied' } : { code: 'backup-code', state: 'backup-state' };
        const harness = createHarness(query, doubles);
        await settleAsync(harness);
        expect(doubles.browser.replaceCurrentUrl).toHaveBeenCalledWith('/auth/telegram');
        expect(doubles.backupEmail.handleCallbackAsync).toHaveBeenCalledWith(
            denied ? null : 'backup-code',
            denied ? null : 'backup-state',
            denied,
        );
        expect(doubles.navigation.navigateToProfileAsync).toHaveBeenCalledOnce();
        expect(doubles.facade.exchangeAsync).not.toHaveBeenCalled();
        expect(doubles.facade.restoreIntent).not.toHaveBeenCalled();
        expect(doubles.facade.loadConfigurationAsync).not.toHaveBeenCalled();
        expect(doubles.navigation.navigateToHomeAsync).not.toHaveBeenCalled();
    });
});

describe('Telegram action completion', () => {
    it('submits the explicit login action with the current language and selected timezone', async () => {
        const harness = await createIntentHarnessAsync('login');
        harness.fixture.componentInstance['registrationModel'].set({ timeZoneId: 'Asia/Tbilisi' });
        await clickAsync(harness, 'AUTH.TELEGRAM.SIGN_IN');
        expect(harness.facade.completeAsync).toHaveBeenCalledWith('login', 'ru', 'Asia/Tbilisi');
        expect(harness.navigation.navigateToHomeAsync).toHaveBeenCalledOnce();
    });

    it('stays on the page when completion fails and renders the facade error', async () => {
        const harness = await createIntentHarnessAsync('login');
        harness.facade.completeAsync.mockImplementation(async (): Promise<boolean> => {
            harness.facade.errorKey.set('AUTH.TELEGRAM.EXPIRED');
            await waitForAsyncTasksAsync();
            return false;
        });
        await clickAsync(harness, 'AUTH.TELEGRAM.SIGN_IN');
        expect(harness.root.querySelector('[role="alert"]')?.textContent).toContain('AUTH.TELEGRAM.EXPIRED');
        expect(harness.navigation.navigateToHomeAsync).not.toHaveBeenCalled();
        expect(harness.navigation.navigateToEmailVerificationPendingAsync).not.toHaveBeenCalled();
        expect(harness.navigation.navigateToRequiredPasswordChangeAsync).not.toHaveBeenCalled();
    });

    it.each([
        { verifyEmail: true, changePassword: true, destination: 'navigateToEmailVerificationPendingAsync' },
        { verifyEmail: false, changePassword: true, destination: 'navigateToRequiredPasswordChangeAsync' },
        { verifyEmail: false, changePassword: false, destination: 'navigateToHomeAsync' },
    ] as const)(
        'redirects completed login using session restrictions: $destination',
        async ({ verifyEmail, changePassword, destination }) => {
            const harness = await createIntentHarnessAsync('login');
            harness.facade.completeAsync.mockImplementation(async (): Promise<boolean> => {
                await waitForAsyncTasksAsync();
                harness.auth.requiresEmailVerification.set(verifyEmail);
                harness.auth.mustChangePassword.set(changePassword);
                return true;
            });
            await clickAsync(harness, 'AUTH.TELEGRAM.SIGN_IN');
            expect(harness.navigation[destination]).toHaveBeenCalledOnce();
            expect(harness.navigation.navigateToEmailVerificationPendingAsync).toHaveBeenCalledTimes(verifyEmail ? 1 : 0);
            expect(harness.navigation.navigateToRequiredPasswordChangeAsync).toHaveBeenCalledTimes(!verifyEmail && changePassword ? 1 : 0);
            expect(harness.navigation.navigateToHomeAsync).toHaveBeenCalledTimes(!verifyEmail && !changePassword ? 1 : 0);
        },
    );

    it('waits for the signed-in user before enabling explicit account linking', async () => {
        const doubles = new TelegramAuthDoubles();
        doubles.auth.isAuthenticated.set(true);
        doubles.facade.intent.set(INTENT);
        const harness = createHarness({}, doubles);
        await settleAsync(harness);
        expect(button(harness, 'AUTH.TELEGRAM.LINK').disabled).toBe(true);
        button(harness, 'AUTH.TELEGRAM.LINK').click();
        expect(doubles.facade.completeAsync).not.toHaveBeenCalled();
        doubles.users.user.set(USER);
        await settleAsync(harness);
        expect(harness.root.querySelector('strong')?.textContent).toBe('Alex Smith');
        expect(button(harness, 'AUTH.TELEGRAM.LINK').disabled).toBe(false);
        await clickAsync(harness, 'AUTH.TELEGRAM.LINK');
        expect(doubles.facade.completeAsync).toHaveBeenCalledWith('link', 'ru', expect.any(String));
        expect(doubles.navigation.navigateToHomeAsync).toHaveBeenCalledOnce();
    });
});

describe('Telegram registration', () => {
    it('uses the selected timezone for explicit registration', async () => {
        const harness = await createIntentHarnessAsync();
        const select = harness.fixture.debugElement.query(By.directive(FdUiSelectComponent))
            .componentInstance as FdUiSelectComponent<string>;
        select.value.set('Asia/Tbilisi');
        await settleAsync(harness);
        expect(button(harness, 'AUTH.TELEGRAM.CREATE').disabled).toBe(false);
        await clickAsync(harness, 'AUTH.TELEGRAM.CREATE');
        expect(harness.facade.completeAsync).toHaveBeenCalledWith('register', 'ru', 'Asia/Tbilisi');
    });

    it('disables registration for a missing timezone and blocks a programmatic invalid submission', async () => {
        const harness = await createIntentHarnessAsync();
        harness.fixture.componentInstance['registrationForm'].timeZoneId().value.set('');
        await settleAsync(harness);
        expect(button(harness, 'AUTH.TELEGRAM.CREATE').disabled).toBe(true);
        button(harness, 'AUTH.TELEGRAM.CREATE').click();
        harness.fixture.componentInstance['complete']('register');
        await settleAsync(harness);
        expect(harness.fixture.componentInstance['registrationForm'].timeZoneId().touched()).toBe(true);
        expect(harness.facade.completeAsync).not.toHaveBeenCalled();
        expect(harness.navigation.navigateToHomeAsync).not.toHaveBeenCalled();
    });

    it('filters timezone choices from the rendered search while keeping the selected timezone available', async () => {
        const harness = await createIntentHarnessAsync();
        const select = harness.fixture.debugElement.query(By.directive(FdUiSelectComponent))
            .componentInstance as FdUiSelectComponent<string>;
        select.value.set('UTC');
        await settleAsync(harness);
        const search = harness.root.querySelector<HTMLInputElement>('fd-ui-input input');
        expect(search).not.toBeNull();
        if (search === null) {
            throw new Error('Timezone search is missing.');
        }
        search.value = '  tbilisi  ';
        search.dispatchEvent(new Event('input', { bubbles: true }));
        await settleAsync(harness);
        expect(select.options().map(option => option.value)).toEqual(['Asia/Tbilisi', 'UTC']);
        expect(button(harness, 'AUTH.TELEGRAM.CREATE').disabled).toBe(false);
    });

    it('keeps existing-account sign-in available when registration is disabled', async () => {
        const harness = await createIntentHarnessAsync();
        harness.facade.configuration.set({ ...CONFIGURATION, registrationEnabled: false });
        await settleAsync(harness);
        expect(harness.root.querySelector('fd-ui-select')).toBeNull();
        expect(harness.root.textContent).not.toContain('AUTH.TELEGRAM.CREATE');
        await clickAsync(harness, 'AUTH.TELEGRAM.EXISTING');
        expect(harness.root.querySelector('fd-auth')).not.toBeNull();
        expect(harness.facade.completeAsync).not.toHaveBeenCalled();
    });
});

describe('Telegram existing-account choices', () => {
    it('embeds sign-in with a Telegram return URL and allows returning to the preserved choice', async () => {
        const harness = await createIntentHarnessAsync('link');
        expect(harness.root.textContent).not.toContain('AUTH.TELEGRAM.CREATE');
        await clickAsync(harness, 'AUTH.TELEGRAM.EXISTING');
        const signIn = harness.fixture.debugElement.query(By.directive(ExistingAuthStubComponent))
            .componentInstance as ExistingAuthStubComponent;
        expect(signIn.initialReturnUrl()).toBe('/auth/telegram');
        expect(signIn.useRouting()).toBe(false);
        expect(signIn.hideTelegram()).toBe(true);
        await clickAsync(harness, 'AUTH.TELEGRAM.BACK_TO_CHOICE');
        expect(harness.root.querySelector('fd-auth')).toBeNull();
        expect(button(harness, 'AUTH.TELEGRAM.EXISTING')).toBeDefined();
        expect(harness.facade.clearIntent).not.toHaveBeenCalled();
        expect(harness.facade.completeAsync).not.toHaveBeenCalled();
    });

    it('returns to linking after the embedded sign-in authenticates and loads the user', async () => {
        const harness = await createIntentHarnessAsync('link');
        await clickAsync(harness, 'AUTH.TELEGRAM.EXISTING');
        harness.users.user.set(USER);
        harness.auth.isAuthenticated.set(true);
        await settleAsync(harness);
        expect(harness.root.querySelector('fd-auth')).toBeNull();
        expect(harness.users.getInfo).toHaveBeenCalledOnce();
        expect(button(harness, 'AUTH.TELEGRAM.LINK').disabled).toBe(false);
        expect(harness.facade.completeAsync).not.toHaveBeenCalled();
    });

    it('clears the intent and stale error when starting over', async () => {
        const harness = await createIntentHarnessAsync();
        harness.facade.errorKey.set('AUTH.TELEGRAM.EXPIRED');
        await settleAsync(harness);
        await clickAsync(harness, 'AUTH.TELEGRAM.START_OVER');
        expect(harness.facade.clearIntent).toHaveBeenCalledOnce();
        expect(harness.root.querySelector('[role="alert"]')).toBeNull();
        expect(button(harness, 'AUTH.TELEGRAM.CONTINUE')).toBeDefined();
        expect(harness.root.textContent).not.toContain('AUTH.TELEGRAM.CREATE');
        expect(harness.facade.beginAsync).not.toHaveBeenCalled();
        expect(harness.facade.completeAsync).not.toHaveBeenCalled();
    });
});

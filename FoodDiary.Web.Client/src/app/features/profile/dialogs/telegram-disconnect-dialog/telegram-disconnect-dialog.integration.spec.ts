import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, of, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { stubScrollIntoView } from '../../../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { GoogleIdentityService } from '../../../../shared/auth/google-identity.service';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { User } from '../../../../shared/models/user.data';
import { TelegramDisconnectDialogComponent, type TelegramDisconnectDialogData } from './telegram-disconnect-dialog';

const OWNER = entityId<'user'>('telegram-disconnect-fixture');
const PASSWORD = 'fixture-current-password';
const CREDENTIAL = 'fixture-google-credential';
const PROVIDER_BUTTON = 'Continue with test provider';
const DISCONNECTED: User = {
    id: OWNER,
    email: 'telegram-dialog@example.test',
    hasPassword: true,
    isEmailConfirmed: true,
    isActive: true,
    hasTelegramIdentity: false,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
};

let disconnect: Mock<AuthService['disconnectTelegram']>;
let initializeGoogle: Mock<GoogleIdentityService['initializeAsync']>;
let renderGoogle: Mock<GoogleIdentityService['renderButton']>;

beforeEach(() => {
    disconnect = vi.fn<AuthService['disconnectTelegram']>().mockReturnValue(of(DISCONNECTED));
    initializeGoogle = vi.fn<GoogleIdentityService['initializeAsync']>().mockResolvedValue(undefined);
    renderGoogle = vi.fn<GoogleIdentityService['renderButton']>();
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    vi.spyOn(window.history, 'pushState').mockImplementation(() => {});
    stubScrollIntoView();
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            { provide: AuthService, useValue: { disconnectTelegram: disconnect } },
            { provide: GoogleIdentityService, useValue: { initializeAsync: initializeGoogle, renderButton: renderGoogle } },
        ],
    });
});

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

describe('Telegram disconnect with a password', () => {
    it.each([
        { status: HttpStatusCode.TooManyRequests, errorKey: 'USER_MANAGE.TELEGRAM_DISCONNECT_RATE_LIMITED' },
        { status: HttpStatusCode.ServiceUnavailable, errorKey: 'USER_MANAGE.TELEGRAM_DISCONNECT_CONFIRM_ERROR' },
    ])('keeps the dialog open for a $status response and lets the user retry', async ({ status, errorKey }) => {
        const pending = new Subject<User>();
        disconnect.mockReturnValueOnce(pending);
        const { root, result } = openDisconnect({ canUsePassword: true, canUseGoogle: false });
        const input = root.querySelector<HTMLInputElement>('input[autocomplete="current-password"]');
        if (input === null) {
            throw new Error('Expected the current-password input');
        }
        const submit = button(root, 'USER_MANAGE.TELEGRAM_UNLINK');
        const cancel = button(root, 'COMMON.CANCEL');
        expect(submit.disabled).toBe(true);
        input.value = PASSWORD;
        input.dispatchEvent(new Event('input', { bubbles: true }));
        TestBed.tick();
        expect(submit.disabled).toBe(false);

        submit.click();
        await vi.waitFor(() => {
            TestBed.tick();
            expect(cancel.disabled).toBe(true);
        });
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
        TestBed.tick();
        expect(root.isConnected).toBe(true);
        expect(pending.observed).toBe(true);
        expect(disconnect).toHaveBeenCalledExactlyOnceWith(OWNER, { password: PASSWORD });

        pending.error(new HttpErrorResponse({ status }));
        await vi.waitFor(() => {
            TestBed.tick();
            expect(root.querySelector('[role="alert"]')?.textContent).toContain(errorKey);
        });
        expect(root.isConnected).toBe(true);
        expect(cancel.disabled).toBe(false);
        expect(submit.disabled).toBe(false);
        expect(input.value).toBe(PASSWORD);

        submit.click();
        await expect(result).resolves.toEqual(DISCONNECTED);
        expect(root.isConnected).toBe(false);
        expect(disconnect.mock.calls).toEqual([
            [OWNER, { password: PASSWORD }],
            [OWNER, { password: PASSWORD }],
        ]);
    });
});

describe('Telegram disconnect with a Google account', () => {
    it('ignores the provider callback and does not render its button after the dialog was cancelled', async () => {
        useSyntheticGoogleConfiguration();
        const initialization = new Subject<void>();
        const initialized = firstValueFrom(initialization);
        initializeGoogle.mockReturnValueOnce(initialized);
        const { root, result } = openDisconnect({ canUsePassword: false, canUseGoogle: true });
        await vi.waitFor(() => {
            TestBed.tick();
            expect(initializeGoogle).toHaveBeenCalledOnce();
        });
        const callback = googleCredentialCallback();
        expect(root.querySelector('input[type="password"]')).toBeNull();
        button(root, 'COMMON.CANCEL').click();
        await expect(result).resolves.toBeUndefined();

        callback(CREDENTIAL);
        initialization.next();
        initialization.complete();
        await initialized;
        TestBed.tick();

        expect(root.isConnected).toBe(false);
        expect(disconnect).not.toHaveBeenCalled();
        expect(renderGoogle).not.toHaveBeenCalled();
    });

    it('passes the active account and credential from the rendered provider button', async () => {
        useSyntheticGoogleConfiguration();
        renderGoogle.mockImplementationOnce(target => {
            const control = target.ownerDocument.createElement('button');
            control.type = 'button';
            control.textContent = PROVIDER_BUTTON;
            control.addEventListener('click', () => {
                googleCredentialCallback()(CREDENTIAL);
            });
            target.append(control);
        });
        const { root, result } = openDisconnect({ canUsePassword: false, canUseGoogle: true });
        const confirm = await vi.waitFor(() => {
            TestBed.tick();
            return button(root, PROVIDER_BUTTON);
        });
        confirm.click();

        await expect(result).resolves.toEqual(DISCONNECTED);
        expect(disconnect).toHaveBeenCalledExactlyOnceWith(OWNER, { googleCredential: CREDENTIAL });
    });
});

describe('Telegram disconnect with both confirmation methods', () => {
    it('keeps the password submission in flight when a previously opened Google confirmation completes', async () => {
        useSyntheticGoogleConfiguration();
        const passwordAnswer = new Subject<User>();
        const googleAnswer = new Subject<string>();
        const completed = firstValueFrom(googleAnswer);
        const providerStarted = vi.fn();
        disconnect.mockReturnValueOnce(passwordAnswer);
        renderGoogle.mockImplementationOnce(target => {
            const control = target.ownerDocument.createElement('button');
            control.type = 'button';
            control.textContent = PROVIDER_BUTTON;
            control.addEventListener('click', () => {
                providerStarted();
                void completed.then(googleCredentialCallback());
            });
            target.append(control);
        });
        const { root, result } = openDisconnect({ canUsePassword: true, canUseGoogle: true });
        const provider = await vi.waitFor(() => {
            TestBed.tick();
            return button(root, PROVIDER_BUTTON);
        });
        provider.click();
        expect(providerStarted).toHaveBeenCalledOnce();

        const currentInput = root.querySelector<HTMLInputElement>('input[autocomplete="current-password"]');
        if (currentInput === null) {
            throw new Error('Expected the current-password input');
        }
        currentInput.value = PASSWORD;
        currentInput.dispatchEvent(new Event('input', { bubbles: true }));
        TestBed.tick();
        button(root, 'USER_MANAGE.TELEGRAM_UNLINK').click();
        await vi.waitFor(() => {
            TestBed.tick();
            expect(root.querySelector('form')?.getAttribute('aria-busy')).toBe('true');
        });
        expect(disconnect).toHaveBeenCalledExactlyOnceWith(OWNER, { password: PASSWORD });

        googleAnswer.next(CREDENTIAL);
        googleAnswer.complete();
        await completed;
        TestBed.tick();
        expect(root.isConnected).toBe(true);
        expect(button(root, 'COMMON.CANCEL').disabled).toBe(true);
        expect(disconnect).toHaveBeenCalledExactlyOnceWith(OWNER, { password: PASSWORD });

        passwordAnswer.next(DISCONNECTED);
        passwordAnswer.complete();
        await expect(result).resolves.toEqual(DISCONNECTED);
        expect(root.isConnected).toBe(false);
    });
});

function openDisconnect(options: Omit<TelegramDisconnectDialogData, 'owner'>): { root: HTMLElement; result: Promise<User | undefined> } {
    const dialog = TestBed.inject(FdUiDialogService).open<TelegramDisconnectDialogComponent, TelegramDisconnectDialogData, User>(
        TelegramDisconnectDialogComponent,
        { data: { owner: OWNER, ...options } },
    );
    const result = firstValueFrom(dialog.afterClosed());
    TestBed.tick();
    const root = document.querySelector<HTMLElement>('fd-telegram-disconnect-dialog');
    if (root === null) {
        throw new Error('Expected the rendered Telegram confirmation dialog');
    }
    return { root, result };
}

function button(root: HTMLElement, label: string): HTMLButtonElement {
    const found = [...root.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === label);
    if (found === undefined) {
        throw new Error(`Expected Telegram dialog action ${label}`);
    }
    return found;
}

function googleCredentialCallback(): Parameters<GoogleIdentityService['initializeAsync']>[0]['callback'] {
    const latest = initializeGoogle.mock.lastCall;
    if (latest === undefined) {
        throw new Error('Expected the dialog to register the Google callback');
    }
    return latest[0].callback;
}

function useSyntheticGoogleConfiguration(): void {
    const previous = environment.googleClientId;
    environment.googleClientId = 'fixture-dialog-google-client';
    onTestFinished(() => {
        environment.googleClientId = previous;
    });
}

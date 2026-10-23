import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { EmailVerificationCooldownService } from '../../../../shared/auth/email-verification-cooldown.service';
import { SessionEventsService } from '../../../../shared/auth/session-events.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { User } from '../../../../shared/models/user.data';
import { EmailVerificationRealtimeService } from '../../lib/email-verification-realtime.service';
import { EmailVerificationPendingComponent } from './email-verification-pending';

const TOTAL_PROFILE_REQUESTS = 3;
const PROFILE: User = {
    id: entityId<'user'>('verification-user'),
    email: 'pending@example.test',
    hasPassword: true,
    isActive: true,
    isEmailConfirmed: false,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 1,
    fastingCheckInFollowUpReminderHours: 1,
};

let fixture: ComponentFixture<EmailVerificationPendingComponent> | undefined;
let notifyVerified: (() => void) | undefined;
let connect: ReturnType<typeof vi.fn<EmailVerificationRealtimeService['connectAsync']>>;
let disconnect: ReturnType<typeof vi.fn<EmailVerificationRealtimeService['disconnectAsync']>>;
let getInfo: ReturnType<typeof vi.fn<UserFacade['getInfo']>>;
let authenticated: WritableSignal<boolean>;
let currentUserId: WritableSignal<User['id'] | null>;
let setEmailConfirmed: ReturnType<typeof vi.fn<AuthService['setEmailConfirmed']>>;
let resend: ReturnType<typeof vi.fn<AuthService['resendEmailVerification']>>;
let logout: ReturnType<typeof vi.fn<AuthService['onLogoutAsync']>>;
let goHome: ReturnType<typeof vi.fn<NavigationService['navigateToHomeAsync']>>;
let goAuth: ReturnType<typeof vi.fn<NavigationService['navigateToAuthAsync']>>;

beforeEach(() => {
    notifyVerified = undefined;
    connect = vi.fn().mockImplementation(async (_auth: AuthService, onVerified: () => void): Promise<void> => {
        notifyVerified = onVerified;
        await firstValueFrom(of(undefined));
    });
    disconnect = vi.fn().mockResolvedValue(undefined);
    getInfo = vi.fn().mockReturnValue(of(PROFILE));
    authenticated = signal(true);
    currentUserId = signal<User['id'] | null>(PROFILE.id);
    setEmailConfirmed = vi.fn();
    resend = vi.fn().mockReturnValue(of(undefined));
    logout = vi.fn().mockResolvedValue(undefined);
    goHome = vi.fn().mockResolvedValue(undefined);
    goAuth = vi.fn().mockResolvedValue(undefined);
    TestBed.configureTestingModule({
        imports: [EmailVerificationPendingComponent],
        providers: [
            ...provideTranslateTesting(),
            { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
            { provide: UserFacade, useValue: { getInfo } },
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: authenticated,
                    getUserId: (): User['id'] | null => currentUserId(),
                    setEmailConfirmed,
                    resendEmailVerification: resend,
                    onLogoutAsync: logout,
                },
            },
            { provide: NavigationService, useValue: { navigateToHomeAsync: goHome, navigateToAuthAsync: goAuth } },
            { provide: EmailVerificationRealtimeService, useValue: { connectAsync: connect, disconnectAsync: disconnect } },
            {
                provide: EmailVerificationCooldownService,
                useValue: {
                    remainingSeconds: vi.fn<EmailVerificationCooldownService['remainingSeconds']>().mockReturnValue(0),
                    markSent: vi.fn<EmailVerificationCooldownService['markSent']>(),
                },
            },
        ],
    });
});

afterEach(destroyPage);

describe('Email verification realtime presentation', () => {
    it('renders live confirmation, updates account access and navigates home without requesting another profile', async () => {
        const root = await renderPageAsync();
        expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.INITIAL');
        expect(getInfo).toHaveBeenCalledOnce();
        expect(goHome).not.toHaveBeenCalled();
        setEmailConfirmed.mockClear();

        receiveVerification();
        fixture?.detectChanges();

        expect(setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(true);
        expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.SUCCESS');
        expect(goHome).toHaveBeenCalledExactlyOnceWith();
        expect(getInfo).toHaveBeenCalledOnce();
        receiveVerification();
        expect(setEmailConfirmed).toHaveBeenCalledOnce();
        expect(goHome).toHaveBeenCalledOnce();
    });

    it('keeps manual status refresh usable when realtime cannot connect', async () => {
        connect.mockRejectedValueOnce(new Error('Realtime unavailable'));
        getInfo.mockReturnValueOnce(of(PROFILE)).mockReturnValueOnce(of({ ...PROFILE, isEmailConfirmed: true }));
        const root = await renderPageAsync();

        findButton(root, 'AUTH.VERIFY_PENDING.REFRESH').click();
        await fixture?.whenStable();
        fixture?.detectChanges();

        expect(getInfo).toHaveBeenCalledTimes(2);
        expect(setEmailConfirmed).toHaveBeenLastCalledWith(true);
        expect(goHome).toHaveBeenCalledOnce();
        expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.SUCCESS');
    });

    it('redirects an unauthenticated visitor without opening a hub or loading private profile data', async () => {
        authenticated.set(false);

        await renderPageAsync();

        expect(goAuth).toHaveBeenCalledExactlyOnceWith('login');
        expect(connect).not.toHaveBeenCalled();
        expect(getInfo).not.toHaveBeenCalled();
    });
});

describe('Email verification realtime ownership', () => {
    it('disconnects its completed connection when the page is destroyed', async () => {
        await renderPageAsync();

        destroyPage();

        expect(disconnect).toHaveBeenCalledOnce();
    });

    it('ignores a late verification event after destruction instead of changing the current account or redirecting', async () => {
        await renderPageAsync();
        destroyPage();
        setEmailConfirmed.mockClear();

        receiveVerification();

        expect(setEmailConfirmed).not.toHaveBeenCalled();
        expect(goHome).not.toHaveBeenCalled();
    });

    it('cleans up a connection that completes after the page has already been destroyed', async () => {
        const connected = new Subject<void>();
        const completion = firstValueFrom(connected);
        connect.mockImplementation(async (_auth, onVerified) => {
            notifyVerified = onVerified;
            await completion;
        });
        await renderPageAsync();
        expect(connect).toHaveBeenCalledOnce();
        destroyPage();
        expect(disconnect).toHaveBeenCalledOnce();

        connected.next();
        connected.complete();
        await completion;
        await waitForAsyncTasksAsync();

        expect(disconnect).toHaveBeenCalledOnce();
    });
});

describe('Email verification realtime races and recovery', () => {
    it('does not undo live confirmation when the initial profile response arrives late', async () => {
        const profile = new Subject<User | null>();
        getInfo.mockReturnValueOnce(profile);
        const root = await renderPageAsync();

        receiveVerification();
        profile.next(PROFILE);
        profile.complete();
        fixture?.detectChanges();

        expect(setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(true);
        expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.SUCCESS');
        expect(goHome).toHaveBeenCalledOnce();
    });

    it('does not let a manual refresh in flight replace live confirmation with an older unconfirmed profile', async () => {
        const profile = new Subject<User | null>();
        getInfo.mockReturnValueOnce(of(PROFILE)).mockReturnValueOnce(profile);
        const root = await renderPageAsync();
        findButton(root, 'AUTH.VERIFY_PENDING.REFRESH').click();
        expect(getInfo).toHaveBeenCalledTimes(2);
        expect(profile.observed).toBe(true);
        setEmailConfirmed.mockClear();

        receiveVerification();
        profile.next(PROFILE);
        profile.complete();
        fixture?.detectChanges();

        expect(setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(true);
        expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.SUCCESS');
        expect(goHome).toHaveBeenCalledOnce();
    });

    it('handles transport shutdown failure during page destruction without an unhandled rejection', async () => {
        disconnect.mockRejectedValueOnce(new Error('Transport shutdown failed'));
        await renderPageAsync();

        destroyPage();
        await waitForAsyncTasksAsync();

        expect(disconnect).toHaveBeenCalledOnce();
        expect(goHome).not.toHaveBeenCalled();
    });
});

describe('Email verification completed while resending', () => {
    it.each(['success', 'network-error', 'cooldown'] as const)('preserves live success after a late resend %s', async outcome => {
        const response = new Subject<void>();
        resend.mockReturnValueOnce(response);
        const root = await renderPageAsync();
        const resendButton = findButton(root, 'AUTH.VERIFY_PENDING.RESEND');
        resendButton.click();
        fixture?.detectChanges();
        expect(resendButton.disabled).toBe(true);
        setEmailConfirmed.mockClear();

        receiveVerification();
        finishResend(response, outcome);
        fixture?.detectChanges();

        expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.SUCCESS');
        expect(resendButton.disabled).toBe(false);
        expect(setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(true);
        expect(goHome).toHaveBeenCalledOnce();
        receiveVerification();
        resendButton.click();
        expect(resend).toHaveBeenCalledOnce();
        expect(goHome).toHaveBeenCalledOnce();
    });
});

describe('Email verification page account ownership', () => {
    it.each([entityId<'user'>('replacement-user'), null])('ignores the previous account event after the owner becomes %s', async userId => {
        translateEmailDescription();
        const root = await renderPageAsync();
        expect(root.textContent).toContain(PROFILE.email);
        setEmailConfirmed.mockClear();
        const sessionEvents = TestBed.inject(SessionEventsService);
        if (userId === null) {
            sessionEvents.notifySessionEnded();
        } else {
            sessionEvents.notifyAuthenticated();
        }
        currentUserId.set(userId);
        authenticated.set(userId !== null);
        fixture?.detectChanges();

        findButton(root, 'AUTH.VERIFY_PENDING.USE_ANOTHER_EMAIL').click();
        receiveVerification();

        expect(logout).not.toHaveBeenCalled();
        expect(setEmailConfirmed).not.toHaveBeenCalled();
        expect(goHome).not.toHaveBeenCalled();
        expect(disconnect).toHaveBeenCalledOnce();
        expect(root.textContent).not.toContain(PROFILE.email);
        findButton(root, 'AUTH.VERIFY_PENDING.REFRESH').click();
        expect(getInfo).toHaveBeenCalledOnce();
    });

    it('rejects a previous-account callback even before account cleanup is delivered', async () => {
        await renderPageAsync();
        setEmailConfirmed.mockClear();
        currentUserId.set(entityId<'user'>('replacement-user'));

        receiveVerification();

        expect(setEmailConfirmed).not.toHaveBeenCalled();
        expect(goHome).not.toHaveBeenCalled();
        expect(disconnect).toHaveBeenCalledOnce();
    });

    it('releases an old profile request when the account changes before its response', async () => {
        const response = new Subject<User | null>();
        getInfo.mockReturnValueOnce(response);
        translateEmailDescription();
        const root = await renderPageAsync();
        expect(getInfo).toHaveBeenCalledOnce();
        expect(response.observed).toBe(true);
        TestBed.inject(SessionEventsService).notifyAuthenticated();
        currentUserId.set(entityId<'user'>('replacement-user'));
        fixture?.detectChanges();
        expect(response.observed).toBe(false);

        response.next({ ...PROFILE, isEmailConfirmed: true });
        response.complete();
        fixture?.detectChanges();

        expect(setEmailConfirmed).not.toHaveBeenCalled();
        expect(goHome).not.toHaveBeenCalled();
        expect(root.textContent).not.toContain(PROFILE.email);
        expect(disconnect).toHaveBeenCalledOnce();
    });
});

describe('Email verification cancels pending work for a replacement session', () => {
    it.each(['refresh', 'resend'] as const)(
        'releases a pending %s and prevents stale actions from changing the replacement account',
        async kind => {
            const profileResponse = new Subject<User | null>();
            const resendResponse = new Subject<void>();
            getInfo.mockReturnValueOnce(of(PROFILE)).mockReturnValue(profileResponse);
            resend.mockReturnValue(resendResponse);
            translateEmailDescription();
            const root = await renderPageAsync();
            expect(root.textContent).toContain(PROFILE.email);
            const actionKey = kind === 'refresh' ? 'AUTH.VERIFY_PENDING.REFRESH' : 'AUTH.VERIFY_PENDING.RESEND';
            const action = findButton(root, actionKey);
            action.click();
            fixture?.detectChanges();
            const response = kind === 'refresh' ? profileResponse : resendResponse;
            expect(response.observed).toBe(true);
            expect(action.disabled).toBe(true);
            const priorProfileRequests = getInfo.mock.calls.length;
            const priorResendRequests = resend.mock.calls.length;
            setEmailConfirmed.mockClear();

            TestBed.inject(SessionEventsService).notifyAuthenticated();
            currentUserId.set(entityId<'user'>('replacement-user'));
            fixture?.detectChanges();

            expect(response.observed).toBe(false);
            expect(action.disabled).toBe(false);
            if (kind === 'refresh') {
                profileResponse.next({ ...PROFILE, isEmailConfirmed: true });
                profileResponse.complete();
            } else {
                resendResponse.next();
                resendResponse.complete();
            }
            fixture?.detectChanges();
            expect(root.querySelector('[role="status"]')).toBeNull();
            expect(root.textContent).not.toContain(PROFILE.email);
            expect(setEmailConfirmed).not.toHaveBeenCalled();
            expect(goHome).not.toHaveBeenCalled();
            expect(disconnect).toHaveBeenCalledOnce();

            findButton(root, 'AUTH.VERIFY_PENDING.REFRESH').click();
            findButton(root, 'AUTH.VERIFY_PENDING.RESEND').click();
            findButton(root, 'AUTH.VERIFY_PENDING.USE_ANOTHER_EMAIL').click();
            expect(getInfo).toHaveBeenCalledTimes(priorProfileRequests);
            expect(resend).toHaveBeenCalledTimes(priorResendRequests);
            expect(logout).not.toHaveBeenCalled();
            expect(goHome).not.toHaveBeenCalled();
        },
    );
});

describe('Email verification page releases work before logout completes', () => {
    it('disconnects immediately and ignores verification while logout is still pending', async () => {
        const response = new Subject<void>();
        const completion = firstValueFrom(response);
        logout.mockReturnValueOnce(completion);
        const root = await renderPageAsync();
        setEmailConfirmed.mockClear();

        findButton(root, 'AUTH.VERIFY_PENDING.USE_ANOTHER_EMAIL').click();

        expect(logout).toHaveBeenCalledExactlyOnceWith(true);
        expect(disconnect).toHaveBeenCalledOnce();
        receiveVerification();
        expect(setEmailConfirmed).not.toHaveBeenCalled();
        expect(goHome).not.toHaveBeenCalled();
        response.next();
        response.complete();
        await completion;
        await waitForAsyncTasksAsync();
        expect(disconnect).toHaveBeenCalledOnce();
    });
});

describe('Email verification completed by manual refresh', () => {
    it.each(['initial-profile', 'resend'] as const)(
        'retains manual confirmation when an older %s finishes before navigation',
        async kind => {
            const initialProfile = new Subject<User | null>();
            const resendResponse = new Subject<void>();
            const navigationResponse = new Subject<void>();
            const navigation = firstValueFrom(navigationResponse);
            goHome.mockReturnValueOnce(navigation);
            getInfo
                .mockReturnValueOnce(kind === 'initial-profile' ? initialProfile : of(PROFILE))
                .mockReturnValueOnce(of({ ...PROFILE, isEmailConfirmed: true }));
            resend.mockReturnValueOnce(resendResponse);
            const root = await renderPageAsync();
            if (kind === 'resend') {
                findButton(root, 'AUTH.VERIFY_PENDING.RESEND').click();
                fixture?.detectChanges();
                expect(resend).toHaveBeenCalledOnce();
                expect(findButton(root, 'AUTH.VERIFY_PENDING.RESEND').disabled).toBe(true);
            }
            const pendingResponse = kind === 'initial-profile' ? initialProfile : resendResponse;
            expect(pendingResponse.observed).toBe(true);
            expect(getInfo).toHaveBeenCalledOnce();
            setEmailConfirmed.mockClear();

            findButton(root, 'AUTH.VERIFY_PENDING.REFRESH').click();
            fixture?.detectChanges();

            expect(getInfo).toHaveBeenCalledTimes(2);
            expect(setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(true);
            expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.SUCCESS');
            expect(goHome).toHaveBeenCalledOnce();
            if (kind === 'initial-profile') {
                initialProfile.next(PROFILE);
                initialProfile.complete();
            } else {
                resendResponse.next();
                resendResponse.complete();
            }
            fixture?.detectChanges();

            expect(setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(true);
            expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.SUCCESS');
            expect(findButton(root, 'AUTH.VERIFY_PENDING.RESEND').disabled).toBe(false);
            expect(goHome).toHaveBeenCalledOnce();
            receiveVerification();
            expect(setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(true);
            expect(goHome).toHaveBeenCalledOnce();
            navigationResponse.next();
            navigationResponse.complete();
            await navigation;
            await waitForAsyncTasksAsync();
        },
    );
});

describe('Email verification manual refresh recovery', () => {
    it('ends a null-result refresh with a recoverable error and accepts the next confirmed profile', async () => {
        const response = new Subject<User | null>();
        getInfo
            .mockReturnValueOnce(of(PROFILE))
            .mockReturnValueOnce(response)
            .mockReturnValueOnce(of({ ...PROFILE, isEmailConfirmed: true }));
        const root = await renderPageAsync();
        const refreshButton = findButton(root, 'AUTH.VERIFY_PENDING.REFRESH');
        setEmailConfirmed.mockClear();
        refreshButton.click();
        fixture?.detectChanges();
        expect(refreshButton.disabled).toBe(true);

        response.next(null);
        response.complete();
        fixture?.detectChanges();

        expect(refreshButton.disabled).toBe(false);
        expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.ERROR');
        expect(setEmailConfirmed).not.toHaveBeenCalled();
        expect(goHome).not.toHaveBeenCalled();
        refreshButton.click();
        await fixture?.whenStable();
        fixture?.detectChanges();
        expect(getInfo).toHaveBeenCalledTimes(TOTAL_PROFILE_REQUESTS);
        expect(setEmailConfirmed).toHaveBeenCalledExactlyOnceWith(true);
        expect(root.querySelector('[role="status"]')?.textContent).toContain('AUTH.VERIFY_PENDING.SUCCESS');
        expect(goHome).toHaveBeenCalledOnce();
    });
});

async function renderPageAsync(): Promise<HTMLElement> {
    fixture = TestBed.createComponent(EmailVerificationPendingComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
}

function destroyPage(): void {
    fixture?.destroy();
    fixture = undefined;
}

function receiveVerification(): void {
    if (notifyVerified === undefined) {
        throw new Error('The page must register its verification callback.');
    }
    notifyVerified();
}

function findButton(root: HTMLElement, text: string): HTMLButtonElement {
    const button = [...root.querySelectorAll('button')].find(candidate => candidate.textContent.includes(text));
    if (button === undefined) {
        throw new Error(`Missing verification action: ${text}`);
    }
    return button;
}

function finishResend(response: Subject<void>, outcome: 'success' | 'network-error' | 'cooldown'): void {
    if (outcome === 'success') {
        response.next();
        response.complete();
        return;
    }
    response.error(
        outcome === 'network-error'
            ? new Error('Resend unavailable')
            : { error: { error: 'Validation.Invalid', errors: { EmailVerification: ['Cooldown active'] } } },
    );
}

function translateEmailDescription(): void {
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { AUTH: { VERIFY_PENDING: { DESCRIPTION: 'Account: {{email}}' } } });
    translate.use('en');
}

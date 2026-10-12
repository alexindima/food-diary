import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { EmailVerificationCooldownService } from '../../../../shared/auth/email-verification-cooldown.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { BrowserStorageService } from '../../../../shared/platform/browser-storage.service';
import { EmailVerificationRealtimeService } from '../../lib/email-verification-realtime.service';
import { EmailVerificationPendingComponent } from './email-verification-pending';

const RESEND_PAUSE_SECONDS = 60;
const RESEND_PAUSE_MILLISECONDS = 60_000;

let userServiceMock: { getInfo: ReturnType<typeof vi.fn> };
let authServiceMock: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    getUserId: ReturnType<typeof vi.fn>;
    resendEmailVerification: ReturnType<typeof vi.fn>;
    setEmailConfirmed: ReturnType<typeof vi.fn>;
};
let navigationServiceMock: {
    navigateToAuthAsync: ReturnType<typeof vi.fn>;
    navigateToHomeAsync: ReturnType<typeof vi.fn>;
};
let realtimeServiceMock: {
    connectAsync: ReturnType<typeof vi.fn>;
    disconnectAsync: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    TestBed.inject(BrowserStorageService).removeItem('session', 'email-verification-cooldown:user-1');
    userServiceMock = {
        getInfo: vi.fn().mockReturnValue(
            of({
                id: 'user-1',
                email: 'test@example.com',
                isActive: true,
                isEmailConfirmed: false,
            }),
        ),
    };
    authServiceMock = {
        isAuthenticated: vi.fn().mockReturnValue(true),
        getUserId: vi.fn().mockReturnValue('user-1'),
        setEmailConfirmed: vi.fn(),
        resendEmailVerification: vi.fn().mockReturnValue(of(true)),
    };
    navigationServiceMock = {
        navigateToAuthAsync: vi.fn().mockResolvedValue(void 0),
        navigateToHomeAsync: vi.fn().mockResolvedValue(void 0),
    };
    realtimeServiceMock = {
        connectAsync: vi.fn().mockResolvedValue(void 0),
        disconnectAsync: vi.fn().mockResolvedValue(void 0),
    };
});

afterEach(() => vi.useRealTimers());

describe('EmailVerificationPendingComponent resend', () => {
    it('should not resend verification email on regular pending page open', () => {
        createComponent(false);

        expect(authServiceMock.resendEmailVerification).not.toHaveBeenCalled();
    });

    it('should resend verification email once when requested by login flow', () => {
        createComponent(true);

        expect(authServiceMock.resendEmailVerification).toHaveBeenCalledTimes(1);
    });

    it('restores the registration pause and rejects clicks until the deadline', () => {
        vi.useFakeTimers();
        const component = createComponent(true, true);
        expect(component['resendCooldownSeconds']()).toBe(RESEND_PAUSE_SECONDS);
        component['onResendEmail']();
        expect(authServiceMock.resendEmailVerification).not.toHaveBeenCalled();
        vi.advanceTimersByTime(RESEND_PAUSE_MILLISECONDS);
        expect(component['resendCooldownSeconds']()).toBe(0);
        component['onResendEmail']();
        expect(authServiceMock.resendEmailVerification).toHaveBeenCalledOnce();
    });

    it('prevents repeated requests while sending', () => {
        const response = new Subject<void>();
        authServiceMock.resendEmailVerification.mockReturnValue(response);
        const component = createComponent(false);
        component['onResendEmail']();
        component['onResendEmail']();
        expect(authServiceMock.resendEmailVerification).toHaveBeenCalledOnce();
    });

    it('shows a pause for the server cooldown and preserves other validation failures as errors', () => {
        authServiceMock.resendEmailVerification.mockReturnValue(
            throwError(() => ({
                error: { error: 'Validation.Invalid', errors: { EmailVerification: ['Wait before requesting another email'] } },
            })),
        );
        const component = createComponent(false);
        component['onResendEmail']();
        expect(component['resendCooldownSeconds']()).toBe(RESEND_PAUSE_SECONDS);
        expect(component['statusMessage']()).toBeNull();
    });

    it('keeps a recoverable error for network and unrelated validation failures', () => {
        authServiceMock.resendEmailVerification.mockReturnValue(
            throwError(() => ({
                error: { error: 'Validation.Invalid', errors: { UserId: ['Invalid user'] } },
            })),
        );
        const component = createComponent(false);
        component['onResendEmail']();
        expect(component['resendCooldownSeconds']()).toBe(0);
        expect(component['statusMessage']()).toBe('AUTH.VERIFY_PENDING.RESEND_ERROR');
    });
});

describe('Email verification pause before profile load', () => {
    it('restores the account deadline before a delayed profile request completes', () => {
        vi.useFakeTimers();
        const profile = new Subject<{ id: string; email: string; isEmailConfirmed: boolean }>();
        userServiceMock.getInfo.mockReturnValue(profile);
        const component = createComponent(false, true);
        expect(component['email']()).toBeNull();
        expect(component['resendCooldownSeconds']()).toBe(RESEND_PAUSE_SECONDS);
        component['onResendEmail']();
        expect(authServiceMock.resendEmailVerification).not.toHaveBeenCalled();
        profile.next({ id: 'user-1', email: 'test@example.test', isEmailConfirmed: false });
        expect(component['resendCooldownSeconds']()).toBe(RESEND_PAUSE_SECONDS);
    });
});

describe('EmailVerificationPendingComponent navigation', () => {
    it('should navigate home when email is already confirmed', () => {
        userServiceMock.getInfo.mockReturnValue(
            of({
                id: 'user-1',
                email: 'test@example.com',
                isActive: true,
                isEmailConfirmed: true,
            }),
        );

        createComponent(true);

        expect(authServiceMock.resendEmailVerification).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToHomeAsync).toHaveBeenCalled();
    });
});

function createComponent(autoResend: boolean, justRegistered = false): EmailVerificationPendingComponent {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        imports: [EmailVerificationPendingComponent],
        providers: [
            { provide: UserFacade, useValue: userServiceMock },
            { provide: AuthService, useValue: authServiceMock },
            { provide: NavigationService, useValue: navigationServiceMock },
            { provide: EmailVerificationRealtimeService, useValue: realtimeServiceMock },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        queryParamMap: convertToParamMap(autoResend ? { autoResend: 'true' } : {}),
                    },
                },
            },
        ],
    });
    TestBed.overrideComponent(EmailVerificationPendingComponent, { set: { template: '' } });
    if (justRegistered) {
        TestBed.inject(EmailVerificationCooldownService).markSent('user-1');
    }
    return TestBed.createComponent(EmailVerificationPendingComponent).componentInstance;
}

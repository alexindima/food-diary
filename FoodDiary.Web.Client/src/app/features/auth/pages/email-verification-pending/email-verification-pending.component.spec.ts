import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { UserService } from '../../../../shared/api/user.service';
import { EmailVerificationRealtimeService } from '../../lib/email-verification-realtime.service';
import { EmailVerificationPendingComponent } from './email-verification-pending.component';

let userServiceMock: { getInfo: ReturnType<typeof vi.fn> };
let authServiceMock: {
    isAuthenticated: ReturnType<typeof vi.fn>;
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
        setEmailConfirmed: vi.fn(),
        resendEmailVerification: vi.fn().mockReturnValue(of(true)),
    };
    navigationServiceMock = {
        navigateToAuthAsync: vi.fn().mockResolvedValue(undefined),
        navigateToHomeAsync: vi.fn().mockResolvedValue(undefined),
    };
    realtimeServiceMock = {
        connectAsync: vi.fn().mockResolvedValue(undefined),
        disconnectAsync: vi.fn().mockResolvedValue(undefined),
    };
});

describe('EmailVerificationPendingComponent resend', () => {
    it('should not resend verification email on regular pending page open', () => {
        createComponent(false);

        expect(authServiceMock.resendEmailVerification).not.toHaveBeenCalled();
    });

    it('should resend verification email once when requested by login flow', () => {
        createComponent(true);

        expect(authServiceMock.resendEmailVerification).toHaveBeenCalledTimes(1);
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

function createComponent(autoResend: boolean): void {
    TestBed.configureTestingModule({
        imports: [EmailVerificationPendingComponent],
        providers: [
            { provide: UserService, useValue: userServiceMock },
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
    })
        .overrideComponent(EmailVerificationPendingComponent, {
            set: { template: '' },
        })
        .createComponent(EmailVerificationPendingComponent);
}

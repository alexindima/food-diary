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

describe('EmailVerificationPendingComponent', () => {
    let userServiceMock: { getInfo: ReturnType<typeof vi.fn> };
    let authServiceMock: {
        isAuthenticated: ReturnType<typeof vi.fn>;
        setEmailConfirmed: ReturnType<typeof vi.fn>;
        resendEmailVerification: ReturnType<typeof vi.fn>;
    };
    let navigationServiceMock: {
        navigateToAuth: ReturnType<typeof vi.fn>;
        navigateToHome: ReturnType<typeof vi.fn>;
    };
    let realtimeServiceMock: {
        connect: ReturnType<typeof vi.fn>;
        disconnect: ReturnType<typeof vi.fn>;
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
            navigateToAuth: vi.fn().mockResolvedValue(undefined),
            navigateToHome: vi.fn().mockResolvedValue(undefined),
        };
        realtimeServiceMock = {
            connect: vi.fn().mockResolvedValue(undefined),
            disconnect: vi.fn().mockResolvedValue(undefined),
        };
    });

    it('should not resend verification email on regular pending page open', () => {
        createComponent(false);

        expect(authServiceMock.resendEmailVerification).not.toHaveBeenCalled();
    });

    it('should resend verification email once when requested by login flow', () => {
        createComponent(true);

        expect(authServiceMock.resendEmailVerification).toHaveBeenCalledTimes(1);
    });

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
        expect(navigationServiceMock.navigateToHome).toHaveBeenCalled();
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
});

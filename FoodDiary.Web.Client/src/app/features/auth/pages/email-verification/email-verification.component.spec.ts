import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { EmailVerificationComponent } from './email-verification.component';

let authServiceMock: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    verifyEmail: ReturnType<typeof vi.fn>;
};
let navigationServiceMock: {
    navigateToAuthAsync: ReturnType<typeof vi.fn>;
    navigateToHomeAsync: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    authServiceMock = {
        isAuthenticated: vi.fn().mockReturnValue(true),
        verifyEmail: vi.fn().mockReturnValue(of(true)),
    };
    navigationServiceMock = {
        navigateToAuthAsync: vi.fn().mockResolvedValue(undefined),
        navigateToHomeAsync: vi.fn().mockResolvedValue(undefined),
    };
});

describe('EmailVerificationComponent', () => {
    it('should verify email when token is present', () => {
        const component = createComponent({ userId: 'user-1', token: 'token-1' });

        expect(authServiceMock.verifyEmail).toHaveBeenCalledWith('user-1', 'token-1');
        expect(component.state()).toBe('success');
        expect(component.isBusy()).toBe(false);
    });

    it('should set invalid error when token is missing', () => {
        const component = createComponent({});

        expect(authServiceMock.verifyEmail).not.toHaveBeenCalled();
        expect(component.state()).toBe('error');
        expect(component.errorMessage()).toBe('AUTH.VERIFY.ERROR_INVALID');
    });

    it('should allow retry with the resolved token', () => {
        authServiceMock.verifyEmail.mockReturnValueOnce(throwError(() => new Error('fail'))).mockReturnValueOnce(of(true));
        const component = createComponent({ userId: 'user-1', token: 'token-1' });

        expect(component.state()).toBe('error');

        component.onRetry();

        expect(authServiceMock.verifyEmail).toHaveBeenCalledTimes(2);
        expect(component.state()).toBe('success');
    });

    it('should navigate home on continue when authenticated', () => {
        const component = createComponent({ userId: 'user-1', token: 'token-1' });

        component.onContinue();

        expect(navigationServiceMock.navigateToHomeAsync).toHaveBeenCalled();
    });

    it('should navigate to login on continue when unauthenticated', () => {
        authServiceMock.isAuthenticated.mockReturnValue(false);
        const component = createComponent({ userId: 'user-1', token: 'token-1' });

        component.onContinue();

        expect(navigationServiceMock.navigateToAuthAsync).toHaveBeenCalledWith('login');
    });
});

function createComponent(queryParams: Record<string, string>): EmailVerificationComponent {
    TestBed.configureTestingModule({
        imports: [EmailVerificationComponent],
        providers: [
            { provide: AuthService, useValue: authServiceMock },
            { provide: NavigationService, useValue: navigationServiceMock },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        queryParamMap: convertToParamMap(queryParams),
                    },
                },
            },
        ],
    }).overrideComponent(EmailVerificationComponent, {
        set: { template: '' },
    });

    const fixture = TestBed.createComponent(EmailVerificationComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
}

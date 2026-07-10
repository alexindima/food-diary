import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { User } from '../../../../shared/models/user.data';
import type { AuthResponse, ConfirmPasswordResetRequest } from '../../models/auth.data';
import { PasswordResetComponent } from './password-reset';

const USER: User = {
    id: 'user-1',
    email: 'user@example.com',
    hasPassword: true,
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: true,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
    isActive: true,
    isEmailConfirmed: true,
};
const AUTH_RESPONSE: AuthResponse = {
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    user: USER,
};
const DEFAULT_QUERY_PARAMS = { userId: 'user-1', token: 'tok-abc' };

type PasswordResetTestContext = {
    authServiceSpy: { confirmPasswordReset: ReturnType<typeof vi.fn> };
    component: PasswordResetComponent;
    fixture: ComponentFixture<PasswordResetComponent>;
    navigationServiceSpy: {
        navigateToAuthAsync: ReturnType<typeof vi.fn>;
        navigateToHomeAsync: ReturnType<typeof vi.fn>;
    };
};

function createComponent(queryParams: Record<string, string> = DEFAULT_QUERY_PARAMS): PasswordResetTestContext {
    const authServiceSpy = { confirmPasswordReset: vi.fn() };
    const navigationServiceSpy = { navigateToHomeAsync: vi.fn(), navigateToAuthAsync: vi.fn() };
    navigationServiceSpy.navigateToHomeAsync.mockResolvedValue(undefined);
    navigationServiceSpy.navigateToAuthAsync.mockResolvedValue(undefined);

    TestBed.configureTestingModule({
        imports: [PasswordResetComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AuthService, useValue: authServiceSpy },
            { provide: NavigationService, useValue: navigationServiceSpy },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        queryParamMap: convertToParamMap(queryParams),
                    },
                },
            },
        ],
    });

    const translateServiceSpy = TestBed.inject(TranslateService);
    vi.spyOn(translateServiceSpy, 'instant').mockImplementation((key: string | string[]) => (Array.isArray(key) ? key[0] : key));

    const fixture = TestBed.createComponent(PasswordResetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { authServiceSpy, component, fixture, navigationServiceSpy };
}

function setValidPassword(component: PasswordResetComponent): void {
    component['form'].password().value.set('newPassword123');
    component['form'].confirmPassword().value.set('newPassword123');
}

describe('PasswordResetComponent', () => {
    it('should create', () => {
        const { component } = createComponent();
        expect(component).toBeTruthy();
    });
});

describe('PasswordResetComponent form validation', () => {
    it('should initialize form with password and confirmPassword fields', () => {
        const { component } = createComponent();
        expect(component['form'].password).toBeDefined();
        expect(component['form'].confirmPassword).toBeDefined();
    });

    it('should validate required password', () => {
        const { component } = createComponent();
        const control = component['form'].password;
        control().value.set('');
        control().markAsTouched();
        expect(control().getError('required')).toBeDefined();
    });

    it('should validate password minimum length', () => {
        const { component } = createComponent();
        const control = component['form'].password;
        control().value.set('abc');
        control().markAsTouched();
        expect(control().getError('minLength')).toBeDefined();

        control().value.set('abcdef');
        expect(control().getError('minLength')).toBeUndefined();
    });

    it('should validate password confirmation match', () => {
        const { component } = createComponent();
        component['form'].password().value.set('validPass1');
        component['form'].confirmPassword().value.set('differentPass');
        component['form'].confirmPassword().markAsTouched();

        expect(component['form'].confirmPassword().getError('matchField')).toBeDefined();

        component['form'].confirmPassword().value.set('validPass1');
        expect(component['form'].confirmPassword().getError('matchField')).toBeUndefined();
    });
});

describe('PasswordResetComponent submit', () => {
    it('should prevent native form submit when confirming reset', async () => {
        const { authServiceSpy, component, fixture } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));
        setValidPassword(component);
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledTimes(1);
    });

    it('should call confirmPasswordReset on submit', () => {
        const { authServiceSpy, component } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));

        setValidPassword(component);
        component['onSubmit']();

        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledTimes(1);
        const call = authServiceSpy.confirmPasswordReset.mock.calls.at(-1);
        expect(call).toBeDefined();
        const arg = call?.[0] as ConfirmPasswordResetRequest | undefined;
        if (arg === undefined) {
            throw new Error('Expected confirm password reset argument.');
        }

        expect(arg.userId).toBe('user-1');
        expect(arg.token).toBe('tok-abc');
        expect(arg.newPassword).toBe('newPassword123');
    });

    it('should navigate to home on successful submit', async () => {
        const { authServiceSpy, component, navigationServiceSpy } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));

        setValidPassword(component);
        component['onSubmit']();

        await vi.waitFor(() => {
            expect(navigationServiceSpy.navigateToHomeAsync).toHaveBeenCalled();
        });
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should handle submit error', async () => {
        const { authServiceSpy, component } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(throwError(() => new Error('fail')));

        setValidPassword(component);
        component['onSubmit']();

        await vi.waitFor(() => {
            expect(component['state']()).toBe('ready');
        });
        expect(component['errorMessage']()).toBe('AUTH.RESET.ERROR_GENERIC');
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should allow retry after a submit error', async () => {
        const { authServiceSpy, component, navigationServiceSpy } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValueOnce(throwError(() => new Error('fail'))).mockReturnValueOnce(of(AUTH_RESPONSE));
        setValidPassword(component);

        component['onSubmit']();
        await vi.waitFor(() => {
            expect(component['errorMessage']()).toBe('AUTH.RESET.ERROR_GENERIC');
        });
        component['onSubmit']();

        await vi.waitFor(() => {
            expect(navigationServiceSpy.navigateToHomeAsync).toHaveBeenCalled();
        });
        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledTimes(2);
    });
});

describe('PasswordResetComponent invalid states', () => {
    it('should set invalid state when token is missing', () => {
        const { component } = createComponent({ userId: '', token: '' });
        expect(component['state']()).toBe('invalid');
        expect(component['errorMessage']()).toBe('AUTH.RESET.INVALID');
    });

    it('should not submit when form is invalid', () => {
        const { authServiceSpy, component } = createComponent();
        component['form'].password().value.set('');
        component['onSubmit']();
        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });

    it('should not submit when already submitting', () => {
        const { authServiceSpy, component } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));

        setValidPassword(component);
        component['isSubmitting'].set(true);
        component['onSubmit']();

        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });
});

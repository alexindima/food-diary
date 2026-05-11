import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { User } from '../../../../shared/models/user.data';
import type { AuthResponse, ConfirmPasswordResetRequest } from '../../models/auth.data';
import { PasswordResetComponent } from './password-reset.component';

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

type PasswordResetTestContext = {
    authServiceSpy: { confirmPasswordReset: ReturnType<typeof vi.fn> };
    component: PasswordResetComponent;
    fixture: ComponentFixture<PasswordResetComponent>;
    navigationServiceSpy: {
        navigateToAuthAsync: ReturnType<typeof vi.fn>;
        navigateToHomeAsync: ReturnType<typeof vi.fn>;
    };
};

function createComponent(queryParams: Record<string, string> = { userId: 'user-1', token: 'tok-abc' }): PasswordResetTestContext {
    const authServiceSpy = { confirmPasswordReset: vi.fn() };
    const navigationServiceSpy = { navigateToHomeAsync: vi.fn(), navigateToAuthAsync: vi.fn() };
    navigationServiceSpy.navigateToHomeAsync.mockReturnValue(Promise.resolve());
    navigationServiceSpy.navigateToAuthAsync.mockReturnValue(Promise.resolve());

    TestBed.configureTestingModule({
        imports: [PasswordResetComponent, TranslateModule.forRoot()],
        providers: [
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
    component.form.controls.password.setValue('newPassword123');
    component.form.controls.confirmPassword.setValue('newPassword123');
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
        expect(component.form.contains('password')).toBe(true);
        expect(component.form.contains('confirmPassword')).toBe(true);
    });

    it('should validate required password', () => {
        const { component } = createComponent();
        const control = component.form.controls.password;
        control.setValue('');
        control.markAsTouched();
        expect(control.hasError('required')).toBe(true);
    });

    it('should validate password minimum length', () => {
        const { component } = createComponent();
        const control = component.form.controls.password;
        control.setValue('abc');
        control.markAsTouched();
        expect(control.hasError('minlength')).toBe(true);

        control.setValue('abcdef');
        expect(control.hasError('minlength')).toBe(false);
    });

    it('should validate password confirmation match', () => {
        const { component } = createComponent();
        component.form.controls.password.setValue('validPass1');
        component.form.controls.confirmPassword.setValue('differentPass');
        component.form.controls.confirmPassword.markAsTouched();

        expect(component.form.controls.confirmPassword.hasError('matchField')).toBe(true);

        component.form.controls.confirmPassword.setValue('validPass1');
        expect(component.form.controls.confirmPassword.hasError('matchField')).toBe(false);
    });
});

describe('PasswordResetComponent submit', () => {
    it('should call confirmPasswordReset on submit', () => {
        const { authServiceSpy, component } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));

        setValidPassword(component);
        component.onSubmit();

        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledTimes(1);
        const arg = authServiceSpy.confirmPasswordReset.mock.calls[authServiceSpy.confirmPasswordReset.mock.calls.length - 1][0] as
            | ConfirmPasswordResetRequest
            | undefined;
        if (arg === undefined) {
            throw new Error('Expected confirm password reset argument.');
        }

        expect(arg.userId).toBe('user-1');
        expect(arg.token).toBe('tok-abc');
        expect(arg.newPassword).toBe('newPassword123');
    });

    it('should navigate to home on successful submit', () => {
        const { authServiceSpy, component, navigationServiceSpy } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));

        setValidPassword(component);
        component.onSubmit();

        expect(navigationServiceSpy.navigateToHomeAsync).toHaveBeenCalled();
        expect(component.isSubmitting()).toBe(false);
    });

    it('should handle submit error', () => {
        const { authServiceSpy, component } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(throwError(() => new Error('fail')));

        setValidPassword(component);
        component.onSubmit();

        expect(component.state()).toBe('error');
        expect(component.errorMessage()).toBe('AUTH.RESET.ERROR_GENERIC');
        expect(component.isSubmitting()).toBe(false);
    });
});

describe('PasswordResetComponent invalid states', () => {
    it('should set invalid state when token is missing', () => {
        const { component } = createComponent({ userId: '', token: '' });
        expect(component.state()).toBe('invalid');
        expect(component.errorMessage()).toBe('AUTH.RESET.INVALID');
    });

    it('should not submit when form is invalid', () => {
        const { authServiceSpy, component } = createComponent();
        component.form.controls.password.setValue('');
        component.onSubmit();
        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });

    it('should not submit when already submitting', () => {
        const { authServiceSpy, component } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));

        setValidPassword(component);
        component.isSubmitting.set(true);
        component.onSubmit();

        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });
});

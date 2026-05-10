import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { User } from '../../../../shared/models/user.data';
import type { AuthResponse } from '../../models/auth.data';
import { PasswordResetComponent } from './password-reset.component';

describe('PasswordResetComponent', () => {
    let component: PasswordResetComponent;
    let fixture: ComponentFixture<PasswordResetComponent>;
    let authServiceSpy: { confirmPasswordReset: ReturnType<typeof vi.fn> };
    let navigationServiceSpy: { navigateToHomeAsync: ReturnType<typeof vi.fn>; navigateToAuthAsync: ReturnType<typeof vi.fn> };
    let translateServiceSpy: TranslateService;

    const user: User = {
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
    const authResponse: AuthResponse = {
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        user,
    };

    function createComponent(queryParams: Record<string, string> = { userId: 'user-1', token: 'tok-abc' }): void {
        authServiceSpy = { confirmPasswordReset: vi.fn() };
        navigationServiceSpy = { navigateToHomeAsync: vi.fn(), navigateToAuthAsync: vi.fn() };
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

        translateServiceSpy = TestBed.inject(TranslateService);
        vi.spyOn(translateServiceSpy, 'instant').mockImplementation((key: string | string[]) => (Array.isArray(key) ? key[0] : key));

        fixture = TestBed.createComponent(PasswordResetComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize form with password and confirmPassword fields', () => {
        createComponent();
        expect(component.form.contains('password')).toBe(true);
        expect(component.form.contains('confirmPassword')).toBe(true);
    });

    it('should validate required password', () => {
        createComponent();
        const control = component.form.controls.password;
        control.setValue('');
        control.markAsTouched();
        expect(control.hasError('required')).toBe(true);
    });

    it('should validate password minimum length', () => {
        createComponent();
        const control = component.form.controls.password;
        control.setValue('abc');
        control.markAsTouched();
        expect(control.hasError('minlength')).toBe(true);

        control.setValue('abcdef');
        expect(control.hasError('minlength')).toBe(false);
    });

    it('should validate password confirmation match', () => {
        createComponent();
        component.form.controls.password.setValue('validPass1');
        component.form.controls.confirmPassword.setValue('differentPass');
        component.form.controls.confirmPassword.markAsTouched();

        expect(component.form.controls.confirmPassword.hasError('matchField')).toBe(true);

        component.form.controls.confirmPassword.setValue('validPass1');
        expect(component.form.controls.confirmPassword.hasError('matchField')).toBe(false);
    });

    it('should call confirmPasswordReset on submit', () => {
        createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(authResponse));

        component.form.controls.password.setValue('newPassword123');
        component.form.controls.confirmPassword.setValue('newPassword123');
        component.onSubmit();

        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledTimes(1);
        const arg = authServiceSpy.confirmPasswordReset.mock.calls[authServiceSpy.confirmPasswordReset.mock.calls.length - 1][0];
        expect(arg.userId).toBe('user-1');
        expect(arg.token).toBe('tok-abc');
        expect(arg.newPassword).toBe('newPassword123');
    });

    it('should navigate to home on successful submit', () => {
        createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(authResponse));

        component.form.controls.password.setValue('newPassword123');
        component.form.controls.confirmPassword.setValue('newPassword123');
        component.onSubmit();

        expect(navigationServiceSpy.navigateToHomeAsync).toHaveBeenCalled();
        expect(component.isSubmitting()).toBe(false);
    });

    it('should handle submit error', () => {
        createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(throwError(() => new Error('fail')));

        component.form.controls.password.setValue('newPassword123');
        component.form.controls.confirmPassword.setValue('newPassword123');
        component.onSubmit();

        expect(component.state()).toBe('error');
        expect(component.errorMessage()).toBe('AUTH.RESET.ERROR_GENERIC');
        expect(component.isSubmitting()).toBe(false);
    });

    it('should set invalid state when token is missing', () => {
        createComponent({ userId: '', token: '' });
        expect(component.state()).toBe('invalid');
        expect(component.errorMessage()).toBe('AUTH.RESET.INVALID');
    });

    it('should not submit when form is invalid', () => {
        createComponent();
        component.form.controls.password.setValue('');
        component.onSubmit();
        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });

    it('should not submit when already submitting', () => {
        createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(authResponse));

        component.form.controls.password.setValue('newPassword123');
        component.form.controls.confirmPassword.setValue('newPassword123');
        component.isSubmitting.set(true);
        component.onSubmit();

        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });
});

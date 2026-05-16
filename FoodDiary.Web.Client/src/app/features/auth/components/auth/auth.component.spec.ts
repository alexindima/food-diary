import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS, AUTH_PASSWORD_RESET_COOLDOWN_SECONDS } from '../../../../config/runtime-ui.tokens';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { AuthComponent } from './auth.component';
import { AuthFlowFacade } from './auth-lib/auth-flow.facade';
import { AuthFormManager } from './auth-lib/auth-form.manager';
import { AuthGoogleManager } from './auth-lib/auth-google.manager';
import { AUTH_VALIDATION_ERRORS_PROVIDER } from './auth-lib/auth-validation-errors.provider';

type AuthComponentTestContext = {
    authFlowFacadeSpy: {
        login: ReturnType<typeof vi.fn>;
        register: ReturnType<typeof vi.fn>;
        requestPasswordReset: ReturnType<typeof vi.fn>;
        restoreAccount: ReturnType<typeof vi.fn>;
        loginWithGoogle: ReturnType<typeof vi.fn>;
    };
    component: AuthComponent;
    fixture: ComponentFixture<AuthComponent>;
    routerSpy: { navigate: ReturnType<typeof vi.fn> };
};

function createComponent(mode = 'login'): AuthComponentTestContext {
    const authFlowFacadeSpy = {
        login: vi.fn(),
        register: vi.fn(),
        requestPasswordReset: vi.fn(),
        restoreAccount: vi.fn(),
        loginWithGoogle: vi.fn(),
    };
    const authServiceSpy = {
        isAuthenticated: vi.fn().mockReturnValue(false),
        isEmailConfirmed: vi.fn().mockReturnValue(true),
        isAdmin: vi.fn().mockReturnValue(false),
        startAdminSso: vi.fn(),
    };
    const navigationServiceSpy = {
        navigateToEmailVerificationPendingAsync: vi.fn().mockResolvedValue(undefined),
        navigateToReturnUrlAsync: vi.fn().mockResolvedValue(undefined),
    };
    const routerSpy = { navigate: vi.fn().mockResolvedValue(true) };
    const googleManagerSpy = {
        ready: vi.fn().mockReturnValue(false),
        initializeAsync: vi.fn().mockResolvedValue(undefined),
        renderButton: vi.fn(),
    };

    TestBed.configureTestingModule({
        imports: [AuthComponent, TranslateModule.forRoot()],
        providers: [
            { provide: AuthService, useValue: authServiceSpy },
            { provide: AuthFlowFacade, useValue: authFlowFacadeSpy },
            { provide: NavigationService, useValue: navigationServiceSpy },
            { provide: Router, useValue: routerSpy },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        paramMap: convertToParamMap({ mode }),
                        queryParamMap: convertToParamMap({}),
                    },
                },
            },
            { provide: AUTH_PASSWORD_RESET_COOLDOWN_SECONDS, useValue: 2 },
            { provide: AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS, useValue: [] },
        ],
    });
    TestBed.overrideComponent(AuthComponent, {
        set: {
            template: '',
            providers: [AUTH_VALIDATION_ERRORS_PROVIDER, AuthFormManager, { provide: AuthGoogleManager, useValue: googleManagerSpy }],
        },
    });

    const translateService = TestBed.inject(TranslateService);
    vi.spyOn(translateService, 'instant').mockImplementation((key: string | string[]) => (Array.isArray(key) ? key[0] : key));

    const fixture = TestBed.createComponent(AuthComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { authFlowFacadeSpy, component, fixture, routerSpy };
}

beforeEach(() => {
    TestBed.resetTestingModule();
});

describe('AuthComponent tabs', () => {
    it('should reset transient auth state and navigate when mode changes', async () => {
        const { component, routerSpy } = createComponent();
        component.loginForm.controls.email.setValue('user@example.com');
        component.showPasswordReset.set(true);
        component.passwordResetSent.set(true);

        await component.onTabChangeAsync('register');

        expect(component.authMode).toBe('register');
        expect(component.loginForm.controls.email.value).toBe('');
        expect(component.showPasswordReset()).toBe(false);
        expect(component.passwordResetSent()).toBe(false);
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth', 'register']);
    });
});

describe('AuthComponent login', () => {
    it('should show restore action when login returns deleted account', () => {
        const { authFlowFacadeSpy, component } = createComponent();
        authFlowFacadeSpy.login.mockReturnValue(of('accountDeleted'));
        component.loginForm.controls.email.setValue('deleted@example.com');
        component.loginForm.controls.password.setValue('password123');

        component.onLoginSubmit();

        expect(component.globalError()).toBe('AUTH.LOGIN.ACCOUNT_DELETED');
        expect(component.showRestoreAction()).toBe(true);
        expect(component.isSubmitting()).toBe(false);
    });
});

describe('AuthComponent register', () => {
    it('should mark email as existing when register returns conflict', () => {
        const { authFlowFacadeSpy, component } = createComponent('register');
        authFlowFacadeSpy.register.mockReturnValue(of('emailExists'));
        component.registerForm.controls.email.setValue('taken@example.com');
        component.registerForm.controls.password.setValue('password123');
        component.registerForm.controls.confirmPassword.setValue('password123');
        component.registerForm.controls.agreeTerms.setValue(true);

        component.onRegisterSubmit();

        expect(component.registerForm.controls.email.hasError('userExists')).toBe(true);
        expect(component.isSubmitting()).toBe(false);
    });
});

describe('AuthComponent password reset', () => {
    it('should copy login email when opening password reset form', () => {
        const { component } = createComponent();
        component.loginForm.controls.email.setValue('user@example.com');

        component.onPasswordResetOpen();

        expect(component.showPasswordReset()).toBe(true);
        expect(component.passwordResetForm.controls.email.value).toBe('user@example.com');
    });
});

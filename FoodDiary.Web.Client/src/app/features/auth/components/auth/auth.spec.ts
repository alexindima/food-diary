import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { submit } from '@angular/forms/signals';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS, AUTH_PASSWORD_RESET_COOLDOWN_SECONDS } from '../../../../config/runtime-ui.tokens';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { AuthComponent } from './auth';
import { AuthFlowFacade } from './auth-lib/auth-flow.facade';
import { AuthFormManager } from './auth-lib/auth-form.manager';
import { AuthGoogleManager } from './auth-lib/auth-google.manager';

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
    formManager: AuthFormManager;
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
        navigateToEmailVerificationPendingAsync: vi.fn().mockResolvedValue(void 0),
        navigateToReturnUrlAsync: vi.fn().mockResolvedValue(void 0),
    };
    const routerSpy = { navigate: vi.fn().mockResolvedValue(true) };
    const googleManagerSpy = {
        ready: vi.fn().mockReturnValue(false),
        initializeAsync: vi.fn().mockResolvedValue(void 0),
        renderButton: vi.fn(),
    };

    TestBed.configureTestingModule({
        imports: [AuthComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AuthService, useValue: authServiceSpy },
            { provide: AuthFlowFacade, useValue: authFlowFacadeSpy },
            { provide: NavigationService, useValue: navigationServiceSpy },
            { provide: Router, useValue: routerSpy },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        queryParamMap: convertToParamMap({ auth: mode }),
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
            providers: [AuthFormManager, { provide: AuthGoogleManager, useValue: googleManagerSpy }],
        },
    });

    const translateService = TestBed.inject(TranslateService);
    vi.spyOn(translateService, 'instant').mockImplementation((key: string | string[]) => (Array.isArray(key) ? key[0] : key));

    const fixture = TestBed.createComponent(AuthComponent);
    const component = fixture.componentInstance;
    const formManager = fixture.debugElement.injector.get(AuthFormManager);
    fixture.detectChanges();

    return { authFlowFacadeSpy, component, fixture, formManager, routerSpy };
}

beforeEach(() => {
    TestBed.resetTestingModule();
});

describe('AuthComponent tabs', () => {
    it('should reset transient auth state and navigate when mode changes', async () => {
        const { component, routerSpy } = createComponent();
        component['loginModel'].update(value => ({ ...value, email: 'user@example.com' }));
        component['showPasswordReset'].set(true);
        component['passwordResetSent'].set(true);

        await component['onTabChangeAsync']('register');

        expect(component['authMode']).toBe('register');
        expect(component['loginModel']().email).toBe('');
        expect(component['showPasswordReset']()).toBe(false);
        expect(component['passwordResetSent']()).toBe(false);
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/'], { queryParams: { auth: 'register' } });
    });
});

describe('AuthComponent login', () => {
    it('should submit login through the configured signal form action', async () => {
        const { authFlowFacadeSpy, component } = createComponent();
        authFlowFacadeSpy.login.mockReturnValue(of('invalidCredentials'));
        component['loginModel'].set({ email: 'user@example.com', password: 'password123', rememberMe: false });

        await submit(component['loginForm']);

        expect(authFlowFacadeSpy.login).toHaveBeenCalledOnce();
        expect(component['globalError']()).toBe('FORM_ERRORS.INVALID_CREDENTIALS');
    });

    it('should show invalid credentials error and hide restore action', async () => {
        const { authFlowFacadeSpy, component } = createComponent();
        authFlowFacadeSpy.login.mockReturnValue(of('invalidCredentials'));
        component['loginModel'].set({ email: 'user@example.com', password: 'password123', rememberMe: false });

        await submit(component['loginForm']);

        expect(component['globalError']()).toBe('FORM_ERRORS.INVALID_CREDENTIALS');
        expect(component['showRestoreAction']()).toBe(false);
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should show restore action when login returns deleted account', async () => {
        const { authFlowFacadeSpy, component } = createComponent();
        authFlowFacadeSpy.login.mockReturnValue(of('accountDeleted'));
        component['loginModel'].set({ email: 'deleted@example.com', password: 'password123', rememberMe: false });

        await submit(component['loginForm']);

        expect(component['globalError']()).toBe('AUTH.LOGIN.ACCOUNT_DELETED');
        expect(component['showRestoreAction']()).toBe(true);
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should restore deleted account and complete navigation on success', () => {
        const { authFlowFacadeSpy, component } = createComponent();
        authFlowFacadeSpy.restoreAccount.mockReturnValue(of(true));
        component['loginModel'].set({ email: 'deleted@example.com', password: 'password123', rememberMe: false });

        component['onRestoreSubmit']();

        expect(authFlowFacadeSpy.restoreAccount).toHaveBeenCalledOnce();
        expect(component['isRestoring']()).toBe(false);
        expect(component['globalError']()).toBeNull();
    });
});

describe('AuthComponent register', () => {
    it('should submit registration through the configured signal form action', async () => {
        const { authFlowFacadeSpy, component } = createComponent('register');
        authFlowFacadeSpy.register.mockReturnValue(of('emailExists'));
        component['registerModel'].set({
            email: 'taken@example.com',
            password: 'password123',
            confirmPassword: 'password123',
            agreeTerms: true,
        });

        await submit(component['registerForm']);

        expect(authFlowFacadeSpy.register).toHaveBeenCalledOnce();
        expect(component['registerForm'].email().getError('userExists')).toBeDefined();
    });

    it('should not show register field errors while typing before blur or submit', () => {
        const { component, formManager } = createComponent('register');

        component['registerModel'].update(value => ({ ...value, password: 'abc' }));
        component['registerForm'].password().markAsDirty();
        formManager.updateFieldErrors();

        expect(component['registerForm'].password().dirty()).toBe(true);
        expect(component['registerForm'].password().touched()).toBe(false);
        expect(component['registerFieldErrors']().password).toBeNull();
    });

    it('should show register field errors after blur', () => {
        const { component, formManager } = createComponent('register');

        component['registerModel'].update(value => ({ ...value, password: 'abc' }));
        component['registerForm'].password().markAsTouched();
        formManager.updateFieldErrors();

        expect(component['registerFieldErrors']().password).toContain('FORM_ERRORS.PASSWORD.MIN_LENGTH');
    });

    it('should show register field errors after invalid submit', async () => {
        const { authFlowFacadeSpy, component } = createComponent('register');

        await submit(component['registerForm']);

        expect(component['registerForm'].email().touched()).toBe(true);
        expect(component['registerFieldErrors']().email).toContain('FORM_ERRORS.REQUIRED');
        expect(authFlowFacadeSpy.register).not.toHaveBeenCalled();
    });

    it('should mark email as existing when register returns conflict', async () => {
        const { authFlowFacadeSpy, component } = createComponent('register');
        authFlowFacadeSpy.register.mockReturnValue(of('emailExists'));
        component['registerModel'].set({
            email: 'taken@example.com',
            password: 'password123',
            confirmPassword: 'password123',
            agreeTerms: true,
        });

        await submit(component['registerForm']);

        expect(component['registerForm'].email().getError('userExists')).toBeDefined();
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should show deleted-account error when register returns accountDeleted', async () => {
        const { authFlowFacadeSpy, component } = createComponent('register');
        authFlowFacadeSpy.register.mockReturnValue(of('accountDeleted'));
        component['registerModel'].set({
            email: 'deleted@example.com',
            password: 'password123',
            confirmPassword: 'password123',
            agreeTerms: true,
        });

        await submit(component['registerForm']);

        expect(component['globalError']()).toBe('AUTH.REGISTER.ACCOUNT_DELETED');
        expect(component['isSubmitting']()).toBe(false);
    });
});

describe('AuthComponent password reset', () => {
    it('should request password reset through the configured signal form action', async () => {
        const { authFlowFacadeSpy, component } = createComponent();
        authFlowFacadeSpy.requestPasswordReset.mockReturnValue(of(true));
        component['onPasswordResetOpen']();
        component['passwordResetModel'].set({ email: 'user@example.com' });

        await submit(component['passwordResetForm']);

        expect(authFlowFacadeSpy.requestPasswordReset).toHaveBeenCalledOnce();
        expect(component['passwordResetSent']()).toBe(true);
    });

    it('should copy login email when opening password reset form', () => {
        const { component } = createComponent();
        component['loginModel'].update(value => ({ ...value, email: 'user@example.com' }));

        component['onPasswordResetOpen']();

        expect(component['showPasswordReset']()).toBe(true);
        expect(component['passwordResetModel']().email).toBe('user@example.com');
    });

    it('should mark reset as sent and start cooldown after successful request', async () => {
        const { authFlowFacadeSpy, component } = createComponent();
        authFlowFacadeSpy.requestPasswordReset.mockReturnValue(of(true));
        component['onPasswordResetOpen']();
        component['passwordResetModel'].set({ email: 'user@example.com' });

        await submit(component['passwordResetForm']);

        expect(authFlowFacadeSpy.requestPasswordReset).toHaveBeenCalledOnce();
        expect(component['passwordResetSent']()).toBe(true);
        expect(component['passwordResetCooldownSeconds']()).toBe(2);
        expect(component['isPasswordResetting']()).toBe(false);
    });

    it('should not submit password reset during cooldown', async () => {
        const { authFlowFacadeSpy, component } = createComponent();
        component['onPasswordResetOpen']();
        component['passwordResetModel'].set({ email: 'user@example.com' });
        component['passwordResetCooldownSeconds'].set(1);

        await submit(component['passwordResetForm']);

        expect(authFlowFacadeSpy.requestPasswordReset).not.toHaveBeenCalled();
    });
});

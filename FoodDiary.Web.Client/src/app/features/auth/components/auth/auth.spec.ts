import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { type Signal, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { submit } from '@angular/forms/signals';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AUTH_PASSWORD_RESET_COOLDOWN_SECONDS } from '../../../../config/runtime-ui.tokens';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { AuthComponent } from './auth';
import {
    AuthFlowFacade,
    type AuthLoginResult,
    type AuthPasswordResetResult,
    type AuthRegisterResult,
    type GoogleLoginResult,
} from './auth-lib/auth-flow.facade';
import { AuthFormManager } from './auth-lib/auth-form.manager';
import { AuthGoogleManager } from './auth-lib/auth-google.manager';

const PASSWORD_CONTRACT_MAX_LENGTH = 256;

type AuthComponentTestContext = {
    authFlowFacadeSpy: {
        login: ReturnType<typeof vi.fn>;
        register: ReturnType<typeof vi.fn>;
        requestPasswordReset: ReturnType<typeof vi.fn>;
        restoreAccount: ReturnType<typeof vi.fn>;
        loginWithGoogle: ReturnType<typeof vi.fn>;
        linkGoogle: ReturnType<typeof vi.fn>;
    };
    component: AuthComponent;
    fixture: ComponentFixture<AuthComponent>;
    formManager: AuthFormManager;
    authServiceSpy: {
        mustChangePassword: ReturnType<typeof vi.fn>;
        requiresEmailVerification: ReturnType<typeof vi.fn>;
        isAdmin: ReturnType<typeof vi.fn>;
        getUserId: Mock<AuthService['getUserId']>;
        startAdminSso: Mock<AuthService['startAdminSso']>;
    };
    initializeGoogle: Mock<AuthGoogleManager['initializeAsync']>;
    navigationServiceSpy: {
        navigateToEmailVerificationPendingAsync: ReturnType<typeof vi.fn>;
        navigateToRequiredPasswordChangeAsync: ReturnType<typeof vi.fn>;
        navigateToReturnUrlAsync: ReturnType<typeof vi.fn>;
        navigateToProfileAsync: ReturnType<typeof vi.fn>;
    };
    routerSpy: { navigate: ReturnType<typeof vi.fn> };
    dialogRefSpy: { close: ReturnType<typeof vi.fn> };
};

type AuthFixtureOptions = { authenticated?: Signal<boolean>; location?: Pick<Location, 'origin' | 'assign'> };

function createComponent(
    mode = 'login',
    renderTemplate = false,
    inputs: Record<string, unknown> = {},
    { authenticated = signal(false), location }: AuthFixtureOptions = {},
): AuthComponentTestContext {
    const authFlowFacadeSpy = {
        login: vi.fn(),
        register: vi.fn(),
        requestPasswordReset: vi.fn(),
        restoreAccount: vi.fn(),
        loginWithGoogle: vi.fn(),
        linkGoogle: vi.fn(),
    };
    const authServiceSpy = {
        isAuthenticated: vi.fn(authenticated),
        isEmailConfirmed: vi.fn().mockReturnValue(true),
        requiresEmailVerification: vi.fn().mockReturnValue(false),
        mustChangePassword: vi.fn().mockReturnValue(false),
        isAdmin: vi.fn().mockReturnValue(false),
        getUserId: vi.fn<AuthService['getUserId']>(),
        startAdminSso: vi.fn<AuthService['startAdminSso']>(),
    };
    const navigationServiceSpy = {
        navigateToEmailVerificationPendingAsync: vi.fn().mockResolvedValue(void 0),
        navigateToRequiredPasswordChangeAsync: vi.fn().mockResolvedValue(void 0),
        navigateToReturnUrlAsync: vi.fn().mockResolvedValue(void 0),
        navigateToProfileAsync: vi.fn().mockResolvedValue(void 0),
    };
    const routerSpy = { navigate: vi.fn().mockResolvedValue(true) };
    const dialogRefSpy = { close: vi.fn() };
    const initializeGoogle = vi.fn<AuthGoogleManager['initializeAsync']>().mockResolvedValue(undefined);
    const googleManagerSpy = {
        ready: vi.fn().mockReturnValue(false),
        initializeAsync: initializeGoogle,
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
            { provide: FdUiDialogRef, useValue: dialogRefSpy },
            { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ auth: mode }) } } },
            { provide: AUTH_PASSWORD_RESET_COOLDOWN_SECONDS, useValue: 2 },
        ],
    });
    const locationProviders = location === undefined ? [] : [{ provide: DOCUMENT, useValue: { location } }];
    TestBed.overrideComponent(AuthComponent, {
        set: {
            ...(renderTemplate ? {} : { template: '' }),
            providers: [AuthFormManager, { provide: AuthGoogleManager, useValue: googleManagerSpy }, ...locationProviders],
        },
    });

    const translateService = TestBed.inject(TranslateService);
    vi.spyOn(translateService, 'instant').mockImplementation((key: string | string[]) => (Array.isArray(key) ? key[0] : key));

    const fixture = TestBed.createComponent(AuthComponent);
    for (const [name, value] of Object.entries(inputs)) {
        fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();

    return {
        authFlowFacadeSpy,
        authServiceSpy,
        initializeGoogle,
        component: fixture.componentInstance,
        fixture,
        formManager: fixture.debugElement.injector.get(AuthFormManager),
        navigationServiceSpy,
        routerSpy,
        dialogRefSpy,
    };
}

beforeEach(() => {
    TestBed.resetTestingModule();
});

function loginInput(element: HTMLElement, type: 'email' | 'password'): HTMLInputElement {
    const input = element.querySelector<HTMLInputElement>(`input[type="${type}"]`);
    if (input === null) {
        throw new Error(`The rendered login ${type} input is missing.`);
    }
    return input;
}

describe('AuthComponent tabs', () => {
    it('preserves Mini App launch data when opening Telegram authentication', () => {
        const { component, routerSpy, dialogRefSpy } = createComponent();
        component['openTelegram']();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth/telegram'], { preserveFragment: true });
        expect(dialogRefSpy.close).toHaveBeenCalledOnce();
    });

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

describe('AuthComponent rendered login', () => {
    it.each([
        ['invalidCredentials', 'FORM_ERRORS.INVALID_CREDENTIALS'],
        ['rateLimited', 'FORM_ERRORS.RATE_LIMITED'],
        ['accountDeleted', 'AUTH.LOGIN.ACCOUNT_DELETED'],
    ])('keeps a fast %s response visible until credentials are edited', async (result, errorKey) => {
        const { authFlowFacadeSpy, component, fixture } = createComponent('login', true);
        const element = fixture.nativeElement as HTMLElement;
        component['loginModel'].set({ email: 'user@example.com', password: 'password123', rememberMe: false });
        fixture.detectChanges();
        await fixture.whenStable();
        authFlowFacadeSpy.login.mockReturnValue(of(result));

        await submit(component['loginForm']);
        fixture.detectChanges();
        await fixture.whenStable();

        expect(element.querySelector('[role="alert"]')?.textContent).toContain(errorKey);
        expect(component['showRestoreAction']()).toBe(result === 'accountDeleted');

        const emailInput = loginInput(element, 'email');
        emailInput.value = 'edited@example.com';
        emailInput.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        await fixture.whenStable();

        expect(element.querySelector('[role="alert"]')).toBeNull();
        expect(component['showRestoreAction']()).toBe(false);
    });

    it.each([
        ['invalidCredentials', 'FORM_ERRORS.INVALID_CREDENTIALS'],
        ['rateLimited', 'FORM_ERRORS.RATE_LIMITED'],
        ['accountDeleted', 'AUTH.LOGIN.ACCOUNT_DELETED'],
    ])('keeps a fast %s error after native autofill without input events', async (result, errorKey) => {
        const { authFlowFacadeSpy, fixture } = createComponent('login', true);
        const element = fixture.nativeElement as HTMLElement;
        loginInput(element, 'email').value = 'autofilled@example.com';
        loginInput(element, 'password').value = 'autofilled-password123';
        authFlowFacadeSpy.login.mockReturnValue(of(result));

        authButton(element, 'button[type="submit"]', 'AUTH.LOGIN.LOGIN').click();
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.login).toHaveBeenCalledExactlyOnceWith({
                email: 'autofilled@example.com',
                password: 'autofilled-password123',
                rememberMe: false,
            });
        });
        fixture.detectChanges();
        await fixture.whenStable();
        expect(element.querySelector('[role="alert"]')?.textContent).toContain(errorKey);
        loginInput(element, 'email').value = 'edited-autofill@example.com';
        loginInput(element, 'email').dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        await fixture.whenStable();
        expect(element.querySelector('[role="alert"]')).toBeNull();
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

    it('blocks oversized native autofill values before the login API', async () => {
        const { authFlowFacadeSpy, component, fixture } = createComponent('login', true);
        const element = fixture.nativeElement as HTMLElement;
        loginInput(element, 'email').value = 'user@example.test';
        loginInput(element, 'password').value = 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1);

        await submit(component['loginForm']);

        expect(component['loginForm'].password().getError('maxLength')).toBeDefined();
        expect(authFlowFacadeSpy.login).not.toHaveBeenCalled();
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

    it('explains a failed account restoration and keeps the form available for retry', () => {
        const { authFlowFacadeSpy, component, dialogRefSpy } = createComponent();
        authFlowFacadeSpy.restoreAccount.mockReturnValue(of(false));
        const credentials = { email: 'deleted@example.com', password: 'password123', rememberMe: false };
        component['loginModel'].set(credentials);
        component['showRestoreAction'].set(true);

        component['onRestoreSubmit']();

        expect(component['globalError']()).toBe('AUTH.LOGIN.RESTORE_ERROR');
        expect(component['isRestoring']()).toBe(false);
        expect(component['showRestoreAction']()).toBe(true);
        expect(component['loginModel']()).toEqual(credentials);
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('restores the deleted account and closes after successful navigation', async () => {
        const { fixture, authFlowFacadeSpy, navigationServiceSpy, dialogRefSpy, host } = await createDeletedAccountPromptAsync();
        authFlowFacadeSpy.restoreAccount.mockReturnValueOnce(of(true));
        const restore = authButton(host, 'button', 'AUTH.LOGIN.RESTORE');
        restore.click();

        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(dialogRefSpy.close).toHaveBeenCalledOnce();
            expect(restore.disabled).toBe(false);
        });
        expect(authFlowFacadeSpy.restoreAccount).toHaveBeenCalledOnce();
        expect(navigationServiceSpy.navigateToReturnUrlAsync).toHaveBeenCalledExactlyOnceWith(null);
    });

    it('should link the confirmed Google identity after password login and open profile', async () => {
        const { authFlowFacadeSpy, component, navigationServiceSpy } = createComponent();
        authFlowFacadeSpy.login.mockReturnValue(of('success'));
        authFlowFacadeSpy.linkGoogle.mockReturnValue(of(true));
        component['pendingGoogleLinkCredential'] = 'google-credential';
        component['googleLinkRequired'].set(true);
        component['loginModel'].set({ email: 'user@example.com', password: 'password123', rememberMe: false });

        await submit(component['loginForm']);

        expect(authFlowFacadeSpy.linkGoogle).toHaveBeenCalledWith('google-credential');
        expect(navigationServiceSpy.navigateToProfileAsync).toHaveBeenCalledWith({ googleLink: 'success' });
        expect(component['pendingGoogleLinkCredential']).toBeNull();
    });
});

describe('AuthComponent Google login', () => {
    it('should explain the required password login and retain the credential only in memory', async () => {
        const { component } = createComponent('register');

        await component['prepareGoogleLinkLoginAsync']('google-credential');

        expect(component['authMode']).toBe('login');
        expect(component['googleLinkRequired']()).toBe(true);
        expect(component['pendingGoogleLinkCredential']).toBe('google-credential');
        expect(component['globalError']()).toBe('AUTH.GOOGLE.LINK_REQUIRED_ACTION');
    });
});

describe('AuthComponent rendered Google confirmation', () => {
    it.each(['accountMismatch', 'accountLinkRequired', 'error'])('retains a login form after Google returns %s', async result => {
        const account = entityId<'user'>('expected-account');
        const { fixture, authFlowFacadeSpy, initializeGoogle, navigationServiceSpy } = createComponent('login', true, {
            confirmationUserId: account,
        });
        authFlowFacadeSpy.loginWithGoogle.mockReturnValueOnce(of(result));
        const host = fixture.nativeElement as HTMLElement;
        initializeGoogle.mock.calls[0][0]('google-fixture');
        fixture.detectChanges();
        await fixture.whenStable();

        expect(authFlowFacadeSpy.loginWithGoogle).toHaveBeenCalledExactlyOnceWith({
            credential: 'google-fixture',
            expectedUserId: account,
            rememberMe: true,
        });
        const error = result === 'error' ? 'FORM_ERRORS.UNKNOWN' : 'AUTH.CONFIRM.ACCOUNT_MISMATCH';
        expect(host.querySelector('[role="alert"]')?.textContent).toContain(error);
        expect(host.querySelector('form')).not.toBeNull();
        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
    });

    it.each(['password', 'email', 'inline', 'return'])(
        'completes a successful Google sign-in through the %s destination',
        async destination => {
            const inline = destination === 'inline';
            const { fixture, authFlowFacadeSpy, authServiceSpy, initializeGoogle, navigationServiceSpy } = createComponent('login', true, {
                useRouting: false,
                initialReturnUrl: '/profile?tab=security',
                inlineSignIn: inline,
            });
            authServiceSpy.mustChangePassword.mockReturnValue(destination === 'password');
            authServiceSpy.requiresEmailVerification.mockReturnValue(destination === 'email');
            authFlowFacadeSpy.loginWithGoogle.mockReturnValueOnce(of('success'));
            const signedIn = vi.fn<() => void>();
            fixture.componentInstance.signedIn.subscribe(signedIn);
            initializeGoogle.mock.calls[0][0]('google-fixture');
            await fixture.whenStable();
            fixture.detectChanges();

            expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
            expect(navigationServiceSpy.navigateToRequiredPasswordChangeAsync).toHaveBeenCalledTimes(Number(destination === 'password'));
            expect(navigationServiceSpy.navigateToEmailVerificationPendingAsync).toHaveBeenCalledTimes(Number(destination === 'email'));
            expect(signedIn).toHaveBeenCalledTimes(Number(inline));
            expect(navigationServiceSpy.navigateToReturnUrlAsync).toHaveBeenCalledTimes(Number(destination === 'return'));
            if (destination === 'return') {
                expect(navigationServiceSpy.navigateToReturnUrlAsync).toHaveBeenCalledWith('/profile?tab=security');
            }
        },
    );
});

describe('AuthComponent Google results after switching tabs', () => {
    it.each(['unknown', 'accountMismatch'] as const)('keeps a late Google %s on the originating tab', async result => {
        const { fixture, authFlowFacadeSpy, initializeGoogle } = createComponent('login', true);
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<GoogleLoginResult>();
        authFlowFacadeSpy.loginWithGoogle.mockReturnValueOnce(pending);

        initializeGoogle.mock.calls[0][0]('google-fixture');
        expect(authFlowFacadeSpy.loginWithGoogle).toHaveBeenCalledOnce();
        authButton(host, 'button[role="tab"]', 'AUTH.REGISTER.TITLE').click();
        await fixture.whenStable();
        fixture.detectChanges();

        pending.next(result);
        pending.complete();
        fixture.detectChanges();

        expect(host.querySelector('fd-auth-register-form')).not.toBeNull();
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });

    it('keeps a reopened registration when the earlier Google request needs an account link', async () => {
        const { fixture, authFlowFacadeSpy, initializeGoogle, routerSpy } = createComponent('register', true);
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<GoogleLoginResult>();
        authFlowFacadeSpy.loginWithGoogle.mockReturnValueOnce(pending);

        initializeGoogle.mock.calls[0][0]('google-fixture');
        expect(authFlowFacadeSpy.loginWithGoogle).toHaveBeenCalledOnce();
        authButton(host, 'button[role="tab"]', 'AUTH.LOGIN.TITLE').click();
        fixture.detectChanges();
        authButton(host, 'button[role="tab"]', 'AUTH.REGISTER.TITLE').click();
        fixture.detectChanges();
        await fixture.whenStable();

        pending.next('accountLinkRequired');
        pending.complete();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(routerSpy.navigate).toHaveBeenCalledTimes(2);
        expect(host.querySelector('fd-auth-register-form')).not.toBeNull();
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });

    it('still continues a successful Google sign-in after changing tabs', async () => {
        const { fixture, authFlowFacadeSpy, initializeGoogle, navigationServiceSpy } = createComponent('login', true, {
            useRouting: false,
            initialReturnUrl: '/profile?tab=security',
        });
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<GoogleLoginResult>();
        authFlowFacadeSpy.loginWithGoogle.mockReturnValueOnce(pending);

        initializeGoogle.mock.calls[0][0]('google-fixture');
        authButton(host, 'button[role="tab"]', 'AUTH.REGISTER.TITLE').click();
        fixture.detectChanges();
        expect(host.querySelector('fd-auth-register-form')).not.toBeNull();
        pending.next('success');
        pending.complete();
        await vi.waitFor(() => {
            expect(navigationServiceSpy.navigateToReturnUrlAsync).toHaveBeenCalledExactlyOnceWith('/profile?tab=security');
        });
    });
});

describe('Admin handoff after a rendered sign-in', () => {
    it('requests one handoff when the session update and sign-in complete together', async () => {
        const authenticated = signal(false);
        const { fixture, authServiceSpy, authFlowFacadeSpy } = createComponent(
            'login',
            true,
            { useRouting: false, initialAdminReturnUrl: '/users?tab=active' },
            { authenticated },
        );
        const login = new Subject<AuthLoginResult>();
        const handoff = new Subject<never>();
        authServiceSpy.isAdmin.mockReturnValue(true);
        authServiceSpy.startAdminSso.mockReturnValue(handoff);
        authFlowFacadeSpy.login.mockReturnValueOnce(login);

        try {
            submitRenderedLogin(fixture);
            await vi.waitFor(() => {
                expect(authFlowFacadeSpy.login).toHaveBeenCalledOnce();
            });
            expect(authServiceSpy.startAdminSso).not.toHaveBeenCalled();

            authenticated.set(true);
            login.next('success');
            login.complete();
            fixture.detectChanges();
            await waitForAsyncTasksAsync();
            fixture.detectChanges();

            expect(authServiceSpy.startAdminSso).toHaveBeenCalledOnce();
        } finally {
            fixture.destroy();
            handoff.unsubscribe();
        }
    });
});

describe('Admin handoff results from a rendered sign-in', () => {
    const returnPath = '/users?tab=active';
    const account = entityId<'user'>('fixture-admin');

    function createHandoffComponent(): AuthComponentTestContext & { assign: Mock<Location['assign']> } {
        const assign = vi.fn<Location['assign']>();
        const context = createComponent(
            'login',
            true,
            { useRouting: false, initialAdminReturnUrl: returnPath },
            {
                location: { origin: 'https://diary.example.test', assign },
            },
        );
        context.authServiceSpy.isAdmin.mockReturnValue(true);
        context.authServiceSpy.getUserId.mockReturnValue(account);
        context.authFlowFacadeSpy.login.mockReturnValueOnce(of('success'));
        return { ...context, assign };
    }

    it('asks for confirmation of the same account when the admin service requires a fresh sign-in', async () => {
        const { fixture, authServiceSpy, assign, navigationServiceSpy } = createHandoffComponent();
        const error = new HttpErrorResponse({
            status: HttpStatusCode.Unauthorized,
            error: { error: 'Authentication.ReauthenticationRequired' },
        });
        authServiceSpy.startAdminSso.mockReturnValueOnce(throwError(() => error));

        submitRenderedLogin(fixture);
        await vi.waitFor(() => {
            expect(assign).toHaveBeenCalledOnce();
        });

        const destination = new URL(String(assign.mock.lastCall?.[0]));
        expect(destination.origin).toBe(new URL(environment.adminAppUrl ?? '').origin);
        expect(destination.pathname).toBe('/sign-in');
        expect(destination.searchParams.get('returnUrl')).toBe(returnPath);
        expect(destination.searchParams.get('confirm')).toBe('1');
        expect(new URLSearchParams(destination.hash.slice(1)).get('account')).toBe(account);
        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
    });

    it('keeps the active page when a pending admin handoff completes after the dialog closes', async () => {
        const { fixture, authServiceSpy, assign, navigationServiceSpy } = createHandoffComponent();
        const handoff = new Subject<{ code: string; expiresAtUtc: string }>();
        authServiceSpy.startAdminSso.mockReturnValueOnce(handoff);
        submitRenderedLogin(fixture);
        await vi.waitFor(() => {
            expect(authServiceSpy.startAdminSso).toHaveBeenCalledOnce();
        });

        fixture.destroy();
        handoff.next({ code: 'fixture-admin-code', expiresAtUtc: '2026-01-01T00:01:00Z' });
        handoff.complete();
        await waitForAsyncTasksAsync();

        expect(assign).not.toHaveBeenCalled();
        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
    });
});

describe('Rendered navigation after sign-in with an unavailable admin destination', () => {
    it('opens the configured admin home when the requested admin page cannot be parsed', async () => {
        const assign = vi.fn<Location['assign']>();
        const { fixture, authServiceSpy, authFlowFacadeSpy } = createComponent(
            'login',
            true,
            { useRouting: false, initialAdminReturnUrl: 'https://[' },
            { location: { origin: 'https://diary.example.test', assign } },
        );
        const code = 'fixture-handoff';
        authServiceSpy.isAdmin.mockReturnValue(true);
        authServiceSpy.startAdminSso.mockReturnValueOnce(of({ code, expiresAtUtc: '2026-01-01T00:01:00Z' }));
        authFlowFacadeSpy.login.mockReturnValueOnce(of('success'));

        submitRenderedLogin(fixture);
        await vi.waitFor(() => {
            expect(assign).toHaveBeenCalledExactlyOnceWith(new URL(`/#code=${code}`, environment.adminAppUrl).toString());
        });
        expect(authServiceSpy.startAdminSso).toHaveBeenCalledOnce();
    });

    it('continues to the diary after sign-in on a deployment without an admin app', async () => {
        const savedAdminUrl = environment.adminAppUrl;
        const returnUrl = '/diary?view=today';
        const assign = vi.fn<Location['assign']>();
        environment.adminAppUrl = '';
        try {
            const { fixture, authServiceSpy, authFlowFacadeSpy, navigationServiceSpy } = createComponent(
                'login',
                true,
                { useRouting: false, initialReturnUrl: returnUrl, initialAdminReturnUrl: '/users' },
                { location: { origin: 'https://diary.example.test', assign } },
            );
            authServiceSpy.isAdmin.mockReturnValue(true);
            authFlowFacadeSpy.login.mockReturnValueOnce(of('success'));
            submitRenderedLogin(fixture);

            await vi.waitFor(() => {
                expect(navigationServiceSpy.navigateToReturnUrlAsync).toHaveBeenCalledExactlyOnceWith(returnUrl);
            });
            expect(authServiceSpy.startAdminSso).not.toHaveBeenCalled();
            expect(assign).not.toHaveBeenCalled();
        } finally {
            environment.adminAppUrl = savedAdminUrl;
        }
    });
});

describe('AuthComponent register', () => {
    it('submits the rendered registration form and shows an address conflict', async () => {
        const { authFlowFacadeSpy, component, fixture } = createComponent('register', true);
        authFlowFacadeSpy.register.mockReturnValue(of('emailExists'));
        component['registerModel'].set({
            email: 'taken@example.com',
            password: 'password123',
            agreeTerms: true,
        });

        fixture.detectChanges();
        submitAuthForm(fixture.nativeElement as HTMLElement);
        await fixture.whenStable();

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

    it('should require privacy policy acceptance before registration', async () => {
        const { authFlowFacadeSpy, component } = createComponent('register');
        component['registerModel'].set({
            email: 'user@example.com',
            password: 'password123',
            agreeTerms: false,
        });

        await submit(component['registerForm']);

        expect(component['registerForm'].agreeTerms().touched()).toBe(true);
        expect(component['registerFieldErrors']().agreeTerms).toContain('FORM_ERRORS.REQUIRED');
        expect(authFlowFacadeSpy.register).not.toHaveBeenCalled();
    });
});

describe('AuthComponent register results', () => {
    it('should mark email as existing when register returns conflict', async () => {
        const { authFlowFacadeSpy, component } = createComponent('register');
        authFlowFacadeSpy.register.mockReturnValue(of('emailExists'));
        component['registerModel'].set({
            email: 'taken@example.com',
            password: 'password123',
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
            agreeTerms: true,
        });

        await submit(component['registerForm']);

        expect(component['globalError']()).toBe('AUTH.REGISTER.ACCOUNT_DELETED');
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should navigate to email verification and close dialog after successful registration', async () => {
        const { authFlowFacadeSpy, component, dialogRefSpy, navigationServiceSpy } = createComponent('register');
        authFlowFacadeSpy.register.mockReturnValue(of('success'));
        component['registerModel'].set({
            email: 'new@example.com',
            password: 'password123',
            agreeTerms: true,
        });

        await submit(component['registerForm']);

        expect(authFlowFacadeSpy.register).toHaveBeenCalledOnce();
        expect(navigationServiceSpy.navigateToEmailVerificationPendingAsync).toHaveBeenCalledOnce();
        expect(dialogRefSpy.close).toHaveBeenCalledOnce();
        expect(component['globalError']()).toBeNull();
    });
});

describe('AuthComponent password reset', () => {
    it('clears a failed request before retry and keeps the successful confirmation free of the old error', async () => {
        const { authFlowFacadeSpy, component, fixture } = createComponent();
        const retry = new Subject<'success'>();
        authFlowFacadeSpy.requestPasswordReset.mockReturnValueOnce(of('unknown')).mockReturnValueOnce(retry.asObservable());
        component['onPasswordResetOpen']();
        component['passwordResetModel'].set({ email: 'user@example.test' });
        fixture.detectChanges();
        await component['requestPasswordResetAsync']();
        expect(component['globalError']()).toBe('FORM_ERRORS.UNKNOWN');

        const pending = component['requestPasswordResetAsync']();
        expect(component['globalError']()).toBeNull();
        expect(component['isPasswordResetting']()).toBe(true);
        retry.next('success');
        retry.complete();
        await pending;

        expect(component['passwordResetSent']()).toBe(true);
        expect(component['globalError']()).toBeNull();
    });

    it('submits the rendered password-reset form and announces that the email was sent', async () => {
        const { authFlowFacadeSpy, component, fixture } = createComponent('login', true);
        authFlowFacadeSpy.requestPasswordReset.mockReturnValue(of('success'));
        const host = fixture.nativeElement as HTMLElement;
        authButton(host, 'button[type="button"]', 'AUTH.LOGIN.FORGOT_PASSWORD').click();
        fixture.detectChanges();
        const email = loginInput(host, 'email');
        email.value = 'user@example.com';
        email.dispatchEvent(new Event('input', { bubbles: true }));

        submitAuthForm(host);
        await fixture.whenStable();
        fixture.detectChanges();

        expect(authFlowFacadeSpy.requestPasswordReset).toHaveBeenCalledExactlyOnceWith({ email: 'user@example.com' });
        expect(component['passwordResetSent']()).toBe(true);
        expect(host.querySelector('[role="status"]')?.textContent).toContain('AUTH.RESET.SENT');
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
        authFlowFacadeSpy.requestPasswordReset.mockReturnValue(of('success'));
        component['onPasswordResetOpen']();
        component['passwordResetModel'].set({ email: 'user@example.com' });

        await submit(component['passwordResetForm']);

        expect(authFlowFacadeSpy.requestPasswordReset).toHaveBeenCalledOnce();
        expect(component['passwordResetSent']()).toBe(true);
        expect(component['passwordResetCooldownSeconds']()).toBe(2);
        expect(component['isPasswordResetting']()).toBe(false);
    });

    it('should explain throttling and preserve the reset email without starting a success cooldown', async () => {
        const { authFlowFacadeSpy, component } = createComponent();
        authFlowFacadeSpy.requestPasswordReset.mockReturnValue(of('rateLimited'));
        component['onPasswordResetOpen']();
        component['passwordResetModel'].set({ email: 'user@example.com' });

        await submit(component['passwordResetForm']);

        expect(component['globalError']()).toBe('FORM_ERRORS.RATE_LIMITED');
        expect(component['passwordResetModel']().email).toBe('user@example.com');
        expect(component['passwordResetSent']()).toBe(false);
        expect(component['passwordResetCooldownSeconds']()).toBe(0);
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

describe('rendered auth form ownership', () => {
    it('follows the password link flow from Google and reports a rejected link on the profile', async () => {
        const { fixture, authFlowFacadeSpy, initializeGoogle, navigationServiceSpy, dialogRefSpy } = createComponent('register', true, {
            initialMode: 'register',
            useRouting: false,
        });
        authFlowFacadeSpy.loginWithGoogle.mockReturnValueOnce(of('accountLinkRequired'));
        authFlowFacadeSpy.login.mockReturnValueOnce(of('success'));
        authFlowFacadeSpy.linkGoogle.mockReturnValueOnce(of(false));

        initializeGoogle.mock.calls[0][0]('google-fixture');
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
                'AUTH.GOOGLE.LINK_REQUIRED_ACTION',
            );
        });
        submitRenderedLogin(fixture);

        await vi.waitFor(() => {
            expect(navigationServiceSpy.navigateToProfileAsync).toHaveBeenCalledExactlyOnceWith({ googleLink: 'failed' });
        });
        expect(authFlowFacadeSpy.linkGoogle).toHaveBeenCalledExactlyOnceWith('google-fixture');
        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).toHaveBeenCalledOnce();
    });

    it('leaves navigation to the current page when a password login completes after the form closes', async () => {
        const { fixture, authFlowFacadeSpy, navigationServiceSpy, dialogRefSpy } = createComponent('login', true, {
            useRouting: false,
            initialReturnUrl: '/profile',
        });
        const login = new Subject<AuthLoginResult>();
        authFlowFacadeSpy.login.mockReturnValueOnce(login);
        submitRenderedLogin(fixture);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.login).toHaveBeenCalledOnce();
        });

        fixture.destroy();
        login.next('success');
        login.complete();
        await waitForAsyncTasksAsync();

        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('leaves navigation to the current page when Google sign-in completes after the form closes', async () => {
        const { fixture, authFlowFacadeSpy, initializeGoogle, navigationServiceSpy, dialogRefSpy } = createComponent('login', true, {
            useRouting: false,
            initialReturnUrl: '/profile',
        });
        const login = new Subject<GoogleLoginResult>();
        authFlowFacadeSpy.loginWithGoogle.mockReturnValueOnce(login);
        initializeGoogle.mock.calls[0][0]('google-fixture');
        expect(authFlowFacadeSpy.loginWithGoogle).toHaveBeenCalledOnce();

        fixture.destroy();
        login.next('success');
        login.complete();
        await waitForAsyncTasksAsync();

        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });
});

describe('AuthComponent native navigation controls', () => {
    it('returns from password reset to login with the entered address and no reset error', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('login', true);
        const host = fixture.nativeElement as HTMLElement;
        const email = 'return-to-sign-in@example.test';
        enterAuthEmail(fixture, email);
        authFlowFacadeSpy.requestPasswordReset.mockReturnValueOnce(of('rateLimited'));

        authButton(host, 'button', 'AUTH.LOGIN.FORGOT_PASSWORD').click();
        fixture.detectChanges();
        expect(loginInput(host, 'email').value).toBe(email);
        submitAuthForm(host);
        await waitForAsyncTasksAsync();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.RATE_LIMITED');

        authButton(host, 'button', 'AUTH.RESET.BACK').click();
        fixture.detectChanges();
        expect(host.querySelector('fd-auth-login-form')).not.toBeNull();
        expect(loginInput(host, 'email').value).toBe(email);
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });

    it('switches the shipped tab to registration and surfaces a throttled native form submission', async () => {
        const { fixture, authFlowFacadeSpy, routerSpy } = createComponent('login', true);
        const host = fixture.nativeElement as HTMLElement;
        authButton(host, 'button[role="tab"]', 'AUTH.REGISTER.TITLE').click();
        fixture.detectChanges();
        expect(host.querySelector('fd-auth-register-form')).not.toBeNull();
        expect(routerSpy.navigate).toHaveBeenCalledExactlyOnceWith(['/'], { queryParams: { auth: 'register' } });
        authFlowFacadeSpy.register.mockReturnValueOnce(of('rateLimited'));

        fillRenderedRegistration(fixture);
        submitAuthForm(host);
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(authFlowFacadeSpy.register).toHaveBeenCalledOnce();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.RATE_LIMITED');
    });
});

describe('Native account restoration and sign-in ownership', () => {
    it('keeps sign-in unavailable while restoring and allows sign-in after the server rejects restoration', async () => {
        const { fixture, authFlowFacadeSpy, host } = await createDeletedAccountPromptAsync();
        const pendingRestore = new Subject<boolean>();
        authFlowFacadeSpy.restoreAccount.mockReturnValueOnce(pendingRestore);
        authFlowFacadeSpy.login.mockReturnValue(of('invalidCredentials'));
        const restore = authButton(host, 'button', 'AUTH.LOGIN.RESTORE');
        const signIn = authButton(host, 'button[type="submit"]', 'AUTH.LOGIN.LOGIN');

        restore.click();
        restore.click();
        fixture.detectChanges();
        signIn.click();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(authFlowFacadeSpy.login).toHaveBeenCalledOnce();
        expect(authFlowFacadeSpy.restoreAccount).toHaveBeenCalledOnce();
        expect(signIn.disabled).toBe(true);
        expect(restore.disabled).toBe(true);
        expect(host.querySelector('form')?.getAttribute('aria-busy')).toBe('true');

        pendingRestore.next(false);
        pendingRestore.complete();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('AUTH.LOGIN.RESTORE_ERROR');
        expect(signIn.disabled).toBe(false);
        expect(restore.disabled).toBe(false);

        signIn.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.INVALID_CREDENTIALS');
        });
        expect(authFlowFacadeSpy.login).toHaveBeenCalledTimes(2);
    });

    it('waits for the destination after successful restoration and can sign in again after that navigation fails', async () => {
        const { fixture, authFlowFacadeSpy, navigationServiceSpy, dialogRefSpy, host } = await createDeletedAccountPromptAsync();
        const pendingDestination = new Subject<void>();
        navigationServiceSpy.navigateToReturnUrlAsync.mockReturnValueOnce(firstValueFrom(pendingDestination));
        authFlowFacadeSpy.restoreAccount.mockReturnValueOnce(of(true)).mockReturnValue(of(false));
        authFlowFacadeSpy.login.mockReturnValue(of('success'));
        const restore = authButton(host, 'button', 'AUTH.LOGIN.RESTORE');
        const signIn = authButton(host, 'button[type="submit"]', 'AUTH.LOGIN.LOGIN');

        restore.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(navigationServiceSpy.navigateToReturnUrlAsync).toHaveBeenCalledOnce();
        });
        restore.click();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(authFlowFacadeSpy.restoreAccount).toHaveBeenCalledOnce();
        expect(authFlowFacadeSpy.login).toHaveBeenCalledOnce();
        expect(restore.disabled).toBe(true);
        expect(signIn.disabled).toBe(true);
        expect(dialogRefSpy.close).not.toHaveBeenCalled();

        pendingDestination.error(new Error('Fixture route unavailable'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.UNKNOWN');
        });
        expect(signIn.disabled).toBe(false);
        expect(dialogRefSpy.close).not.toHaveBeenCalled();

        signIn.click();
        await vi.waitFor(() => {
            expect(dialogRefSpy.close).toHaveBeenCalledOnce();
        });
        expect(navigationServiceSpy.navigateToReturnUrlAsync).toHaveBeenCalledTimes(2);
    });
});

describe('Account restoration after browser autofill', () => {
    it.each(['sign-in', 'restore'])('does not reuse a silently cleared native password for %s', async action => {
        const { fixture, authFlowFacadeSpy, host } = await createDeletedAccountPromptAsync();
        authFlowFacadeSpy.login.mockReturnValue(of('invalidCredentials'));
        authFlowFacadeSpy.restoreAccount.mockReturnValue(of(false));
        const password = loginInput(host, 'password');
        password.value = '';

        const button =
            action === 'sign-in'
                ? authButton(host, 'button[type="submit"]', 'AUTH.LOGIN.LOGIN')
                : authButton(host, 'button', 'AUTH.LOGIN.RESTORE');
        button.click();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(authFlowFacadeSpy.login).toHaveBeenCalledOnce();
        expect(authFlowFacadeSpy.restoreAccount).not.toHaveBeenCalled();
        expect(password.value).toBe('');
        expect(host.textContent).not.toContain('AUTH.LOGIN.ACCOUNT_DELETED');
    });

    it('requires a fresh sign-in when the browser replaces the displayed account before restoration', async () => {
        const { fixture, authFlowFacadeSpy, host } = await createDeletedAccountPromptAsync();
        authFlowFacadeSpy.restoreAccount.mockReturnValue(of(false));
        const email = 'another-autofilled-account@example.test';
        const password = 'another-autofilled-password';
        loginInput(host, 'email').value = email;
        loginInput(host, 'password').value = password;

        authButton(host, 'button', 'AUTH.LOGIN.RESTORE').click();
        expect(authFlowFacadeSpy.restoreAccount).not.toHaveBeenCalled();
        fixture.detectChanges();
        expect(host.textContent).not.toContain('AUTH.LOGIN.RESTORE');

        authFlowFacadeSpy.login.mockReturnValueOnce(of('accountDeleted'));
        authButton(host, 'button[type="submit"]', 'AUTH.LOGIN.LOGIN').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('AUTH.LOGIN.ACCOUNT_DELETED');
        });
        expect(authFlowFacadeSpy.login).toHaveBeenCalledTimes(2);
        expect(authFlowFacadeSpy.login).toHaveBeenLastCalledWith(expect.objectContaining({ email, password }));
        authButton(host, 'button', 'AUTH.LOGIN.RESTORE').click();
        expect(authFlowFacadeSpy.restoreAccount).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ email, password }), false);
    });
});

async function createDeletedAccountPromptAsync(): Promise<AuthComponentTestContext & { host: HTMLElement }> {
    const context = createComponent('login', true);
    const { fixture, authFlowFacadeSpy } = context;
    const host = fixture.nativeElement as HTMLElement;
    authFlowFacadeSpy.login.mockReturnValueOnce(of('accountDeleted'));
    submitRenderedLogin(fixture);
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('AUTH.LOGIN.ACCOUNT_DELETED');
    });
    return { ...context, host };
}

describe('AuthComponent pending form results after destruction', () => {
    it('leaves navigation to the current view when registration finishes after the old form closes', async () => {
        const { fixture, authFlowFacadeSpy, navigationServiceSpy, dialogRefSpy } = createComponent('register', true);
        const pending = new Subject<AuthRegisterResult>();
        authFlowFacadeSpy.register.mockReturnValueOnce(pending);
        fillRenderedRegistration(fixture);
        submitAuthForm(fixture.nativeElement as HTMLElement);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.register).toHaveBeenCalledOnce();
        });

        fixture.destroy();
        pending.next('success');
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(navigationServiceSpy.navigateToEmailVerificationPendingAsync).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('does not navigate from the old account restoration prompt after it closes', async () => {
        const { fixture, authFlowFacadeSpy, navigationServiceSpy, dialogRefSpy } = createComponent('login', true);
        const pending = new Subject<boolean>();
        authFlowFacadeSpy.login.mockReturnValueOnce(of('accountDeleted'));
        authFlowFacadeSpy.restoreAccount.mockReturnValueOnce(pending);
        submitRenderedLogin(fixture);
        await waitForAsyncTasksAsync();
        fixture.detectChanges();
        authButton(fixture.nativeElement as HTMLElement, 'button', 'AUTH.LOGIN.RESTORE').click();
        expect(authFlowFacadeSpy.restoreAccount).toHaveBeenCalledOnce();

        fixture.destroy();
        pending.next(true);
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('does not start a countdown when a password reset response arrives after the form closes', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('login', true, { initialPasswordReset: true });
        const pending = new Subject<AuthPasswordResetResult>();
        authFlowFacadeSpy.requestPasswordReset.mockReturnValueOnce(pending);
        enterAuthEmail(fixture, 'reset@example.test');
        submitAuthForm(fixture.nativeElement as HTMLElement);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.requestPasswordReset).toHaveBeenCalledOnce();
        });
        const startInterval = vi.spyOn(window, 'setInterval').mockReturnValue(0);

        try {
            fixture.destroy();
            pending.next('success');
            pending.complete();
            await waitForAsyncTasksAsync();

            expect(startInterval).not.toHaveBeenCalled();
        } finally {
            startInterval.mockRestore();
        }
    });
});

describe('Authentication errors after switching tabs', () => {
    it('keeps the registration panel free of a previous sign-in error', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('login', true);
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<AuthLoginResult>();
        authFlowFacadeSpy.login.mockReturnValueOnce(pending);
        submitRenderedLogin(fixture);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.login).toHaveBeenCalledOnce();
        });

        authButton(host, 'button[role="tab"]', 'AUTH.REGISTER.TITLE').click();
        fixture.detectChanges();
        pending.next('invalidCredentials');
        pending.complete();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(host.querySelector('fd-auth-register-form')).not.toBeNull();
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });

    it('keeps a freshly reopened registration clear of an earlier rate limit message', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('register', true);
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<AuthRegisterResult>();
        authFlowFacadeSpy.register.mockReturnValueOnce(pending);
        fillRenderedRegistration(fixture);
        submitAuthForm(host);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.register).toHaveBeenCalledOnce();
        });

        authButton(host, 'button[role="tab"]', 'AUTH.LOGIN.TITLE').click();
        fixture.detectChanges();
        authButton(host, 'button[role="tab"]', 'AUTH.REGISTER.TITLE').click();
        fixture.detectChanges();
        pending.next('rateLimited');
        pending.complete();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(host.querySelector('fd-auth-register-form')).not.toBeNull();
        expect(loginInput(host, 'email').value).toBe('');
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });

    it('keeps the registration panel clear when a previous account restoration fails', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('login', true);
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<boolean>();
        authFlowFacadeSpy.login.mockReturnValueOnce(of('accountDeleted'));
        authFlowFacadeSpy.restoreAccount.mockReturnValueOnce(pending);
        submitRenderedLogin(fixture);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('AUTH.LOGIN.ACCOUNT_DELETED');
        });
        authButton(host, 'button', 'AUTH.LOGIN.RESTORE').click();
        expect(authFlowFacadeSpy.restoreAccount).toHaveBeenCalledOnce();

        authButton(host, 'button[role="tab"]', 'AUTH.REGISTER.TITLE').click();
        fixture.detectChanges();
        pending.next(false);
        pending.complete();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(host.querySelector('fd-auth-register-form')).not.toBeNull();
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });
});

describe('Rendered authentication with a completed server response', () => {
    it.each([
        { outcome: 'accountMismatch', error: 'AUTH.CONFIRM.ACCOUNT_MISMATCH' },
        { outcome: 'accountDeleted', error: 'AUTH.LOGIN.ACCOUNT_DELETED' },
        { outcome: 'unknown', error: 'FORM_ERRORS.UNKNOWN' },
    ] satisfies Array<{ outcome: AuthLoginResult; error: string }>)(
        'shows $outcome when confirming a specific account without offering restoration',
        async ({ outcome, error }) => {
            const account = entityId<'user'>('expected-account');
            const { fixture, authFlowFacadeSpy, navigationServiceSpy } = createComponent('login', true, {
                confirmationUserId: account,
            });
            const host = fixture.nativeElement as HTMLElement;
            authFlowFacadeSpy.login.mockReturnValueOnce(of(outcome));
            submitRenderedLogin(fixture);
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(host.querySelector('[role="alert"]')?.textContent).toContain(error);
            });

            expect(authFlowFacadeSpy.login).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({ email: 'user@example.test', expectedUserId: account }),
            );
            expect(host.textContent).not.toContain('AUTH.LOGIN.RESTORE');
            expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
        },
    );

    it('keeps registration details available to retry an unexpected failure', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('register', true);
        const host = fixture.nativeElement as HTMLElement;
        authFlowFacadeSpy.register.mockReturnValueOnce(of('unknown'));
        fillRenderedRegistration(fixture);
        submitAuthForm(host);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.UNKNOWN');
        });

        expect(loginInput(host, 'email').value).toBe('register@example.test');
        expect(host.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(false);
    });
});

describe('Completed registration during a tab change', () => {
    it('continues to verification after the server creates the account', async () => {
        const { fixture, authFlowFacadeSpy, navigationServiceSpy } = createComponent('register', true);
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<AuthRegisterResult>();
        authFlowFacadeSpy.register.mockReturnValueOnce(pending);
        fillRenderedRegistration(fixture);
        submitAuthForm(host);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.register).toHaveBeenCalledOnce();
        });

        authButton(host, 'button[role="tab"]', 'AUTH.LOGIN.TITLE').click();
        fixture.detectChanges();
        expect(host.querySelector('fd-auth-login-form')).not.toBeNull();
        pending.next('success');
        pending.complete();

        await vi.waitFor(() => {
            expect(navigationServiceSpy.navigateToEmailVerificationPendingAsync).toHaveBeenCalledOnce();
        });
    });
});

describe('AuthComponent additional request ownership', () => {
    it.each([true, false])('does not open the profile when account linking finishes after dismissal (success=%s)', async linked => {
        const { fixture, authFlowFacadeSpy, initializeGoogle, navigationServiceSpy, dialogRefSpy } = createComponent('login', true, {
            useRouting: false,
        });
        const pending = new Subject<boolean>();
        authFlowFacadeSpy.loginWithGoogle.mockReturnValueOnce(of('accountLinkRequired'));
        authFlowFacadeSpy.login.mockReturnValueOnce(of('success'));
        authFlowFacadeSpy.linkGoogle.mockReturnValueOnce(pending);
        initializeGoogle.mock.calls[0][0]('google-fixture');
        fixture.detectChanges();

        submitRenderedLogin(fixture);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.linkGoogle).toHaveBeenCalledExactlyOnceWith('google-fixture');
        });
        fixture.destroy();
        pending.next(linked);
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(navigationServiceSpy.navigateToProfileAsync).not.toHaveBeenCalled();
        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('does not display a late reset error on the sign-in form after returning from password reset', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('login', true);
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<AuthPasswordResetResult>();
        authFlowFacadeSpy.requestPasswordReset.mockReturnValueOnce(pending);
        enterAuthEmail(fixture, 'sign-in@example.test');
        authButton(host, 'button', 'AUTH.LOGIN.FORGOT_PASSWORD').click();
        fixture.detectChanges();
        submitAuthForm(host);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.requestPasswordReset).toHaveBeenCalledOnce();
        });

        authButton(host, 'button', 'AUTH.RESET.BACK').click();
        fixture.detectChanges();
        pending.next('rateLimited');
        pending.complete();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(host.querySelector('fd-auth-login-form')).not.toBeNull();
        expect(loginInput(host, 'email').value).toBe('sign-in@example.test');
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });

    it('keeps the accepted request cooldown without showing confirmation for a newly opened reset form', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('login', true);
        const host = fixture.nativeElement as HTMLElement;
        const pending = new Subject<AuthPasswordResetResult>();
        authFlowFacadeSpy.requestPasswordReset.mockReturnValueOnce(pending);
        enterAuthEmail(fixture, 'original@example.test');
        authButton(host, 'button', 'AUTH.LOGIN.FORGOT_PASSWORD').click();
        fixture.detectChanges();
        submitAuthForm(host);
        await vi.waitFor(() => {
            expect(authFlowFacadeSpy.requestPasswordReset).toHaveBeenCalledOnce();
        });

        authButton(host, 'button', 'AUTH.RESET.BACK').click();
        fixture.detectChanges();
        enterAuthEmail(fixture, 'another@example.test');
        authButton(host, 'button', 'AUTH.LOGIN.FORGOT_PASSWORD').click();
        fixture.detectChanges();
        pending.next('success');
        pending.complete();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(loginInput(host, 'email').value).toBe('another@example.test');
        expect(host.textContent).not.toContain('AUTH.RESET.SENT');
        expect(host.textContent).toContain('AUTH.RESET.RESEND_COOLDOWN');
    });
});

describe('Google response during a submitted password form', () => {
    it.each(['login', 'register'] as const)('keeps the pending %s in control and allows Google afterward', async mode => {
        const { fixture, authFlowFacadeSpy, initializeGoogle, navigationServiceSpy } = createComponent(mode, true, {
            useRouting: false,
            initialMode: mode,
        });
        const host = fixture.nativeElement as HTMLElement;
        const submitted = authFlowFacadeSpy[mode];
        const pending = new Subject<'unknown'>();
        submitted.mockReturnValueOnce(pending);
        authFlowFacadeSpy.loginWithGoogle.mockReturnValue(of('unknown'));

        if (mode === 'login') {
            submitRenderedLogin(fixture);
        } else {
            fillRenderedRegistration(fixture);
            submitAuthForm(host);
        }
        fixture.detectChanges();
        expect(submitted).toHaveBeenCalledOnce();
        const submitButton = host.querySelector<HTMLButtonElement>('button[type="submit"]');
        expect(submitButton?.disabled).toBe(true);

        const receiveGoogleCredential = initializeGoogle.mock.calls[0][0];
        receiveGoogleCredential('google-fixture-while-busy');
        fixture.detectChanges();
        expect(submitButton?.disabled).toBe(true);
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(authFlowFacadeSpy.loginWithGoogle).not.toHaveBeenCalled();

        pending.next('unknown');
        pending.complete();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(submitButton?.disabled).toBe(false);
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.UNKNOWN');
        });

        receiveGoogleCredential('google-fixture-after-request');
        fixture.detectChanges();
        expect(authFlowFacadeSpy.loginWithGoogle).toHaveBeenCalledExactlyOnceWith({
            credential: 'google-fixture-after-request',
            expectedUserId: undefined,
            rememberMe: true,
        });
        expect(navigationServiceSpy.navigateToReturnUrlAsync).not.toHaveBeenCalled();
    });
});

describe('Reselecting the active authentication tab', () => {
    it.each([
        { mode: 'login', label: 'AUTH.LOGIN.TITLE' },
        { mode: 'register', label: 'AUTH.REGISTER.TITLE' },
    ] as const)('preserves the draft in the active $mode tab', ({ mode, label }) => {
        const { fixture, authFlowFacadeSpy, routerSpy } = createComponent(mode, true, { useRouting: false, initialMode: mode });
        const host = fixture.nativeElement as HTMLElement;
        const tab = authButton(host, 'button[role="tab"]', label);
        enterAuthEmail(fixture, 'draft@example.test');
        expect(tab.getAttribute('aria-selected')).toBe('true');

        tab.click();
        fixture.detectChanges();

        expect(tab.getAttribute('aria-selected')).toBe('true');
        expect(loginInput(host, 'email').value).toBe('draft@example.test');
        expect(authFlowFacadeSpy[mode]).not.toHaveBeenCalled();
        expect(routerSpy.navigate).not.toHaveBeenCalled();
    });
});

describe('Rendered password reset validation and retry', () => {
    it('focuses an invalid address and sends only one request at a time through a failed retry', async () => {
        const { fixture, authFlowFacadeSpy } = createComponent('login', true, { useRouting: false });
        const host = fixture.nativeElement as HTMLElement;
        authButton(host, 'button', 'AUTH.LOGIN.FORGOT_PASSWORD').click();
        fixture.detectChanges();
        const email = loginInput(host, 'email');
        const send = authButton(host, 'button[type="submit"]', 'AUTH.RESET.SEND');
        enterAuthEmail(fixture, 'missing-at-symbol');
        send.click();
        fixture.detectChanges();
        expect(email.getAttribute('aria-invalid')).toBe('true');
        expect(document.activeElement).toBe(email);
        expect(authFlowFacadeSpy.requestPasswordReset).not.toHaveBeenCalled();

        const pending = new Subject<AuthPasswordResetResult>();
        authFlowFacadeSpy.requestPasswordReset.mockReturnValueOnce(pending).mockReturnValueOnce(of('success'));
        enterAuthEmail(fixture, 'reader@example.test');
        send.click();
        fixture.detectChanges();
        expect(send.disabled).toBe(true);
        send.click();
        expect(authFlowFacadeSpy.requestPasswordReset).toHaveBeenCalledExactlyOnceWith({ email: 'reader@example.test' });

        pending.next('unknown');
        pending.complete();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(send.disabled).toBe(false);
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.UNKNOWN');
        });
        expect(email.value).toBe('reader@example.test');
        send.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.textContent).toContain('AUTH.RESET.SENT');
        });
        expect(authFlowFacadeSpy.requestPasswordReset.mock.calls).toEqual([
            [{ email: 'reader@example.test' }],
            [{ email: 'reader@example.test' }],
        ]);
        expect(send.disabled).toBe(true);
        expect(host.querySelector('[role="alert"]')).toBeNull();
    });
});

function fillRenderedRegistration(fixture: ComponentFixture<AuthComponent>): void {
    const host = fixture.nativeElement as HTMLElement;
    enterAuthEmail(fixture, 'register@example.test');
    const password = loginInput(host, 'password');
    password.value = 'fixture-registration-password';
    password.dispatchEvent(new Event('input', { bubbles: true }));
    const consent = host.querySelector<HTMLInputElement>('input[type="checkbox"]');
    if (consent === null) {
        throw new Error('Expected the rendered registration consent control');
    }
    consent.click();
    fixture.detectChanges();
}

function enterAuthEmail(fixture: ComponentFixture<AuthComponent>, value: string): void {
    const email = loginInput(fixture.nativeElement as HTMLElement, 'email');
    email.value = value;
    email.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
}

function authButton(host: HTMLElement, selector: string, label: string): HTMLButtonElement {
    const control = [...host.querySelectorAll<HTMLButtonElement>(selector)].find(button => button.textContent.trim() === label);
    if (control === undefined) {
        throw new Error(`Expected the rendered auth action ${label}`);
    }
    return control;
}

function submitAuthForm(host: HTMLElement): void {
    const form = host.querySelector('form');
    if (form === null) {
        throw new Error('Expected a rendered auth form');
    }
    form.requestSubmit();
}

function submitRenderedLogin(fixture: ComponentFixture<AuthComponent>): void {
    const host = fixture.nativeElement as HTMLElement;
    const email = loginInput(host, 'email');
    const password = loginInput(host, 'password');
    email.value = 'user@example.test';
    email.dispatchEvent(new Event('input', { bubbles: true }));
    password.value = 'fixture-password';
    password.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    const form = host.querySelector('form');
    if (form === null) {
        throw new Error('Expected the rendered sign-in form');
    }
    form.requestSubmit();
}

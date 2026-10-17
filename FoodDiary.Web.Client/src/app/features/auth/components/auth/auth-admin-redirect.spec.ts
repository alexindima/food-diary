import { TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS } from '../../../../config/runtime-ui.tokens';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { AuthComponent } from './auth';
import { AuthFlowFacade } from './auth-lib/auth-flow.facade';
import { AuthFormManager } from './auth-lib/auth-form.manager';
import { AuthGoogleManager } from './auth-lib/auth-google.manager';

const adminAppUrl = environment.adminAppUrl ?? '';

function createComponent(): {
    component: AuthComponent;
    authService: {
        isAuthenticated: ReturnType<typeof vi.fn>;
        isAdmin: ReturnType<typeof vi.fn>;
        startAdminSso: ReturnType<typeof vi.fn>;
    };
} {
    const authService = {
        isAuthenticated: vi.fn().mockReturnValue(false),
        isAdmin: vi.fn().mockReturnValue(true),
        startAdminSso: vi.fn().mockReturnValue(of({ code: 'fresh-admin-code' })),
    };
    TestBed.configureTestingModule({
        imports: [AuthComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AuthService, useValue: authService },
            { provide: AuthFlowFacade, useValue: {} },
            { provide: NavigationService, useValue: {} },
            { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
            { provide: AUTH_LOGIN_AUTOFILL_CHECK_DELAYS_MS, useValue: [] },
        ],
    });
    TestBed.overrideComponent(AuthComponent, {
        set: {
            template: '',
            providers: [
                AuthFormManager,
                {
                    provide: AuthGoogleManager,
                    useValue: {
                        ready: vi.fn().mockReturnValue(false),
                        initializeAsync: vi.fn().mockResolvedValue(void 0),
                        renderButton: vi.fn(),
                    },
                },
            ],
        },
    });
    const fixture = TestBed.createComponent(AuthComponent);
    fixture.detectChanges();
    return { component: fixture.componentInstance, authService };
}

beforeEach(() => {
    TestBed.resetTestingModule();
});
afterEach(() => {
    vi.restoreAllMocks();
});

describe('AuthComponent admin SSO destination', () => {
    it.each([
        `${adminAppUrl}//attacker.example/capture`,
        `${adminAppUrl}/.//attacker.example/capture`,
        `${adminAppUrl}/%2f%2fattacker.example/capture`,
        encodeURIComponent(`${adminAppUrl}//attacker.example/capture`),
    ])('keeps a freshly issued code on the admin origin for %s', async value => {
        const { component, authService } = createComponent();
        component['adminReturnUrl'] = value;

        const result = await component['tryBuildAdminRedirectUrlAsync']();

        expect(result).not.toBeNull();
        const destination = new URL(result ?? '');
        expect(destination.origin).toBe(new URL(adminAppUrl).origin);
        expect(destination.pathname).toBe('/');
        expect(destination.searchParams.get('code')).toBe('fresh-admin-code');
        expect(authService.startAdminSso).toHaveBeenCalledOnce();
    });

    it('does not request a code without a handoff target', async () => {
        const { component, authService } = createComponent();

        expect(await component['tryBuildAdminRedirectUrlAsync']()).toBeNull();
        expect(authService.startAdminSso).not.toHaveBeenCalled();
    });

    it.each(['/users?tab=active', `${adminAppUrl}/users?tab=active`])('preserves a valid return URL: %s', async value => {
        const { component } = createComponent();
        component['adminReturnUrl'] = value;

        const destination = new URL((await component['tryBuildAdminRedirectUrlAsync']()) ?? '');

        expect(destination.origin).toBe(new URL(adminAppUrl).origin);
        expect(destination.pathname).toBe('/users');
        expect(destination.searchParams.get('tab')).toBe('active');
        expect(destination.searchParams.get('code')).toBe('fresh-admin-code');
    });

    it('sends a non-admin to the trusted unauthorized page without issuing a code', async () => {
        const { component, authService } = createComponent();
        authService.isAdmin.mockReturnValue(false);
        component['adminReturnUrl'] = `${adminAppUrl}//attacker.example/capture`;

        const destination = new URL((await component['tryBuildAdminRedirectUrlAsync']()) ?? '');

        expect(destination.origin).toBe(new URL(adminAppUrl).origin);
        expect(destination.pathname).toBe('/unauthorized');
        expect(destination.searchParams.get('returnUrl')).toBe('/');
        expect(destination.searchParams.has('code')).toBe(false);
        expect(authService.startAdminSso).not.toHaveBeenCalled();
    });

    it('preserves the unauthorized fallback when code issuance fails', async () => {
        const { component, authService } = createComponent();
        component['adminReturnUrl'] = '/users?tab=active';
        authService.startAdminSso.mockReturnValue(throwError(() => new Error('Unavailable')));

        const destination = new URL((await component['tryBuildAdminRedirectUrlAsync']()) ?? '');

        expect(destination.origin).toBe(new URL(adminAppUrl).origin);
        expect(destination.pathname).toBe('/unauthorized');
        expect(destination.searchParams.get('returnUrl')).toBe('/users?tab=active');
        expect(destination.searchParams.has('code')).toBe(false);
    });
});

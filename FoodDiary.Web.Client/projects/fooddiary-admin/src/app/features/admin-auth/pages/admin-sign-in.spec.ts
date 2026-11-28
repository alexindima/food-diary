import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, Subject } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, type MockInstance, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../../environments/environment';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../lib/admin-auth.service';
import { AdminGoogleSignInService } from '../lib/admin-google-sign-in.service';
import { AdminSignInComponent } from './admin-sign-in';

const ADMIN_ORIGIN = 'https://admin.example.test';
const ACCOUNT = adminId<'user'>('account-1');
const RETURN_URL = '/users?page=2';
const originalGoogleClientId = environment.googleClientId;

type SignInFixture = ComponentFixture<AdminSignInComponent>;
type AuthMock = {
    [Method in 'restoreSessionAsync' | 'signInWithPasswordAsync' | 'signInWithGoogleAsync']: Mock<AdminAuthService[Method]>;
};
type SignInContext = {
    fixture: SignInFixture;
    auth: AuthMock;
    renderGoogle: Mock<AdminGoogleSignInService['renderAsync']>;
    replace: Mock<(url: string) => void>;
    navigate: MockInstance<Router['navigate']>;
    navigateByUrl: MockInstance<Router['navigateByUrl']>;
};

async function initializeLanguageAsync(locale: string | null): Promise<void> {
    if (locale !== null) {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation(locale, {});
        await firstValueFrom(translate.use(locale));
    }
}

async function createPageAsync({
    confirm = '1',
    returnUrl = RETURN_URL,
    account = ACCOUNT,
    restoredAccount = null,
    unavailable = false,
    googleUnavailable = false,
    locale = 'en',
}: {
    confirm?: string | string[];
    returnUrl?: string | string[];
    account?: string;
    restoredAccount?: string | null;
    unavailable?: boolean;
    googleUnavailable?: boolean;
    locale?: string | null;
} = {}): Promise<SignInContext> {
    const auth = {
        restoreSessionAsync: vi.fn<AdminAuthService['restoreSessionAsync']>().mockResolvedValue(undefined),
        sessionUnavailable: signal(unavailable).asReadonly(),
        isAdmin: vi.fn(() => restoredAccount !== null),
        getUserId: vi.fn(() => restoredAccount),
        signInWithPasswordAsync: vi.fn<AdminAuthService['signInWithPasswordAsync']>(),
        signInWithGoogleAsync: vi.fn<AdminAuthService['signInWithGoogleAsync']>(),
    };
    const renderGoogle = vi.fn<AdminGoogleSignInService['renderAsync']>();
    if (googleUnavailable) {
        renderGoogle.mockRejectedValue(new Error('Google Identity Services unavailable'));
    } else {
        renderGoogle.mockResolvedValue(undefined);
    }
    const replace = vi.fn<(url: string) => void>();
    await TestBed.configureTestingModule({
        imports: [AdminSignInComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        fragment: new URLSearchParams({ account }).toString(),
                    },
                },
            },
            { provide: AdminAuthService, useValue: auth },
            { provide: AdminGoogleSignInService, useValue: { renderAsync: renderGoogle } },
        ],
    })
        .overrideComponent(AdminSignInComponent, {
            add: { providers: [{ provide: DOCUMENT, useValue: { location: { origin: ADMIN_ORIGIN, replace } } }] },
        })
        .compileComponents();

    await initializeLanguageAsync(locale);

    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const navigateByUrl = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(AdminSignInComponent);
    fixture.componentRef.setInput('confirm', confirm);
    fixture.componentRef.setInput('returnUrl', returnUrl);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, auth, renderGoogle, replace, navigate, navigateByUrl };
}

function element(fixture: SignInFixture, selector: string): HTMLElement {
    const found = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>(selector);
    if (found === null) {
        throw new Error(`Expected sign-in control: ${selector}`);
    }
    return found;
}

function enterCredentials(fixture: SignInFixture, email: string, password: string): void {
    for (const [type, value] of Object.entries({ email, password })) {
        const input = element(fixture, `input[type="${type}"]`) as HTMLInputElement;
        input.value = value;
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }
    fixture.detectChanges();
}

async function submitAsync(fixture: SignInFixture): Promise<void> {
    element(fixture, 'form').dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
    fixture.detectChanges();
}

afterEach(() => {
    environment.googleClientId = originalGoogleClientId;
});

describe('Admin sign-in navigation defaults', () => {
    it('takes a restored administrator to the dashboard when no return route is supplied', async () => {
        const { navigateByUrl, auth } = await createPageAsync({ confirm: [], returnUrl: [], restoredAccount: ACCOUNT });

        expect(auth.restoreSessionAsync).toHaveBeenCalledOnce();
        expect(navigateByUrl).toHaveBeenCalledExactlyOnceWith('/', { replaceUrl: true });
    });

    it('uses English for Google before a language is active', async () => {
        environment.googleClientId = 'test-google-client';
        const { fixture, renderGoogle } = await createPageAsync({ locale: null });
        const target = element(fixture, '.admin-sign-in__google');
        expect(renderGoogle).toHaveBeenCalledExactlyOnceWith(target, 'test-google-client', 'en', expect.any(Function));
    });

    it.each(['/sign-in;step=again', '/unauthorized', '/users?returnUrl=%2Fusers'])(
        'returns to the admin root instead of creating another sign-in loop through %s',
        async returnUrl => {
            const { navigateByUrl } = await createPageAsync({ confirm: '', restoredAccount: ACCOUNT, returnUrl });
            expect(navigateByUrl).toHaveBeenCalledExactlyOnceWith('/', { replaceUrl: true });
        },
    );
});

describe('Admin sign-in confirmation', () => {
    it('keeps password confirmation available when a failure has no service error code', async () => {
        const { fixture, auth, navigateByUrl } = await createPageAsync();
        auth.signInWithPasswordAsync.mockRejectedValue(new Error('Network unavailable'));
        enterCredentials(fixture, 'admin@example.test', 'password');
        await submitAsync(fixture);

        expect(element(fixture, '[role="alert"]').textContent).toContain('ADMIN_AUTH.SIGN_IN_ERROR');
        expect((element(fixture, 'button[type="submit"]') as HTMLButtonElement).disabled).toBe(false);
        expect(navigateByUrl).not.toHaveBeenCalled();
    });

    it('hands off to the diary confirmation without a fragment when the handoff account is empty', async () => {
        const { replace, auth } = await createPageAsync({ confirm: ['0', '1'], account: '' });

        expect(replace).toHaveBeenCalledOnce();
        const redirect = new URL(replace.mock.calls[0][0]);
        expect(redirect.pathname).toBe('/auth/confirm-admin');
        expect(redirect.searchParams.get('adminReturnUrl')).toBe(RETURN_URL);
        expect(redirect.hash).toBe('');
        expect(auth.restoreSessionAsync).not.toHaveBeenCalled();
    });

    it('blocks invalid credentials and submits a valid password to the account named in the fragment', async () => {
        const { auth, fixture, navigateByUrl } = await createPageAsync({
            returnUrl: [`${RETURN_URL}&code=old#account=another&tab=active`, '/audit'],
        });
        auth.signInWithPasswordAsync.mockResolvedValue(true);

        enterCredentials(fixture, 'not-an-email', 'password');
        await submitAsync(fixture);
        expect(auth.signInWithPasswordAsync).not.toHaveBeenCalled();
        expect(navigateByUrl).not.toHaveBeenCalled();

        enterCredentials(fixture, 'admin@example.test', 'password');
        await submitAsync(fixture);
        expect(auth.signInWithPasswordAsync).toHaveBeenCalledExactlyOnceWith('admin@example.test', 'password', ACCOUNT);
        expect(navigateByUrl).toHaveBeenCalledExactlyOnceWith(`${RETURN_URL}#tab=active`, { replaceUrl: true });
        expect(auth.restoreSessionAsync).not.toHaveBeenCalled();
    });

    it('shows a rejected sign-in and an account-mismatch error with password retry still available', async () => {
        const { fixture, auth, navigateByUrl } = await createPageAsync();
        auth.signInWithPasswordAsync
            .mockResolvedValueOnce(false)
            .mockRejectedValueOnce(
                new HttpErrorResponse({ error: { error: 'Authentication.ReauthenticationAccountMismatch' }, status: 409 }),
            );
        enterCredentials(fixture, 'admin@example.test', 'password');

        await submitAsync(fixture);
        expect(element(fixture, '[role="alert"]').textContent).toContain('ADMIN_AUTH.SIGN_IN_ERROR');
        await submitAsync(fixture);
        expect(element(fixture, '[role="alert"]').textContent).toContain('AUTH.CONFIRM.ACCOUNT_MISMATCH');
        expect((element(fixture, 'button[type="submit"]') as HTMLButtonElement).disabled).toBe(false);
        expect((element(fixture, 'input[type="email"]') as HTMLInputElement).value).toBe('admin@example.test');
        expect(navigateByUrl).not.toHaveBeenCalled();
    });
});

describe('Admin sign-in Google confirmation', () => {
    it('keeps the initial Google confirmation and destination through later input updates', async () => {
        environment.googleClientId = 'test-google-client';
        const { fixture, auth, renderGoogle, navigateByUrl } = await createPageAsync({ locale: 'ru', returnUrl: '//other.example.test/' });
        const pending = new Subject<boolean>();
        auth.signInWithGoogleAsync.mockReturnValue(firstValueFrom(pending));
        const target = element(fixture, '.admin-sign-in__google');
        expect(renderGoogle).toHaveBeenCalledExactlyOnceWith(target, 'test-google-client', 'ru', expect.any(Function));
        const callback = renderGoogle.mock.calls[0][3];

        fixture.componentRef.setInput('confirm', '0');
        fixture.componentRef.setInput('returnUrl', '/mail-inbox');
        fixture.detectChanges();
        callback('test-credential');
        callback('duplicate-credential');
        fixture.componentRef.setInput('returnUrl', '/audit');
        fixture.detectChanges();
        expect(element(fixture, 'form').getAttribute('aria-busy')).toBe('true');
        expect(target.hasAttribute('inert')).toBe(true);
        expect((element(fixture, 'button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
        expect(auth.signInWithGoogleAsync).toHaveBeenCalledExactlyOnceWith('test-credential', ACCOUNT);
        expect(auth.restoreSessionAsync).not.toHaveBeenCalled();
        expect(renderGoogle).toHaveBeenCalledOnce();
        expect(navigateByUrl).not.toHaveBeenCalled();

        pending.next(true);
        await vi.waitFor(() => {
            expect(navigateByUrl).toHaveBeenCalledExactlyOnceWith('/', { replaceUrl: true });
        });
        await fixture.whenStable();
        expect(target.hasAttribute('inert')).toBe(false);
    });

    it('announces a Google initialization failure while retaining password sign-in', async () => {
        environment.googleClientId = 'test-google-client';
        const { fixture } = await createPageAsync({ googleUnavailable: true });
        fixture.detectChanges();

        expect(element(fixture, '[role="status"]').textContent).toContain('ADMIN_AUTH.SIGN_IN_GOOGLE_UNAVAILABLE');
        expect((element(fixture, 'button[type="submit"]') as HTMLButtonElement).disabled).toBe(false);
    });
});

describe('Admin sign-in session restoration', () => {
    it('takes the restored administrator for the requested account to the original route', async () => {
        const { fixture, auth, navigateByUrl, replace } = await createPageAsync({ confirm: '', restoredAccount: ACCOUNT });

        expect(element(fixture, '[role="status"]').textContent).toContain('COMMON.LOADING');
        expect(auth.restoreSessionAsync).toHaveBeenCalledOnce();
        expect(navigateByUrl).toHaveBeenCalledExactlyOnceWith(RETURN_URL, { replaceUrl: true });
        expect(replace).not.toHaveBeenCalled();
    });

    it('preserves the return route and account when restoration is temporarily unavailable', async () => {
        const { navigate, navigateByUrl, replace } = await createPageAsync({ confirm: '', unavailable: true });

        expect(navigate).toHaveBeenCalledExactlyOnceWith(['/unauthorized'], {
            queryParams: { reason: 'unavailable', returnUrl: RETURN_URL },
            fragment: `account=${ACCOUNT}`,
            replaceUrl: true,
        });
        expect(navigateByUrl).not.toHaveBeenCalled();
        expect(replace).not.toHaveBeenCalled();
    });

    it.each([
        { confirm: '', restoredAccount: 'different-account', shouldRestore: true },
        { confirm: '0', restoredAccount: ACCOUNT, shouldRestore: false },
    ])(
        'uses diary confirmation for confirm=$confirm and restored account $restoredAccount',
        async ({ confirm, restoredAccount, shouldRestore }) => {
            const { auth, navigateByUrl, replace } = await createPageAsync({ confirm, restoredAccount });

            expect(auth.restoreSessionAsync).toHaveBeenCalledTimes(Number(shouldRestore));
            expect(navigateByUrl).not.toHaveBeenCalled();
            expect(replace).toHaveBeenCalledOnce();
            const destination = new URL(replace.mock.calls[0]?.[0] ?? '');
            expect(destination.origin).toBe(new URL(environment.mainAppUrl).origin);
            expect(destination.pathname).toBe('/auth/confirm-admin');
            expect(destination.searchParams.get('adminReturnUrl')).toBe(RETURN_URL);
            expect(destination.hash).toBe(`#account=${ACCOUNT}`);
        },
    );
});

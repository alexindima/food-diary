import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, of, throwError } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { User } from '../../../../shared/models/user.data';
import { AuthDialogComponent } from '../../dialogs/auth-dialog/auth-dialog';
import type { AuthResponse, ConfirmPasswordResetRequest } from '../../models/auth.data';
import { PasswordResetComponent } from './password-reset';

const PASSWORD_CONTRACT_MAX_LENGTH = 256;
const RESET_PASSWORD_FIELD_COUNT = 2;

const USER: User = {
    id: entityId<'user'>('user-1'),
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
    user: USER,
};
const DEFAULT_QUERY_PARAMS = { userId: 'user-1', token: 'tok-abc' };

type PasswordResetTestContext = {
    authServiceSpy: { confirmPasswordReset: ReturnType<typeof vi.fn> };
    component: PasswordResetComponent;
    dialogServiceSpy: { open: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<PasswordResetComponent>;
    navigationServiceSpy: {
        navigateToAuthAsync: ReturnType<typeof vi.fn>;
        navigateToHomeAsync: ReturnType<typeof vi.fn>;
    };
};

function createComponent(queryParams: Record<string, string> = DEFAULT_QUERY_PARAMS): PasswordResetTestContext {
    const authServiceSpy = { confirmPasswordReset: vi.fn() };
    const dialogServiceSpy = { open: vi.fn<FdUiDialogService['open']>() };
    const navigationServiceSpy = { navigateToHomeAsync: vi.fn(), navigateToAuthAsync: vi.fn() };
    navigationServiceSpy.navigateToHomeAsync.mockResolvedValue(undefined);
    navigationServiceSpy.navigateToAuthAsync.mockResolvedValue(undefined);

    TestBed.configureTestingModule({
        imports: [PasswordResetComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AuthService, useValue: authServiceSpy },
            { provide: FdUiDialogService, useValue: dialogServiceSpy },
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

    const fixture = TestBed.createComponent(PasswordResetComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { authServiceSpy, component, dialogServiceSpy, fixture, navigationServiceSpy };
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

    it('rejects a matching 257-character password without calling the API', async () => {
        const { component, authServiceSpy } = createComponent();
        component['formModel'].set({
            password: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1),
            confirmPassword: 'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH + 1),
        });

        await component['submitAsync']();

        expect(component['form'].password().getError('maxLength')).toBeDefined();
        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });

    it('submits the literal 256-character boundary without trimming', async () => {
        const { component, authServiceSpy } = createComponent();
        const password = ` ${'p'.repeat(PASSWORD_CONTRACT_MAX_LENGTH - 2)} `;
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));
        component['formModel'].set({ password, confirmPassword: password });

        await component['submitAsync']();

        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledWith(expect.objectContaining({ newPassword: password }));
    });
});

describe('PasswordResetComponent submit', () => {
    it('allows an invalid submit to reveal errors and focus the password without sending a request', async () => {
        const { fixture, authServiceSpy } = createComponent();
        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLButtonElement>('button[type="submit"]');
        expect(button?.disabled, 'An invalid form must allow an explanatory submit attempt').toBe(false);
        button?.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('FORM_ERRORS.REQUIRED');
        expect(document.activeElement).toBe(host.querySelector('input[type="password"]'));
        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });

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
describe('Password reset recovery controls', () => {
    it('offers a new link and login after the server rejects an expired reset link', async () => {
        const { authServiceSpy, dialogServiceSpy, fixture, navigationServiceSpy } = createComponent();
        authServiceSpy.confirmPasswordReset.mockReturnValue(
            throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Unauthorized })),
        );
        enterResetPasswords(fixture, 'newPassword123');
        await submitRenderedResetAsync(fixture);
        const root = fixture.nativeElement as HTMLElement;
        const state = root.querySelector<HTMLElement>('.password-reset__state');
        if (state === null) {
            throw new Error('The expired link did not reveal recovery actions');
        }

        expect(state.getAttribute('role')).toBe('status');
        expect(state.textContent).toContain('AUTH.RESET.INVALID');
        expect(root.querySelector('form')).toBeNull();
        expect(navigationServiceSpy.navigateToHomeAsync).not.toHaveBeenCalled();

        resetButton(state, 'AUTH.RESET.REQUEST_NEW_LINK').click();
        expect(dialogServiceSpy.open).toHaveBeenCalledExactlyOnceWith(AuthDialogComponent, {
            preset: 'form',
            ariaLabel: 'AUTH.DIALOG_TITLE',
            data: { mode: 'login', requestPasswordReset: true },
        });
        resetButton(state, 'AUTH.RESET.BACK_TO_LOGIN').click();
        expect(navigationServiceSpy.navigateToAuthAsync).toHaveBeenCalledExactlyOnceWith('login');
        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledOnce();
    });

    it('updates a displayed mismatch message after switching language and keeps the entered values', async () => {
        const { authServiceSpy, fixture } = createComponent();
        const translate = TestBed.inject(TranslateService);
        const mismatchEn = 'Passwords must be the same';
        const mismatchRu = 'Пароли должны совпадать';
        translate.setTranslation('en', { FORM_ERRORS: { PASSWORD: { MATCH: mismatchEn } } });
        translate.setTranslation('ru', { FORM_ERRORS: { PASSWORD: { MATCH: mismatchRu } } });
        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();
        const password = 'newPassword123';
        const confirmation = 'differentPassword123';
        const [firstInput, confirmationInput] = enterResetPasswords(fixture, password, confirmation);
        await submitRenderedResetAsync(fixture);

        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('[role="alert"]')?.textContent).toContain(mismatchEn);
        expect(confirmationInput.getAttribute('aria-invalid')).toBe('true');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect(root.querySelector('[role="alert"]')?.textContent).toContain(mismatchRu);
        expect(firstInput.value).toBe(password);
        expect(confirmationInput.value).toBe(confirmation);
        expect(authServiceSpy.confirmPasswordReset).not.toHaveBeenCalled();
    });
});

describe('Password reset link compatibility', () => {
    it.each(['user', 'id'])('submits a legacy link using the %s query parameter', async key => {
        const { authServiceSpy, fixture } = createComponent({ [key]: 'legacy-account', token: 'legacy-reset-token' });
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));
        enterResetPasswords(fixture, 'newPassword123');
        await submitRenderedResetAsync(fixture);

        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ userId: 'legacy-account', token: 'legacy-reset-token', newPassword: 'newPassword123' }),
        );
    });

    it('removes fragment tokens from the visible URL and uses them for the submitted reset', async () => {
        const history = document.defaultView?.history;
        if (history === undefined) {
            throw new Error('Expected browser history for reset link');
        }
        const originalUrl = document.location.href;
        const originalState: unknown = history.state;
        history.replaceState(null, '', '/reset-password?from=email#userId=fragment-account&token=fragment-reset-token');
        onTestFinished(() => {
            history.replaceState(originalState, '', originalUrl);
        });
        const { authServiceSpy, fixture } = createComponent({ userId: 'legacy-account', token: 'legacy-reset-token' });
        authServiceSpy.confirmPasswordReset.mockReturnValue(of(AUTH_RESPONSE));

        expect(document.location.pathname).toBe('/reset-password');
        expect(document.location.search).toBe('?from=email');
        expect(document.location.hash).toBe('');

        enterResetPasswords(fixture, 'newPassword123');
        await submitRenderedResetAsync(fixture);
        expect(authServiceSpy.confirmPasswordReset).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ userId: 'fragment-account', token: 'fragment-reset-token', newPassword: 'newPassword123' }),
        );
    });
});

function enterResetPasswords(
    fixture: ComponentFixture<PasswordResetComponent>,
    password: string,
    confirmation = password,
): [HTMLInputElement, HTMLInputElement] {
    const root = fixture.nativeElement as HTMLElement;
    const inputs = root.querySelectorAll<HTMLInputElement>('input[autocomplete="new-password"]');
    if (inputs.length !== RESET_PASSWORD_FIELD_COUNT) {
        throw new Error('Expected both password reset inputs');
    }
    const [first, second] = inputs;
    first.value = password;
    first.dispatchEvent(new Event('input', { bubbles: true }));
    second.value = confirmation;
    second.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    return [first, second];
}

async function submitRenderedResetAsync(fixture: ComponentFixture<PasswordResetComponent>): Promise<void> {
    const root = fixture.nativeElement as HTMLElement;
    resetButton(root, 'AUTH.RESET.SAVE').click();
    await fixture.whenStable();
    fixture.detectChanges();
}

function resetButton(host: HTMLElement, label: string): HTMLButtonElement {
    const button = [...host.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === label);
    if (button === undefined) {
        throw new Error(`Expected the password reset button ${label}`);
    }
    return button;
}

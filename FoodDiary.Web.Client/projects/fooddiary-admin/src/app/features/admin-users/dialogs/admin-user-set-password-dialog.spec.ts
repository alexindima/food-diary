import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import enCore from '../../../../../../../assets/i18n/en/core.json';
import ruCore from '../../../../../../../assets/i18n/ru/core.json';
import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminUsersService } from '../api/admin-users.service';
import { AdminUserSetPasswordDialogComponent } from './admin-user-set-password-dialog';

const user = {
    id: 'u1',
    email: 'user@example.com',
    username: 'alex',
    isActive: true,
    isEmailConfirmed: true,
    createdOnUtc: '2026-01-01T00:00:00Z',
    roles: [],
};

let component: AdminUserSetPasswordDialogComponent;
let fixture: ComponentFixture<AdminUserSetPasswordDialogComponent>;
let usersService: { setPassword: ReturnType<typeof vi.fn> };
let dialogRef: { close: ReturnType<typeof vi.fn> };

beforeEach(async () => {
    usersService = { setPassword: vi.fn() };
    dialogRef = { close: vi.fn() };
    usersService.setPassword.mockReturnValue(of(null));

    await TestBed.configureTestingModule({
        imports: [AdminUserSetPasswordDialogComponent],
        providers: [
            provideTranslateTesting({ lang: 'en' }),
            { provide: AdminUsersService, useValue: usersService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: user },
        ],
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { FORM_ERRORS: enCore.FORM_ERRORS });
    translate.setTranslation('ru', { FORM_ERRORS: ruCore.FORM_ERRORS });
    fixture = TestBed.createComponent(AdminUserSetPasswordDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
});

describe('Admin password validation through the form', () => {
    const PASSWORD_MAXIMUM = 256;
    const oversizedPassword = 'p'.repeat(PASSWORD_MAXIMUM + 1);

    it.each([
        { password: '       ', confirmation: '       ', lang: 'en', message: 'New password must be at least 6 characters.' },
        { password: '   five!  ', confirmation: '   five!  ', lang: 'en', message: 'New password must be at least 6 characters.' },
        { password: oversizedPassword, confirmation: oversizedPassword, lang: 'en', message: 'Use 256 characters or fewer' },
        { password: oversizedPassword, confirmation: oversizedPassword, lang: 'ru', message: 'Не больше 256 символов' },
        { password: 'test-password', confirmation: '', lang: 'en', message: 'Confirm password is required.' },
    ])('blocks the request and displays $message', async ({ password, confirmation, lang, message }) => {
        await firstValueFrom(TestBed.inject(TranslateService).use(lang));
        const element = fixture.nativeElement as HTMLElement;
        const inputs = element.querySelectorAll<HTMLInputElement>('input[type="password"]');
        for (const [index, value] of [password, confirmation].entries()) {
            inputs[index].value = value;
            inputs[index].dispatchEvent(new Event('input', { bubbles: true }));
        }
        fixture.detectChanges();
        element.querySelector<HTMLButtonElement>('button[type="submit"]')?.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(usersService.setPassword).not.toHaveBeenCalled();
        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(element.textContent).toContain(message);
    });
});

describe('AdminUserSetPasswordDialogComponent', () => {
    it('should require a new password', () => {
        component['submit']();

        expect(usersService.setPassword).not.toHaveBeenCalled();
        expect(component['newPasswordError']()).toBe('New password is required.');
    });

    it('should validate minimum password length', () => {
        component['form'].newPassword().value.set('123');
        component['form'].confirmPassword().value.set('123');
        component['submit']();

        expect(usersService.setPassword).not.toHaveBeenCalled();
        expect(component['newPasswordError']()).toBe('New password must be at least 6 characters.');
    });

    it('should validate password confirmation', () => {
        component['form'].newPassword().value.set('NewPassword123!');
        component['form'].confirmPassword().value.set('Different123!');
        component['submit']();

        expect(usersService.setPassword).not.toHaveBeenCalled();
        expect(component['confirmPasswordError']()).toBe('Passwords must match.');
    });

    it('should set password and close with success', async () => {
        component['form'].newPassword().value.set(' NewPassword123! ');
        component['form'].confirmPassword().value.set(' NewPassword123! ');
        component['submit']();

        expect(usersService.setPassword).toHaveBeenCalledWith('u1', { newPassword: 'NewPassword123!' });
        await vi.waitFor(() => {
            expect(dialogRef.close).toHaveBeenCalledWith(true);
        });
    });

    it('should prevent native form submit when setting password', async () => {
        component['form'].newPassword().value.set('NewPassword123!');
        component['form'].confirmPassword().value.set('NewPassword123!');
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(usersService.setPassword).toHaveBeenCalledWith('u1', { newPassword: 'NewPassword123!' });
    });

    it('should show submit error on failure', async () => {
        usersService.setPassword.mockReturnValueOnce(throwError(() => new Error('failed')));
        component['form'].newPassword().value.set('NewPassword123!');
        component['form'].confirmPassword().value.set('NewPassword123!');
        component['submit']();

        expect(dialogRef.close).not.toHaveBeenCalled();
        await vi.waitFor(() => {
            expect(component['submitError']()).toBe('Could not set password. Please try again.');
        });
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should close with false on cancel', () => {
        component['close']();

        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });
});

describe('AdminUserSetPasswordDialogComponent pending saves', () => {
    it('locks both password fields and cancellation during a pending request, then permits retry', async () => {
        const pending = new Subject<null>();
        usersService.setPassword.mockReturnValueOnce(pending);
        component['formModel'].set({ newPassword: 'QaPassword123!', confirmPassword: 'QaPassword123!' });
        component['submit']();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        for (const control of element.querySelectorAll<HTMLInputElement>('input')) {
            expect(control.disabled).toBe(true);
        }
        component['close']();
        expect(dialogRef.close).not.toHaveBeenCalled();

        pending.error(new Error('offline'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(element.querySelector<HTMLInputElement>('input')?.disabled).toBe(false);
        });
        expect(component['formModel']().newPassword).toBe('QaPassword123!');
        component['submit']();
        await vi.waitFor(() => {
            expect(dialogRef.close).toHaveBeenCalledWith(true);
        });
    });
});

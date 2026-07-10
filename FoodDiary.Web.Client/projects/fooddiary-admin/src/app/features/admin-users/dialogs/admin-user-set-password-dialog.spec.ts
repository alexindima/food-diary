import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminUsersFacade } from '../lib/admin-users.facade';
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

describe('AdminUserSetPasswordDialogComponent', () => {
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
                { provide: AdminUsersFacade, useValue: usersService },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: user },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminUserSetPasswordDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

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

import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminUsersService } from '../api/admin-users.service';
import { AdminUserImpersonationDialogComponent } from './admin-user-impersonation-dialog.component';

describe('AdminUserImpersonationDialogComponent', () => {
    let component: AdminUserImpersonationDialogComponent;
    let fixture: ComponentFixture<AdminUserImpersonationDialogComponent>;
    let usersService: { startImpersonation: ReturnType<typeof vi.fn> };
    let dialogRef: { close: ReturnType<typeof vi.fn> };

    const user = {
        id: 'u1',
        email: 'user@example.com',
        username: 'alex',
        isActive: true,
        isEmailConfirmed: true,
        createdOnUtc: '2026-01-01T00:00:00Z',
        roles: [],
    };

    const response = {
        accessToken: 'token',
        expiresAtUtc: '2026-01-01T00:10:00Z',
        reason: 'Support case investigation',
    };

    beforeEach(async () => {
        usersService = { startImpersonation: vi.fn() };
        dialogRef = { close: vi.fn() };
        usersService.startImpersonation.mockReturnValue(of(response));

        await TestBed.configureTestingModule({
            imports: [AdminUserImpersonationDialogComponent],
            providers: [
                { provide: AdminUsersService, useValue: usersService },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: user },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminUserImpersonationDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should require a reason', () => {
        component.submit();

        expect(usersService.startImpersonation).not.toHaveBeenCalled();
        expect(component.reasonError()).toBe('Reason is required.');
    });

    it('should validate minimum reason length', () => {
        component.form.controls.reason.setValue('too short');
        component.submit();

        expect(usersService.startImpersonation).not.toHaveBeenCalled();
        expect(component.reasonError()).toBe('Reason must be at least 10 characters.');
    });

    it('should start impersonation and close with response', () => {
        component.form.controls.reason.setValue(' Support case investigation ');
        component.submit();

        expect(usersService.startImpersonation).toHaveBeenCalledWith('u1', 'Support case investigation');
        expect(dialogRef.close).toHaveBeenCalledWith(response);
    });

    it('should show submit error on failure', () => {
        usersService.startImpersonation.mockReturnValueOnce(throwError(() => new Error('failed')));
        component.form.controls.reason.setValue('Support case investigation');
        component.submit();

        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(component.submitError()).toBe('Could not start impersonation. Please try again.');
        expect(component.isSubmitting()).toBe(false);
    });

    it('should close with null on cancel', () => {
        component.close();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

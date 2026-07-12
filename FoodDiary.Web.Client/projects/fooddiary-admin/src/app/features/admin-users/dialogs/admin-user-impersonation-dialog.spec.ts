import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import { AdminUserImpersonationDialogComponent } from './admin-user-impersonation-dialog';

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

describe('AdminUserImpersonationDialogComponent', () => {
    let component: AdminUserImpersonationDialogComponent;
    let fixture: ComponentFixture<AdminUserImpersonationDialogComponent>;
    let usersService: { startImpersonation: ReturnType<typeof vi.fn> };
    let dialogRef: { close: ReturnType<typeof vi.fn> };

    beforeEach(async () => {
        usersService = { startImpersonation: vi.fn() };
        dialogRef = { close: vi.fn() };
        usersService.startImpersonation.mockReturnValue(of(response));

        await TestBed.configureTestingModule({
            imports: [AdminUserImpersonationDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: AdminUsersFacade, useValue: usersService },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: user },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminUserImpersonationDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should require a reason', () => {
        component['submit']();

        expect(usersService.startImpersonation).not.toHaveBeenCalled();
        expect(component['reasonError']()).toBe('Reason is required.');
    });

    it('should validate minimum reason length', () => {
        component['form'].reason().value.set('too short');
        component['submit']();

        expect(usersService.startImpersonation).not.toHaveBeenCalled();
        expect(component['reasonError']()).toBe('Reason must be at least 10 characters.');
    });

    it('should start impersonation and close with response', async () => {
        component['form'].reason().value.set(' Support case investigation ');
        component['submit']();

        expect(usersService.startImpersonation).toHaveBeenCalledWith('u1', 'Support case investigation');
        await vi.waitFor(() => {
            expect(dialogRef.close).toHaveBeenCalledWith(response);
        });
    });

    it('should prevent native form submit when starting impersonation', async () => {
        component['form'].reason().value.set(' Support case investigation ');
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(usersService.startImpersonation).toHaveBeenCalledWith('u1', 'Support case investigation');
    });

    it('should show submit error on failure', async () => {
        usersService.startImpersonation.mockReturnValueOnce(throwError(() => new Error('failed')));
        component['form'].reason().value.set('Support case investigation');
        component['submit']();

        expect(dialogRef.close).not.toHaveBeenCalled();
        await vi.waitFor(() => {
            expect(component['submitError']()).toBe('Could not start impersonation. Please try again.');
        });
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should close with null on cancel', () => {
        component['close']();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

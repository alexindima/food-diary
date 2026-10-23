import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { NEVER, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import { AdminUserEditDialogComponent } from './admin-user-edit-dialog';

let component: AdminUserEditDialogComponent;
let fixture: ComponentFixture<AdminUserEditDialogComponent>;
let usersService: { updateUser: ReturnType<typeof vi.fn> };
let dialogRef: { close: ReturnType<typeof vi.fn> };

const user = {
    id: 'u1',
    email: 'user@example.com',
    language: 'en',
    isActive: true,
    isEmailConfirmed: false,
    createdOnUtc: '2026-01-01T00:00:00Z',
    roles: ['Admin'],
};

beforeEach(async () => {
    usersService = { updateUser: vi.fn() };
    dialogRef = { close: vi.fn() };
    usersService.updateUser.mockReturnValue(of(user));

    await TestBed.configureTestingModule({
        imports: [AdminUserEditDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AdminUsersFacade, useValue: usersService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: user },
        ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUserEditDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
});

describe('AdminUserEditDialogComponent', () => {
    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should toggle roles', () => {
        expect(component['hasRole']('Admin')).toBe(true);

        component['toggleRole']('Support');
        expect(component['hasRole']('Support')).toBe(true);

        component['toggleRole']('Admin');
        expect(component['hasRole']('Admin')).toBe(false);
    });

    it('should close with false on cancel', () => {
        component['close']();
        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('should save and close with true on success', () => {
        component['formModel'].update(value => ({ ...value, isEmailConfirmed: true, language: 'ru' }));
        component['save']();

        expect(usersService.updateUser).toHaveBeenCalledWith('u1', {
            isActive: true,
            isEmailConfirmed: true,
            roles: ['Admin'],
            language: 'ru',
        });
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });

    it('should not submit twice while save is running', () => {
        usersService.updateUser.mockReturnValueOnce(NEVER);

        component['save']();
        component['save']();

        expect(usersService.updateUser).toHaveBeenCalledTimes(1);
        expect(component['isSaving']()).toBe(true);
    });

    it('locks account fields, role changes and dismissal until a pending save fails', () => {
        const pending = new Subject<typeof user>();
        usersService.updateUser.mockReturnValueOnce(pending);
        component['save']();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        for (const control of element.querySelectorAll<HTMLInputElement>('input')) {
            expect(control.disabled).toBe(true);
        }
        expect(element.querySelector<HTMLButtonElement>('fd-ui-select button')?.disabled).toBe(true);
        expect(element.querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button')?.disabled).toBe(true);
        component['toggleRole']('Support');
        component['close']();
        expect(component['hasRole']('Support')).toBe(false);
        expect(dialogRef.close).not.toHaveBeenCalled();

        pending.error(new Error('offline'));
        fixture.detectChanges();
        expect(element.querySelector<HTMLInputElement>('input')?.disabled).toBe(false);
        expect(component['formModel']().isActive).toBe(true);
        expect(component['saveFailed']()).toBe(true);
    });

    it('preserves edits after a failed save, shows the error and allows retry', () => {
        usersService.updateUser.mockReturnValueOnce(throwError(() => new Error('save failed')));
        component['toggleRole']('Support');

        component['save']();

        fixture.detectChanges();
        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(component['isSaving']()).toBe(false);
        expect(component['hasRole']('Support')).toBe(true);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_USER.SAVE_ERROR');

        component['save']();
        expect(usersService.updateUser).toHaveBeenLastCalledWith('u1', expect.objectContaining({ roles: ['Admin', 'Support'] }));
        expect(dialogRef.close).toHaveBeenCalledWith(true);
        expect(component['saveFailed']()).toBe(false);
    });
});

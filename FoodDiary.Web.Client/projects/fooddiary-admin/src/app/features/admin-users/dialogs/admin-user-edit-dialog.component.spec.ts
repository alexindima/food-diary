import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { AdminUserEditDialogComponent } from './admin-user-edit-dialog.component';
import { AdminUsersService } from '../api/admin-users.service';

describe('AdminUserEditDialogComponent', () => {
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
                { provide: AdminUsersService, useValue: usersService },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: user },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminUserEditDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should toggle roles', () => {
        expect(component.hasRole('Admin')).toBe(true);

        component.toggleRole('Support');
        expect(component.hasRole('Support')).toBe(true);

        component.toggleRole('Admin');
        expect(component.hasRole('Admin')).toBe(false);
    });

    it('should close with false on cancel', () => {
        component.close();
        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('should save and close with true on success', () => {
        component.form.controls.isEmailConfirmed.setValue(true);
        component.form.controls.language.setValue('ru');
        component.save();

        expect(usersService.updateUser).toHaveBeenCalledWith('u1', {
            isActive: true,
            isEmailConfirmed: true,
            roles: ['Admin'],
            language: 'ru',
        });
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });

    it('should close with false on save failure', () => {
        usersService.updateUser.mockReturnValueOnce(throwError(() => new Error('save failed')));

        component.save();

        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });
});

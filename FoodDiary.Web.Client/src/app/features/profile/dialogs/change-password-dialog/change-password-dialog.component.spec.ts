import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../../shared/api/user.service';
import { ChangePasswordDialogComponent, type ChangePasswordDialogData } from './change-password-dialog.component';

let component: ChangePasswordDialogComponent;
let fixture: ComponentFixture<ChangePasswordDialogComponent>;
let userServiceSpy: { changePassword: ReturnType<typeof vi.fn>; setPassword: ReturnType<typeof vi.fn> };
let dialogRefSpy: { close: ReturnType<typeof vi.fn> };
let translateServiceSpy: TranslateService;

function configureComponent(dialogData: ChangePasswordDialogData | null = null): void {
    userServiceSpy = { changePassword: vi.fn(), setPassword: vi.fn() };
    dialogRefSpy = { close: vi.fn() };

    TestBed.configureTestingModule({
        imports: [ChangePasswordDialogComponent, TranslateModule.forRoot()],
        providers: [
            { provide: UserService, useValue: userServiceSpy },
            { provide: FdUiDialogRef, useValue: dialogRefSpy },
            { provide: FD_UI_DIALOG_DATA, useValue: dialogData },
        ],
    });

    translateServiceSpy = TestBed.inject(TranslateService);
    vi.spyOn(translateServiceSpy, 'instant').mockImplementation((key: string | string[]) => (Array.isArray(key) ? key[0] : key));

    fixture = TestBed.createComponent(ChangePasswordDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

beforeEach(() => {
    configureComponent();
});

describe('ChangePasswordDialogComponent validation', () => {
    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should validate required fields', () => {
        const { currentPassword, newPassword, confirmPassword } = component.form.controls;

        currentPassword.markAsTouched();
        newPassword.markAsTouched();
        confirmPassword.markAsTouched();

        expect(currentPassword.hasError('required')).toBe(true);
        expect(newPassword.hasError('required')).toBe(true);
        expect(confirmPassword.hasError('required')).toBe(true);
    });

    it('should validate newPassword minimum length', () => {
        const control = component.form.controls.newPassword;
        control.setValue('abc');
        control.markAsTouched();
        expect(control.hasError('minlength')).toBe(true);

        control.setValue('abcdef');
        expect(control.hasError('minlength')).toBe(false);
    });

    it('should validate password match', () => {
        component.form.controls.newPassword.setValue('validPass1');
        component.form.controls.confirmPassword.setValue('differentPass');
        component.form.controls.confirmPassword.markAsTouched();

        expect(component.form.controls.confirmPassword.hasError('matchField')).toBe(true);

        component.form.controls.confirmPassword.setValue('validPass1');
        expect(component.form.controls.confirmPassword.hasError('matchField')).toBe(false);
    });
});

describe('ChangePasswordDialogComponent submit', () => {
    it('should call changePassword on submit', () => {
        userServiceSpy.changePassword.mockReturnValue(of(true));

        component.form.controls.currentPassword.setValue('oldPass');
        component.form.controls.newPassword.setValue('newPass123');
        component.form.controls.confirmPassword.setValue('newPass123');
        component.onSubmit();

        expect(userServiceSpy.changePassword).toHaveBeenCalledWith({
            currentPassword: 'oldPass',
            newPassword: 'newPass123',
        });
    });

    it('should close dialog on success', () => {
        userServiceSpy.changePassword.mockReturnValue(of(true));

        component.form.controls.currentPassword.setValue('oldPass');
        component.form.controls.newPassword.setValue('newPass123');
        component.form.controls.confirmPassword.setValue('newPass123');
        component.onSubmit();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
        expect(component.isSubmitting()).toBe(false);
    });

    it('should handle error response from service returning false', () => {
        userServiceSpy.changePassword.mockReturnValue(of(false));

        component.form.controls.currentPassword.setValue('oldPass');
        component.form.controls.newPassword.setValue('newPass123');
        component.form.controls.confirmPassword.setValue('newPass123');
        component.onSubmit();

        expect(dialogRefSpy.close).not.toHaveBeenCalled();
        expect(component.passwordError()).toBe('USER_MANAGE.CHANGE_PASSWORD_ERROR');
        expect(component.isSubmitting()).toBe(false);
    });

    it('should handle error response from service throwing', () => {
        userServiceSpy.changePassword.mockReturnValue(throwError(() => new Error('network error')));

        component.form.controls.currentPassword.setValue('oldPass');
        component.form.controls.newPassword.setValue('newPass123');
        component.form.controls.confirmPassword.setValue('newPass123');
        component.onSubmit();

        expect(component.passwordError()).toBe('USER_MANAGE.CHANGE_PASSWORD_ERROR');
        expect(component.isSubmitting()).toBe(false);
    });

    it('should not submit when form is invalid', () => {
        component.form.controls.currentPassword.setValue('');
        component.onSubmit();
        expect(userServiceSpy.changePassword).not.toHaveBeenCalled();
    });
});

describe('ChangePasswordDialogComponent cancel and set password mode', () => {
    it('should not close dialog on cancel while submitting', () => {
        component.isSubmitting.set(true);
        component.onCancel();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('should close dialog on cancel when not submitting', () => {
        component.onCancel();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should use setPassword mode when account has no password', () => {
        TestBed.resetTestingModule();
        configureComponent({ hasPassword: false });

        expect(component.hasPassword).toBe(false);

        userServiceSpy.setPassword.mockReturnValue(of(true));
        component.form.controls.newPassword.setValue('newPass123');
        component.form.controls.confirmPassword.setValue('newPass123');
        component.onSubmit();

        expect(userServiceSpy.setPassword).toHaveBeenCalledWith({
            newPassword: 'newPass123',
        });
        expect(userServiceSpy.changePassword).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });
});

import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { ChangePasswordDialogComponent, type ChangePasswordDialogData } from './change-password-dialog';

let component: ChangePasswordDialogComponent;
let fixture: ComponentFixture<ChangePasswordDialogComponent>;
let userServiceSpy: { changePassword: ReturnType<typeof vi.fn>; setPassword: ReturnType<typeof vi.fn> };
let dialogRefSpy: { close: ReturnType<typeof vi.fn> };
let translateServiceSpy: TranslateService;

function configureComponent(dialogData: ChangePasswordDialogData | null = null): void {
    userServiceSpy = { changePassword: vi.fn(), setPassword: vi.fn() };
    dialogRefSpy = { close: vi.fn() };

    TestBed.configureTestingModule({
        imports: [ChangePasswordDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: UserFacade, useValue: userServiceSpy },
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
        const { currentPassword, newPassword, confirmPassword } = component['form'];

        currentPassword().markAsTouched();
        newPassword().markAsTouched();
        confirmPassword().markAsTouched();

        expect(currentPassword().getError('required')).toBeDefined();
        expect(newPassword().getError('required')).toBeDefined();
        expect(confirmPassword().getError('required')).toBeDefined();
    });

    it('should validate newPassword minimum length', () => {
        const control = component['form'].newPassword;
        control().value.set('abc');
        control().markAsTouched();
        expect(control().getError('minLength')).toBeDefined();

        control().value.set('abcdef');
        expect(control().getError('minLength')).toBeUndefined();
    });

    it('should validate password match', () => {
        component['form'].newPassword().value.set('validPass1');
        component['form'].confirmPassword().value.set('differentPass');
        component['form'].confirmPassword().markAsTouched();

        expect(component['form'].confirmPassword().getError('matchField')).toBeDefined();

        component['form'].confirmPassword().value.set('validPass1');
        expect(component['form'].confirmPassword().getError('matchField')).toBeUndefined();
    });
});

describe('ChangePasswordDialogComponent submit', () => {
    it('should call changePassword on submit', () => {
        userServiceSpy.changePassword.mockReturnValue(of(true));

        component['form'].currentPassword().value.set('oldPass');
        component['form'].newPassword().value.set('newPass123');
        component['form'].confirmPassword().value.set('newPass123');
        component['onSubmit']();

        expect(userServiceSpy.changePassword).toHaveBeenCalledWith({
            currentPassword: 'oldPass',
            newPassword: 'newPass123',
        });
    });

    it('should close dialog on success', () => {
        userServiceSpy.changePassword.mockReturnValue(of(true));

        component['form'].currentPassword().value.set('oldPass');
        component['form'].newPassword().value.set('newPass123');
        component['form'].confirmPassword().value.set('newPass123');
        component['onSubmit']();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should handle error response from service returning false', () => {
        userServiceSpy.changePassword.mockReturnValue(of(false));

        component['form'].currentPassword().value.set('oldPass');
        component['form'].newPassword().value.set('newPass123');
        component['form'].confirmPassword().value.set('newPass123');
        component['onSubmit']();

        expect(dialogRefSpy.close).not.toHaveBeenCalled();
        expect(component['passwordError']()).toBe('USER_MANAGE.CHANGE_PASSWORD_ERROR');
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should handle error response from service throwing', () => {
        userServiceSpy.changePassword.mockReturnValue(throwError(() => new Error('network error')));

        component['form'].currentPassword().value.set('oldPass');
        component['form'].newPassword().value.set('newPass123');
        component['form'].confirmPassword().value.set('newPass123');
        component['onSubmit']();

        expect(component['passwordError']()).toBe('USER_MANAGE.CHANGE_PASSWORD_ERROR');
        expect(component['isSubmitting']()).toBe(false);
    });

    it('should not submit when form is invalid', () => {
        component['form'].currentPassword().value.set('');
        component['onSubmit']();
        expect(userServiceSpy.changePassword).not.toHaveBeenCalled();
    });
});

describe('ChangePasswordDialogComponent cancel and set password mode', () => {
    it('should not close dialog on cancel while submitting', () => {
        component['isSubmitting'].set(true);
        component['onCancel']();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('should close dialog on cancel when not submitting', () => {
        component['onCancel']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should use setPassword mode when account has no password', () => {
        TestBed.resetTestingModule();
        configureComponent({ hasPassword: false });

        expect(component['hasPassword']).toBe(false);

        userServiceSpy.setPassword.mockReturnValue(of(true));
        component['form'].newPassword().value.set('newPass123');
        component['form'].confirmPassword().value.set('newPass123');
        component['onSubmit']();

        expect(userServiceSpy.setPassword).toHaveBeenCalledWith({
            newPassword: 'newPass123',
        });
        expect(userServiceSpy.changePassword).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });
});

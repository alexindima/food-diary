import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ChangePasswordDialogComponent } from './change-password-dialog.component';
import { UserService } from '../../../../shared/api/user.service';

describe('ChangePasswordDialogComponent', () => {
    let component: ChangePasswordDialogComponent;
    let fixture: ComponentFixture<ChangePasswordDialogComponent>;
    let userServiceSpy: jasmine.SpyObj<UserService>;
    let dialogRefSpy: jasmine.SpyObj<MatDialogRef<ChangePasswordDialogComponent, boolean>>;
    let translateServiceSpy: TranslateService;

    beforeEach(() => {
        userServiceSpy = jasmine.createSpyObj('UserService', ['changePassword']);
        dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

        TestBed.configureTestingModule({
            imports: [ChangePasswordDialogComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: UserService, useValue: userServiceSpy },
                { provide: MatDialogRef, useValue: dialogRefSpy },
            ],
        });

        translateServiceSpy = TestBed.inject(TranslateService);
        spyOn(translateServiceSpy, 'instant').and.callFake(((key: string | string[]) => key as string) as any);

        fixture = TestBed.createComponent(ChangePasswordDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should validate required fields', () => {
        const { currentPassword, newPassword, confirmPassword } = component.form.controls;

        currentPassword.markAsTouched();
        newPassword.markAsTouched();
        confirmPassword.markAsTouched();

        expect(currentPassword.hasError('required')).toBeTrue();
        expect(newPassword.hasError('required')).toBeTrue();
        expect(confirmPassword.hasError('required')).toBeTrue();
    });

    it('should validate newPassword minimum length', () => {
        const control = component.form.controls.newPassword;
        control.setValue('abc');
        control.markAsTouched();
        expect(control.hasError('minlength')).toBeTrue();

        control.setValue('abcdef');
        expect(control.hasError('minlength')).toBeFalse();
    });

    it('should validate password match', () => {
        component.form.controls.newPassword.setValue('validPass1');
        component.form.controls.confirmPassword.setValue('differentPass');
        component.form.controls.confirmPassword.markAsTouched();

        expect(component.form.controls.confirmPassword.hasError('matchField')).toBeTrue();

        component.form.controls.confirmPassword.setValue('validPass1');
        expect(component.form.controls.confirmPassword.hasError('matchField')).toBeFalse();
    });

    it('should call changePassword on submit', () => {
        userServiceSpy.changePassword.and.returnValue(of(true));

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
        userServiceSpy.changePassword.and.returnValue(of(true));

        component.form.controls.currentPassword.setValue('oldPass');
        component.form.controls.newPassword.setValue('newPass123');
        component.form.controls.confirmPassword.setValue('newPass123');
        component.onSubmit();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
        expect(component.isSubmitting()).toBeFalse();
    });

    it('should handle error response from service returning false', () => {
        userServiceSpy.changePassword.and.returnValue(of(false));

        component.form.controls.currentPassword.setValue('oldPass');
        component.form.controls.newPassword.setValue('newPass123');
        component.form.controls.confirmPassword.setValue('newPass123');
        component.onSubmit();

        expect(dialogRefSpy.close).not.toHaveBeenCalled();
        expect(component.passwordError()).toBe('USER_MANAGE.CHANGE_PASSWORD_ERROR');
        expect(component.isSubmitting()).toBeFalse();
    });

    it('should handle error response from service throwing', () => {
        userServiceSpy.changePassword.and.returnValue(throwError(() => new Error('network error')));

        component.form.controls.currentPassword.setValue('oldPass');
        component.form.controls.newPassword.setValue('newPass123');
        component.form.controls.confirmPassword.setValue('newPass123');
        component.onSubmit();

        expect(component.passwordError()).toBe('USER_MANAGE.CHANGE_PASSWORD_ERROR');
        expect(component.isSubmitting()).toBeFalse();
    });

    it('should not submit when form is invalid', () => {
        component.form.controls.currentPassword.setValue('');
        component.onSubmit();
        expect(userServiceSpy.changePassword).not.toHaveBeenCalled();
    });

    it('should not close dialog on cancel while submitting', () => {
        component.isSubmitting.set(true);
        component.onCancel();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('should close dialog on cancel when not submitting', () => {
        component.onCancel();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });
});

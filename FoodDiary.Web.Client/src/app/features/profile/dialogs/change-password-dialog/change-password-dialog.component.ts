import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { UserService } from '../../../../shared/api/user.service';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { ChangePasswordRequest, SetPasswordRequest } from '../../../../shared/models/user.data';
import { matchFieldValidator } from '../../../../validators/match-field.validator';

export interface ChangePasswordDialogData {
    hasPassword?: boolean;
}

@Component({
    selector: 'fd-change-password-dialog',
    standalone: true,
    templateUrl: './change-password-dialog.component.html',
    styleUrls: ['./change-password-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslateModule,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
    ],
})
export class ChangePasswordDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ChangePasswordDialogComponent, boolean>);
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly data = inject<ChangePasswordDialogData | null>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    public readonly hasPassword = this.data.hasPassword ?? true;

    public readonly form = new FormGroup<ChangePasswordFormData>({
        currentPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: this.hasPassword ? [Validators.required] : [],
        }),
        newPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, Validators.minLength(6)],
        }),
        confirmPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, matchFieldValidator('newPassword')],
        }),
    });

    public readonly passwordError = signal<string | null>(null);
    public readonly isSubmitting = signal<boolean>(false);

    public onCancel(): void {
        if (this.isSubmitting()) {
            return;
        }

        this.dialogRef.close(false);
    }

    public onSubmit(): void {
        this.form.markAllAsTouched();
        if (this.form.invalid || this.isSubmitting()) {
            return;
        }

        const value = this.form.value;
        const currentPassword = value.currentPassword?.trim() ?? '';
        const newPassword = value.newPassword?.trim() ?? '';

        this.isSubmitting.set(true);
        this.passwordError.set(null);

        const request$ = this.hasPassword
            ? this.userService.changePassword({
                  currentPassword,
                  newPassword,
              } satisfies ChangePasswordRequest)
            : this.userService.setPassword({
                  newPassword,
              } satisfies SetPasswordRequest);

        request$.subscribe({
            next: success => {
                this.isSubmitting.set(false);
                if (success) {
                    this.dialogRef.close(true);
                    return;
                }

                this.setPasswordError(this.hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_ERROR' : 'USER_MANAGE.SET_PASSWORD_ERROR');
            },
            error: () => {
                this.isSubmitting.set(false);
                this.setPasswordError(this.hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_ERROR' : 'USER_MANAGE.SET_PASSWORD_ERROR');
            },
        });
    }

    public getControlError(controlName: keyof ChangePasswordFormValues): string | null {
        const control = this.form.controls[controlName];
        return this.resolveControlError(control);
    }

    private setPasswordError(key: string): void {
        this.passwordError.set(this.translateService.instant(key));
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (!control || !control.invalid || !control.touched) {
            return null;
        }

        if (control.errors?.['required']) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (control.errors?.['minlength']) {
            const requiredLength = control.errors['minlength'].requiredLength;
            return this.translateService.instant('FORM_ERRORS.PASSWORD.MIN_LENGTH', { requiredLength });
        }

        if (control.errors?.['matchField']) {
            return this.translateService.instant('FORM_ERRORS.PASSWORD.MATCH');
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }
}

interface ChangePasswordFormValues {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
}

type ChangePasswordFormData = FormGroupControls<ChangePasswordFormValues>;

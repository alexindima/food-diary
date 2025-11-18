import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FormGroupControls } from '../../../../types/common.data';
import { ChangePasswordRequest } from '../../../../types/user.data';
import { UserService } from '../../../../services/user.service';
import { matchFieldValidator } from '../../../../validators/match-field.validator';

@Component({
    selector: 'fd-change-password-dialog',
    standalone: true,
    templateUrl: './change-password-dialog.component.html',
    styleUrls: ['./change-password-dialog.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
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

    public readonly form = new FormGroup<ChangePasswordFormData>({
        currentPassword: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
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
        const payload: ChangePasswordRequest = {
            currentPassword: value.currentPassword?.trim() ?? '',
            newPassword: value.newPassword?.trim() ?? '',
        };

        this.isSubmitting.set(true);
        this.passwordError.set(null);

        this.userService.changePassword(payload).subscribe({
            next: success => {
                this.isSubmitting.set(false);
                if (success) {
                    this.dialogRef.close(true);
                    return;
                }
                this.setPasswordError('USER_MANAGE.CHANGE_PASSWORD_ERROR');
            },
            error: () => {
                this.isSubmitting.set(false);
                this.setPasswordError('USER_MANAGE.CHANGE_PASSWORD_ERROR');
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
            return this.translateService.instant('FORM_ERRORS.PASSWORD.MIN_LENGTH', {
                requiredLength,
            });
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

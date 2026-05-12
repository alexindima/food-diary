import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { EMPTY, merge, type Observable } from 'rxjs';

import { UserService } from '../../../../shared/api/user.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { getNumberProperty } from '../../../../shared/lib/unknown-value.utils';
import type { ChangePasswordRequest, SetPasswordRequest } from '../../../../shared/models/user.data';
import { matchFieldValidator } from '../../../../validators/match-field.validator';
import { AUTH_PASSWORD_MIN_LENGTH } from '../../../auth/lib/auth.constants';

export type ChangePasswordDialogData = {
    hasPassword?: boolean;
};

const ERROR_FIELDS = ['currentPassword', 'newPassword', 'confirmPassword'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

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
    private readonly destroyRef = inject(DestroyRef);
    private readonly data = inject<ChangePasswordDialogData | null>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    public readonly hasPassword = this.data.hasPassword ?? true;

    public readonly form = new FormGroup<ChangePasswordFormData>({
        currentPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: this.hasPassword ? [Validators.required] : [],
        }),
        newPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, Validators.minLength(AUTH_PASSWORD_MIN_LENGTH)],
        }),
        confirmPassword: new FormControl<string>('', {
            nonNullable: true,
            validators: [Validators.required, matchFieldValidator('newPassword')],
        }),
    });

    public readonly passwordError = signal<string | null>(null);
    public readonly isSubmitting = signal<boolean>(false);
    public readonly fieldErrors = signal<FieldErrors>(this.createEmptyFieldErrors());
    public readonly dialogCopyState = computed(() => ({
        titleKey: this.hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD' : 'USER_MANAGE.SET_PASSWORD',
        submitLabelKey: this.hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_SAVE' : 'USER_MANAGE.SET_PASSWORD_SAVE',
    }));

    public constructor() {
        const formEvents = (this.form as { events?: Observable<unknown> }).events ?? EMPTY;
        merge(formEvents, this.form.statusChanges, this.form.valueChanges, this.translateService.onLangChange)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.updateFieldErrors();
            });
        this.updateFieldErrors();
    }

    public onCancel(): void {
        if (this.isSubmitting()) {
            return;
        }

        this.dialogRef.close(false);
    }

    public onSubmit(): void {
        this.form.markAllAsTouched();
        this.updateFieldErrors();
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
                if (success === true) {
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

    private setPasswordError(key: string): void {
        this.passwordError.set(this.translateService.instant(key));
    }

    private updateFieldErrors(): void {
        this.fieldErrors.set(
            ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
                errors[field] = this.resolveControlError(this.form.controls[field]);
                return errors;
            }, this.createEmptyFieldErrors()),
        );
    }

    private createEmptyFieldErrors(): FieldErrors {
        return {
            currentPassword: null,
            newPassword: null,
            confirmPassword: null,
        };
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (control === null || !control.invalid || !control.touched) {
            return null;
        }

        if (control.errors?.['required'] !== undefined) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        const minLengthErrorMessage = this.resolveMinLengthError(control);
        if (minLengthErrorMessage !== null) {
            return minLengthErrorMessage;
        }

        if (control.errors?.['matchField'] !== undefined) {
            return this.translateService.instant('FORM_ERRORS.PASSWORD.MATCH');
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private resolveMinLengthError(control: AbstractControl): string | null {
        const minLengthError: unknown = control.getError('minlength');
        if (minLengthError === null || minLengthError === undefined) {
            return null;
        }

        const requiredLength = getNumberProperty(minLengthError, 'requiredLength') ?? AUTH_PASSWORD_MIN_LENGTH;
        return this.translateService.instant('FORM_ERRORS.PASSWORD.MIN_LENGTH', { requiredLength });
    }
}

type ChangePasswordFormValues = {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
};

type ChangePasswordFormData = FormGroupControls<ChangePasswordFormValues>;

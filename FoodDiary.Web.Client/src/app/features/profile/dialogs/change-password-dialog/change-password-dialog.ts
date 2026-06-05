import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, minLength, required, validate } from '@angular/forms/signals';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { UserFacade } from '../../../../shared/lib/user.facade';
import type { ChangePasswordRequest, SetPasswordRequest } from '../../../../shared/models/user.data';
import { AUTH_PASSWORD_MIN_LENGTH } from '../../../auth/lib/auth.constants';

export type ChangePasswordDialogData = {
    hasPassword?: boolean;
};

const ERROR_FIELDS = ['currentPassword', 'newPassword', 'confirmPassword'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

@Component({
    selector: 'fd-change-password-dialog',
    templateUrl: './change-password-dialog.html',
    styleUrls: ['./change-password-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormField, TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiInputComponent, FdUiButtonComponent],
})
export class ChangePasswordDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ChangePasswordDialogComponent, boolean>);
    private readonly userFacade = inject(UserFacade);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly data = inject<ChangePasswordDialogData | null>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    protected readonly hasPassword = this.data.hasPassword ?? true;

    protected readonly formModel = signal<ChangePasswordFormValues>({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
    });
    protected readonly form = form(this.formModel, path => {
        if (this.hasPassword) {
            required(path.currentPassword);
        }

        required(path.newPassword);
        minLength(path.newPassword, AUTH_PASSWORD_MIN_LENGTH);
        required(path.confirmPassword);
        validate(path.confirmPassword, ({ value }) => (value() === this.formModel().newPassword ? undefined : { kind: 'matchField' }));
    });

    protected readonly passwordError = signal<string | null>(null);
    protected readonly isSubmitting = signal<boolean>(false);
    private readonly languageVersion = signal(0);
    protected readonly fieldErrors = computed<FieldErrors>(() => {
        this.languageVersion();
        this.formModel();

        return ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
            errors[field] = resolveSignalFormFieldError(this.form[field], this.validationErrors, this.translateService);
            return errors;
        }, this.createEmptyFieldErrors());
    });
    protected readonly dialogCopyState = computed(() => ({
        titleKey: this.hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD' : 'USER_MANAGE.SET_PASSWORD',
        submitLabelKey: this.hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_SAVE' : 'USER_MANAGE.SET_PASSWORD_SAVE',
    }));

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected onCancel(): void {
        if (this.isSubmitting()) {
            return;
        }

        this.dialogRef.close(false);
    }

    protected onSubmit(): void {
        this.form().markAsTouched();
        if (this.form().invalid() || this.isSubmitting()) {
            return;
        }

        const value = this.formModel();
        const currentPassword = value.currentPassword.trim();
        const newPassword = value.newPassword.trim();

        this.isSubmitting.set(true);
        this.passwordError.set(null);

        const request$ = this.hasPassword
            ? this.userFacade.changePassword({
                  currentPassword,
                  newPassword,
              } satisfies ChangePasswordRequest)
            : this.userFacade.setPassword({
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

    private setPasswordError(key: string): void {
        this.passwordError.set(this.translateService.instant(key));
    }

    private createEmptyFieldErrors(): FieldErrors {
        return {
            currentPassword: null,
            newPassword: null,
            confirmPassword: null,
        };
    }
}

type ChangePasswordFormValues = {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
};

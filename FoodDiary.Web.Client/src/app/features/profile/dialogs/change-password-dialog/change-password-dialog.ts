import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { type FieldTree, form, FormField, minLength, required, validate, type ValidationError } from '@angular/forms/signals';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { type FdValidationErrorConfig, type FdValidationErrors, getNumberProperty } from 'fd-ui-kit/form-error/fd-ui-form-error';
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
const CHANGE_PASSWORD_VALIDATION_ERRORS: Partial<FdValidationErrors> = {
    required: () => 'FORM_ERRORS.REQUIRED',
    minlength: (error?: unknown) => ({
        key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
        params: { requiredLength: getNumberProperty(error, 'requiredLength') },
    }),
    matchField: () => 'FORM_ERRORS.PASSWORD.MATCH',
};

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
    protected readonly fieldErrors = signal<FieldErrors>(this.createEmptyFieldErrors());
    protected readonly dialogCopyState = computed(() => ({
        titleKey: this.hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD' : 'USER_MANAGE.SET_PASSWORD',
        submitLabelKey: this.hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_SAVE' : 'USER_MANAGE.SET_PASSWORD_SAVE',
    }));

    public constructor() {
        effect(() => {
            this.formModel();
            this.translateService.onLangChange;
            this.updateFieldErrors();
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
        this.updateFieldErrors();
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

    private updateFieldErrors(): void {
        this.fieldErrors.set(
            ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
                errors[field] = this.resolveTranslatedFieldError(this.form[field]);
                return errors;
            }, this.createEmptyFieldErrors()),
        );
    }

    private resolveTranslatedFieldError(field: FieldTree<unknown>): string | null {
        const state = field();
        if (!state.invalid() || (!state.touched() && !state.dirty())) {
            return null;
        }

        const error = state.errors()[0];
        const key = this.mapValidationErrorKey(error);
        const resolver = CHANGE_PASSWORD_VALIDATION_ERRORS[key];
        if (resolver === undefined) {
            return this.translateService.instant('FORM_ERRORS.UNKNOWN');
        }

        const params = this.getValidationParams(error);
        const result = resolver(params);
        return this.translateValidationResult(result, params);
    }

    private mapValidationErrorKey(error: ValidationError): string {
        return error.kind === 'minLength' ? 'minlength' : error.kind;
    }

    private getValidationParams(error: ValidationError): Record<string, unknown> {
        if (error.kind === 'minLength') {
            return { requiredLength: getNumberProperty(error, 'minLength') };
        }

        return {};
    }

    private translateValidationResult(result: FdValidationErrorConfig | string, params: Record<string, unknown>): string {
        if (typeof result === 'string') {
            return this.translateService.instant(result, params);
        }

        return this.translateService.instant(result.key, {
            ...params,
            ...result.params,
        });
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

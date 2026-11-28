import { CommonModule, DOCUMENT } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { form, FormField, maxLength, minLength, required, validate } from '@angular/forms/signals';
import { ActivatedRoute, type ParamMap } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiFormDirective } from 'fd-ui-kit/form/fd-ui-form.directive';
import {
    FD_VALIDATION_ERRORS,
    FdUiFormErrorComponent,
    type FdValidationErrors,
    resolveSignalFormFieldError,
} from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { AUTH_PASSWORD_MAX_LENGTH, AUTH_PASSWORD_MIN_LENGTH } from '../../../../shared/auth/auth.constants';
import { getNumberProperty } from '../../../../shared/lib/unknown-value.utils';
import { AuthDialogComponent } from '../../dialogs/auth-dialog/auth-dialog';
import { ConfirmPasswordResetRequest } from '../../models/auth.data';

type ResetState = 'ready' | 'invalid';
type ResetCredentials = Pick<ConfirmPasswordResetRequest, 'userId' | 'token'>;
const ERROR_FIELDS = ['password', 'confirmPassword'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

@Component({
    selector: 'fd-password-reset',
    imports: [
        CommonModule,
        FormField,
        FdUiFormDirective,
        TranslatePipe,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
    ],
    templateUrl: './password-reset.html',
    styleUrl: './password-reset.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordResetComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly document = inject(DOCUMENT);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });

    private readonly resetCredentials = signal<ResetCredentials | null>(null);
    protected readonly state = computed<ResetState>(() => (this.resetCredentials() === null ? 'invalid' : 'ready'));
    protected readonly passwordMaximumLength = AUTH_PASSWORD_MAX_LENGTH;
    protected readonly isSubmitting = signal(false);
    protected readonly errorMessage = signal<string | null>(null);
    private readonly languageChange = toSignal(this.translateService.onLangChange);
    protected readonly fieldErrors = computed<FieldErrors>(() => {
        this.languageChange();
        this.formModel();

        return ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
            errors[field] = resolveSignalFormFieldError(this.form[field], this.validationErrors, this.translateService);
            return errors;
        }, this.createEmptyFieldErrors());
    });
    protected readonly formModel = signal<PasswordResetFormValues>({
        password: '',
        confirmPassword: '',
    });
    private readonly submitPasswordResetFormAsync = async (): Promise<void> => {
        await this.submitAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            required(path.password);
            minLength(path.password, AUTH_PASSWORD_MIN_LENGTH);
            maxLength(path.password, AUTH_PASSWORD_MAX_LENGTH);
            required(path.confirmPassword);
            maxLength(path.confirmPassword, AUTH_PASSWORD_MAX_LENGTH);
            validate(path.confirmPassword, ({ value }) => (value() === this.formModel().password ? undefined : { kind: 'matchField' }));
        },
        {
            submission: {
                action: this.submitPasswordResetFormAsync,
            },
        },
    );

    public constructor() {
        this.resolveResetCredentials();
    }

    protected onSubmit(): void {
        void this.submitAsync();
    }

    private async submitAsync(): Promise<void> {
        this.form().markAsTouched();
        const credentials = this.resetCredentials();
        if (credentials === null || this.form().invalid() || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        this.errorMessage.set(null);

        const request = new ConfirmPasswordResetRequest({
            ...credentials,
            newPassword: this.formModel().password,
        });

        try {
            await firstValueFrom(this.authService.confirmPasswordReset(request).pipe(takeUntilDestroyed(this.destroyRef)));
            this.isSubmitting.set(false);
            await this.navigationService.navigateToHomeAsync();
        } catch (error: unknown) {
            this.isSubmitting.set(false);
            if (getNumberProperty(error, 'status') === HttpStatusCode.Unauthorized) {
                this.resetCredentials.set(null);
                this.formModel.set({ password: '', confirmPassword: '' });
                this.errorMessage.set(this.translateService.instant('AUTH.RESET.INVALID'));
            } else {
                this.errorMessage.set(this.translateService.instant('AUTH.RESET.ERROR_GENERIC'));
            }
        }
    }

    protected onBackToLogin(): void {
        void this.navigationService.navigateToAuthAsync('login');
    }

    protected requestNewLink(): void {
        this.dialogs.open(AuthDialogComponent, {
            preset: 'form',
            ariaLabel: this.translateService.instant('AUTH.DIALOG_TITLE'),
            data: { mode: 'login', requestPasswordReset: true },
        });
    }

    private createEmptyFieldErrors(): FieldErrors {
        return {
            password: null,
            confirmPassword: null,
        };
    }

    private resolveResetCredentials(): void {
        const fragmentParams = new URLSearchParams(this.document.location.hash.replace(/^#/, ''));
        const queryParams = this.route.snapshot.queryParamMap;
        const userId = fragmentParams.get('userId') ?? this.resolveLegacyUserId(queryParams);
        const token = fragmentParams.get('token') ?? queryParams.get('token');

        if (fragmentParams.has('token') || fragmentParams.has('userId')) {
            this.document.defaultView?.history.replaceState({}, '', this.document.location.pathname + this.document.location.search);
        }

        if (userId === null || token === null || userId.length === 0 || token.length === 0) {
            this.errorMessage.set(this.translateService.instant('AUTH.RESET.INVALID'));
            return;
        }
        this.resetCredentials.set({ userId, token });
    }

    private resolveLegacyUserId(queryParams: ParamMap): string | null {
        return queryParams.get('userId') ?? queryParams.get('user') ?? queryParams.get('id');
    }
}

type PasswordResetFormValues = {
    password: string;
    confirmPassword: string;
};

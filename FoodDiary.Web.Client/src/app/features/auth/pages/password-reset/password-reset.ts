import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, minLength, required, validate } from '@angular/forms/signals';
import { ActivatedRoute, type ParamMap } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
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
import { AUTH_PASSWORD_MIN_LENGTH } from '../../lib/auth.constants';
import { ConfirmPasswordResetRequest } from '../../models/auth.data';

type ResetState = 'ready' | 'invalid';
const ERROR_FIELDS = ['password', 'confirmPassword'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

@Component({
    selector: 'fd-password-reset',
    imports: [
        CommonModule,
        FormField,
        FormRoot,
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
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });

    protected readonly state = signal<ResetState>('ready');
    protected readonly isSubmitting = signal(false);
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly token = signal<{ userId: string | null; token: string | null }>({ userId: null, token: null });
    private readonly languageVersion = signal(0);
    protected readonly fieldErrors = computed<FieldErrors>(() => {
        this.languageVersion();
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
            required(path.confirmPassword);
            validate(path.confirmPassword, ({ value }) => (value() === this.formModel().password ? undefined : { kind: 'matchField' }));
        },
        {
            submission: {
                action: this.submitPasswordResetFormAsync,
            },
        },
    );

    public constructor() {
        this.resolveToken();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected onSubmit(): void {
        void this.submitAsync();
    }

    private async submitAsync(): Promise<void> {
        this.form().markAsTouched();
        if (this.state() !== 'ready' || this.form().invalid() || this.isSubmitting()) {
            return;
        }

        const { userId, token } = this.token();
        if (userId === null || userId.length === 0 || token === null || token.length === 0) {
            this.state.set('invalid');
            this.errorMessage.set(this.translateService.instant('AUTH.RESET.INVALID'));
            return;
        }

        this.isSubmitting.set(true);
        this.errorMessage.set(null);

        const request = new ConfirmPasswordResetRequest({
            userId,
            token,
            newPassword: this.formModel().password,
        });

        try {
            await firstValueFrom(this.authService.confirmPasswordReset(request).pipe(takeUntilDestroyed(this.destroyRef)));
            this.isSubmitting.set(false);
            await this.navigationService.navigateToHomeAsync();
        } catch {
            this.isSubmitting.set(false);
            this.errorMessage.set(this.translateService.instant('AUTH.RESET.ERROR_GENERIC'));
        }
    }

    protected onBackToLogin(): void {
        void this.navigationService.navigateToAuthAsync('login');
    }

    private createEmptyFieldErrors(): FieldErrors {
        return {
            password: null,
            confirmPassword: null,
        };
    }

    private resolveToken(): void {
        const fragmentParams = new URLSearchParams(this.document.location.hash.replace(/^#/, ''));
        const queryParams = this.route.snapshot.queryParamMap;
        const userId = fragmentParams.get('userId') ?? this.resolveLegacyUserId(queryParams);
        const token = fragmentParams.get('token') ?? queryParams.get('token');
        this.token.set({ userId, token });

        if (fragmentParams.has('token') || fragmentParams.has('userId')) {
            this.document.defaultView?.history.replaceState({}, '', this.document.location.pathname + this.document.location.search);
        }

        if (userId === null || userId.length === 0 || token === null || token.length === 0) {
            this.state.set('invalid');
            this.errorMessage.set(this.translateService.instant('AUTH.RESET.INVALID'));
        }
    }

    private resolveLegacyUserId(queryParams: ParamMap): string | null {
        return queryParams.get('userId') ?? queryParams.get('user') ?? queryParams.get('id');
    }
}

type PasswordResetFormValues = {
    password: string;
    confirmPassword: string;
};

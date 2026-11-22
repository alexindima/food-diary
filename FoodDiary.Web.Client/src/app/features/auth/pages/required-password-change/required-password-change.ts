import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { form, FormField, maxLength, minLength, required, validate } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiFormDirective } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { AUTH_PASSWORD_MAX_LENGTH, AUTH_PASSWORD_MIN_LENGTH } from '../../../../shared/auth/auth.constants';
import { UserFacade } from '../../../../shared/lib/user.facade';

type RequiredPasswordChangeFormModel = {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
};

@Component({
    selector: 'fd-required-password-change',
    imports: [FormField, FdUiFormDirective, TranslatePipe, FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent],
    templateUrl: './required-password-change.html',
    styleUrl: './required-password-change.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequiredPasswordChangeComponent {
    private readonly userFacade = inject(UserFacade);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);

    private changedPassword: string | null = null;
    private logoutRequested = false;
    protected readonly passwordMaximumLength = AUTH_PASSWORD_MAX_LENGTH;
    protected readonly isSubmitting = signal(false);
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly formModel = signal<RequiredPasswordChangeFormModel>({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
    });
    protected readonly form = form(this.formModel, path => {
        required(path.currentPassword);
        maxLength(path.currentPassword, AUTH_PASSWORD_MAX_LENGTH);
        required(path.newPassword);
        minLength(path.newPassword, AUTH_PASSWORD_MIN_LENGTH);
        maxLength(path.newPassword, AUTH_PASSWORD_MAX_LENGTH);
        required(path.confirmPassword);
        maxLength(path.confirmPassword, AUTH_PASSWORD_MAX_LENGTH);
        validate(path.confirmPassword, ({ value }) => (value() === this.formModel().newPassword ? undefined : { kind: 'matchField' }));
    });

    protected submit(): void {
        void this.submitAsync();
    }

    protected logout(): void {
        this.logoutRequested = true;
        this.changedPassword = null;
        void this.authService.onLogoutAsync();
    }

    private async submitAsync(): Promise<void> {
        this.form().markAsTouched();
        if (this.form().invalid() || this.isSubmitting() || !this.isPageActive()) {
            return;
        }

        this.isSubmitting.set(true);
        this.errorMessage.set(null);
        try {
            const password = this.changedPassword ?? (await this.persistPasswordAsync(this.formModel()));
            if (password === null || !this.isPageActive()) {
                return;
            }
            await this.authService.completeRequiredPasswordChangeAsync(password);
            if (this.isPageActive()) {
                await this.navigationService.navigateToHomeAsync();
            }
        } catch (error: unknown) {
            this.showSubmissionError(error, 'AUTH.REQUIRED_PASSWORD_CHANGE.SESSION_ERROR');
        } finally {
            this.isSubmitting.set(false);
        }
    }

    private async persistPasswordAsync(value: RequiredPasswordChangeFormModel): Promise<string | null> {
        try {
            const success = await firstValueFrom(
                this.userFacade.changePassword({
                    currentPassword: value.currentPassword,
                    newPassword: value.newPassword,
                }),
            );
            if (!this.isPageActive()) {
                return null;
            }
            if (!success) {
                this.errorMessage.set(this.translateService.instant('AUTH.REQUIRED_PASSWORD_CHANGE.ERROR'));
                return null;
            }
            this.changedPassword = value.newPassword;
            return value.newPassword;
        } catch (error: unknown) {
            this.showSubmissionError(error, 'AUTH.REQUIRED_PASSWORD_CHANGE.ERROR');
            return null;
        }
    }

    private showSubmissionError(error: unknown, fallbackKey: string): void {
        if (!this.isPageActive()) {
            return;
        }
        const key =
            error instanceof HttpErrorResponse && error.status === Number(HttpStatusCode.TooManyRequests)
                ? 'FORM_ERRORS.RATE_LIMITED'
                : fallbackKey;
        this.errorMessage.set(this.translateService.instant(key));
    }

    private isPageActive(): boolean {
        return !this.logoutRequested && !this.destroyRef.destroyed;
    }
}

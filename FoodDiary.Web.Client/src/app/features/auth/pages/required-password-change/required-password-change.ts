import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, minLength, required, validate } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { AUTH_PASSWORD_MIN_LENGTH } from '../../lib/auth.constants';

type RequiredPasswordChangeFormModel = {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
};

@Component({
    selector: 'fd-required-password-change',
    imports: [FormField, FormRoot, TranslatePipe, FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent],
    templateUrl: './required-password-change.html',
    styleUrl: './required-password-change.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequiredPasswordChangeComponent {
    private readonly userFacade = inject(UserFacade);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);

    protected readonly isSubmitting = signal(false);
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly formModel = signal<RequiredPasswordChangeFormModel>({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
    });
    protected readonly form = form(this.formModel, path => {
        required(path.currentPassword);
        required(path.newPassword);
        minLength(path.newPassword, AUTH_PASSWORD_MIN_LENGTH);
        required(path.confirmPassword);
        validate(path.confirmPassword, ({ value }) => (value() === this.formModel().newPassword ? undefined : { kind: 'matchField' }));
    });

    protected submit(): void {
        void this.submitAsync();
    }

    protected logout(): void {
        void this.authService.onLogoutAsync();
    }

    private async submitAsync(): Promise<void> {
        this.form().markAsTouched();
        if (this.form().invalid() || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        this.errorMessage.set(null);
        const value = this.formModel();
        const success = await firstValueFrom(
            this.userFacade.changePassword({
                currentPassword: value.currentPassword,
                newPassword: value.newPassword,
            }),
        );
        this.isSubmitting.set(false);
        if (!success) {
            this.errorMessage.set(this.translateService.instant('AUTH.REQUIRED_PASSWORD_CHANGE.ERROR'));
            return;
        }

        this.authService.completeRequiredPasswordChange();
        await this.navigationService.navigateToHomeAsync();
    }
}

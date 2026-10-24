import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { localizedPublicPath, publicSiteLocale } from '../../../../../config/public-site-paths';
import { AUTH_PASSWORD_MAX_LENGTH } from '../../../../../shared/auth/auth.constants';
import type { RegisterFieldErrors, RegisterFormValues } from '../auth-lib/auth.types';

@Component({
    selector: 'fd-auth-register-fields',
    imports: [FormField, TranslatePipe, FdUiCheckboxComponent, FdUiInputComponent],
    templateUrl: './auth-register-fields.html',
    styleUrl: '../auth.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthRegisterFieldsComponent {
    public readonly form = input.required<FieldTree<RegisterFormValues>>();
    public readonly errors = input.required<RegisterFieldErrors>();
    protected readonly passwordMaximumLength = AUTH_PASSWORD_MAX_LENGTH;
    protected readonly isPasswordVisible = signal(false);
    protected readonly passwordInputType = computed<'password' | 'text'>(() => (this.isPasswordVisible() ? 'text' : 'password'));
    protected readonly passwordToggleIcon = computed(() => (this.isPasswordVisible() ? 'visibility_off' : 'visibility'));
    protected readonly passwordToggleLabelKey = computed(() =>
        this.isPasswordVisible() ? 'AUTH.LOGIN.HIDE_PASSWORD' : 'AUTH.LOGIN.SHOW_PASSWORD',
    );
    private readonly router = inject(Router);
    protected readonly privacyUrl = localizedPublicPath('/privacy-policy', publicSiteLocale(this.router.url));

    protected togglePasswordVisibility(): void {
        this.isPasswordVisible.update(visible => !visible);
    }
}

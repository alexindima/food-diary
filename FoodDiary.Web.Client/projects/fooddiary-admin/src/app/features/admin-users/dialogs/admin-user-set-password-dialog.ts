import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, minLength, required, validate } from '@angular/forms/signals';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { firstValueFrom } from 'rxjs';

import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUser } from '../models/admin-user.models';

type AdminUserSetPasswordFormModel = {
    newPassword: string;
    confirmPassword: string;
};

const PASSWORD_MIN_LENGTH = 6;

@Component({
    selector: 'fd-admin-user-set-password-dialog',
    imports: [FormField, FormRoot, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiInputComponent],
    templateUrl: './admin-user-set-password-dialog.html',
    styleUrl: './admin-user-set-password-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserSetPasswordDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<AdminUserSetPasswordDialogComponent, boolean>>(FdUiDialogRef);
    private readonly user = inject<AdminUser>(FD_UI_DIALOG_DATA);
    private readonly usersService = inject(AdminUsersFacade);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly targetEmail = this.user.email;
    protected readonly isSubmitting = signal(false);
    protected readonly submitError = signal<string | null>(null);
    protected readonly formModel = signal<AdminUserSetPasswordFormModel>({
        newPassword: '',
        confirmPassword: '',
    });
    private readonly submitSetPasswordFormAsync = async (): Promise<void> => {
        await this.submitAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            required(path.newPassword);
            minLength(path.newPassword, PASSWORD_MIN_LENGTH);
            required(path.confirmPassword);
            validate(path.confirmPassword, ({ value }) => (value() === this.formModel().newPassword ? undefined : { kind: 'matchField' }));
        },
        {
            submission: {
                action: this.submitSetPasswordFormAsync,
            },
        },
    );

    protected readonly newPasswordError = computed(() => this.resolveNewPasswordError());
    protected readonly confirmPasswordError = computed(() => {
        const field = this.form.confirmPassword();
        if (!field.touched() && !field.dirty()) {
            return null;
        }

        const errors = field.errors();
        if (errors.some(error => error.kind === 'required')) {
            return 'Confirm password is required.';
        }

        if (errors.some(error => error.kind === 'matchField')) {
            return 'Passwords must match.';
        }

        return null;
    });
    protected readonly submitLabel = computed(() => (this.isSubmitting() ? 'Saving...' : 'Set password'));

    protected close(): void {
        if (this.isSubmitting()) {
            return;
        }

        this.dialogRef.close(false);
    }

    protected submit(): void {
        void this.submitAsync();
    }

    private async submitAsync(): Promise<void> {
        this.submitError.set(null);
        this.form().markAsTouched();
        if (this.form().invalid() || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        try {
            await firstValueFrom(
                this.usersService
                    .setPassword(this.user.id, { newPassword: this.formModel().newPassword.trim() })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.dialogRef.close(true);
        } catch {
            this.submitError.set('Could not set password. Please try again.');
            this.isSubmitting.set(false);
        }
    }

    private resolveNewPasswordError(): string | null {
        const field = this.form.newPassword();
        if (!field.touched() && !field.dirty()) {
            return null;
        }

        const errors = field.errors();
        if (errors.some(error => error.kind === 'required')) {
            return 'New password is required.';
        }

        if (errors.some(error => error.kind === 'minLength')) {
            return 'New password must be at least 6 characters.';
        }

        return null;
    }
}

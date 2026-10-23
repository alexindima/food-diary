import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { applyWhen, disabled, email, form, FormField, FormRoot, maxLength, required, validate } from '@angular/forms/signals';
import { FdUiCheckboxComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { firstValueFrom } from 'rxjs';

import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUserCreate, AdminUserCreation } from '../models/admin-user.models';

const MIN_PASSWORD_LENGTH = 6;
const MAX_PASSWORD_LENGTH = 256;

type AdminUserCreateFormModel = {
    email: string;
    firstName: string;
    lastName: string;
    language: 'en' | 'ru';
    roles: string[];
    generatePassword: boolean;
    temporaryPassword: string;
    isEmailConfirmed: boolean;
    sendCredentialsEmail: boolean;
    requirePasswordChange: boolean;
};

@Component({
    selector: 'fd-admin-user-create-dialog',
    imports: [
        FdUiSelectComponent,
        FdUiCheckboxComponent,
        FormField,
        FormRoot,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiInputComponent,
    ],
    templateUrl: './admin-user-create-dialog.html',
    styleUrl: './admin-user-create-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserCreateDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<AdminUserCreateDialogComponent, boolean>>(FdUiDialogRef);
    private readonly usersFacade = inject(AdminUsersFacade);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly availableRoles = ['Dietologist', 'Premium', 'Support', 'Admin'];
    protected readonly isSubmitting = signal(false);
    protected readonly submitError = signal<string | null>(null);
    protected readonly created = signal<AdminUserCreation | null>(null);
    protected readonly formModel = signal<AdminUserCreateFormModel>({
        email: '',
        firstName: '',
        lastName: '',
        language: 'ru',
        roles: [],
        generatePassword: true,
        temporaryPassword: '',
        isEmailConfirmed: true,
        sendCredentialsEmail: true,
        requirePasswordChange: true,
    });
    protected readonly form = form(this.formModel, path => {
        disabled(path, { when: () => this.isSubmitting() });
        required(path.email);
        email(path.email);
        applyWhen(
            path,
            ({ value }) => !value().generatePassword,
            manualPasswordPath => {
                required(manualPasswordPath.temporaryPassword);
                maxLength(manualPasswordPath.temporaryPassword, MAX_PASSWORD_LENGTH);
                validate(manualPasswordPath.temporaryPassword, ({ value }) =>
                    value().trim().length >= MIN_PASSWORD_LENGTH ? undefined : { kind: 'minLength' },
                );
            },
        );
    });
    protected readonly submitLabel = computed(() => (this.isSubmitting() ? 'Creating...' : 'Create user'));

    protected close(): void {
        if (!this.isSubmitting()) {
            this.dialogRef.close(this.created() !== null);
        }
    }

    protected toggleRole(role: string): void {
        if (this.isSubmitting()) {
            return;
        }
        this.formModel.update(value => ({
            ...value,
            roles: value.roles.includes(role) ? value.roles.filter(item => item !== role) : [...value.roles, role],
        }));
    }

    protected hasRole(role: string): boolean {
        return this.formModel().roles.includes(role);
    }

    protected onCredentialsOptionChange(field: 'sendCredentialsEmail' | 'requirePasswordChange', checked: boolean): void {
        if (this.isSubmitting()) {
            return;
        }
        this.formModel.update(value => {
            const updated = { ...value, [field]: checked };
            return { ...updated, requirePasswordChange: updated.sendCredentialsEmail || updated.requirePasswordChange };
        });
    }

    protected submit(): void {
        void this.submitAsync();
    }

    private async submitAsync(): Promise<void> {
        this.form().markAsTouched();
        const value = this.formModel();
        if (this.form().invalid() || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        this.submitError.set(null);
        try {
            const firstName = value.firstName.trim();
            const lastName = value.lastName.trim();
            const payload: AdminUserCreate = {
                email: value.email.trim(),
                firstName: firstName.length > 0 ? firstName : null,
                lastName: lastName.length > 0 ? lastName : null,
                language: value.language,
                roles: value.roles,
                temporaryPassword: value.generatePassword ? null : value.temporaryPassword,
                generatePassword: value.generatePassword,
                isEmailConfirmed: value.isEmailConfirmed,
                sendCredentialsEmail: value.sendCredentialsEmail,
                requirePasswordChange: value.requirePasswordChange,
            };
            const result = await firstValueFrom(this.usersFacade.createUser(payload).pipe(takeUntilDestroyed(this.destroyRef)));
            this.created.set(result);
        } catch {
            this.submitError.set('Could not create the user. Check the email and try again.');
        } finally {
            this.isSubmitting.set(false);
        }
    }
}

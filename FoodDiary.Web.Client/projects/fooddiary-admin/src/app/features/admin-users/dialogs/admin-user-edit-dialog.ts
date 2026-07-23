import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUser, AdminUserUpdate } from '../models/admin-user.models';

type AdminUserFormModel = {
    isActive: boolean;
    isEmailConfirmed: boolean;
    roles: string[];
    language: 'en' | 'ru';
};

@Component({
    selector: 'fd-admin-user-edit-dialog',
    imports: [CommonModule, FormField, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective],
    templateUrl: './admin-user-edit-dialog.html',
    styleUrl: './admin-user-edit-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserEditDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<AdminUserEditDialogComponent, boolean>>(FdUiDialogRef);
    private readonly data = inject<AdminUser>(FD_UI_DIALOG_DATA);
    private readonly usersService = inject(AdminUsersFacade);

    protected readonly roles = ['Admin', 'Premium', 'Support', 'Dietologist'];
    protected readonly isSaving = signal(false);
    protected readonly languages: ReadonlyArray<{ value: 'en' | 'ru'; label: string }> = [
        { value: 'en', label: 'English' },
        { value: 'ru', label: 'Russian' },
    ];
    protected readonly formModel = signal<AdminUserFormModel>({
        isActive: this.data.isActive,
        isEmailConfirmed: this.data.isEmailConfirmed,
        roles: this.data.roles,
        language: this.normalizeLanguage(this.data.language) ?? 'en',
    });
    protected readonly form = form(this.formModel);

    protected readonly user = this.data;

    protected close(): void {
        this.dialogRef.close(false);
    }

    protected save(): void {
        if (this.isSaving()) {
            return;
        }

        const value = this.formModel();
        const payload: AdminUserUpdate = {
            isActive: value.isActive,
            isEmailConfirmed: value.isEmailConfirmed,
            roles: value.roles,
        };
        const currentLanguage = this.normalizeLanguage(this.user.language) ?? 'en';
        if (value.language !== currentLanguage) {
            payload.language = value.language;
        }

        this.isSaving.set(true);
        this.usersService.updateUser(this.user.id, payload).subscribe({
            next: () => {
                this.dialogRef.close(true);
            },
            error: () => {
                this.isSaving.set(false);
                this.dialogRef.close(false);
            },
        });
    }

    protected toggleRole(role: string): void {
        const current = this.formModel().roles;
        const hasRole = current.includes(role);
        const next = hasRole ? current.filter(item => item !== role) : [...current, role];
        this.formModel.update(value => ({ ...value, roles: next }));
    }

    protected hasRole(role: string): boolean {
        return this.formModel().roles.includes(role);
    }

    private normalizeLanguage(value: string | null | undefined): 'en' | 'ru' | null {
        if (value === null || value === undefined || value.trim().length === 0) {
            return null;
        }

        const normalized = value.trim().toLowerCase();
        if (normalized === 'en' || normalized.startsWith('en-')) {
            return 'en';
        }

        if (normalized === 'ru' || normalized.startsWith('ru-')) {
            return 'ru';
        }

        return null;
    }
}

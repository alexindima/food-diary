import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, type FormControl, type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { type AdminUser, AdminUsersService, type AdminUserUpdate } from '../api/admin-users.service';

type AdminUserForm = {
    isActive: FormControl<boolean>;
    isEmailConfirmed: FormControl<boolean>;
    roles: FormControl<string[]>;
    language: FormControl<'en' | 'ru'>;
};

@Component({
    selector: 'fd-admin-user-edit-dialog',
    imports: [CommonModule, ReactiveFormsModule, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective],
    templateUrl: './admin-user-edit-dialog.html',
    styleUrl: './admin-user-edit-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserEditDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<AdminUserEditDialogComponent, boolean>>(FdUiDialogRef);
    private readonly data = inject<AdminUser>(FD_UI_DIALOG_DATA);
    private readonly usersService = inject(AdminUsersService);
    private readonly fb = inject(FormBuilder);

    protected readonly roles = ['Admin', 'Premium', 'Support'];
    protected readonly isSaving = signal(false);
    protected readonly languages: ReadonlyArray<{ value: 'en' | 'ru'; label: string }> = [
        { value: 'en', label: 'English' },
        { value: 'ru', label: 'Russian' },
    ];
    protected readonly form: FormGroup<AdminUserForm> = this.fb.group({
        isActive: this.fb.nonNullable.control(this.data.isActive),
        isEmailConfirmed: this.fb.nonNullable.control(this.data.isEmailConfirmed),
        roles: this.fb.nonNullable.control(this.data.roles),
        language: this.fb.nonNullable.control(this.normalizeLanguage(this.data.language) ?? 'en'),
    });

    protected readonly user = this.data;

    protected close(): void {
        this.dialogRef.close(false);
    }

    protected save(): void {
        if (this.isSaving()) {
            return;
        }

        const value = this.form.getRawValue();
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
        const current = this.form.controls.roles.value;
        const hasRole = current.includes(role);
        const next = hasRole ? current.filter(item => item !== role) : [...current, role];
        this.form.controls.roles.setValue(next);
    }

    protected hasRole(role: string): boolean {
        return this.form.controls.roles.value.includes(role);
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

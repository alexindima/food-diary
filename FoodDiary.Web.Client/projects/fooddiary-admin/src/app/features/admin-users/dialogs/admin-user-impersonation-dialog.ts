import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength, minLength, required } from '@angular/forms/signals';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { firstValueFrom } from 'rxjs';

import { ADMIN_USER_IMPERSONATION_REASON_MAX_LENGTH } from '../lib/admin-user.constants';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminImpersonationStart, AdminUser } from '../models/admin-user.models';

type AdminUserImpersonationFormModel = {
    reason: string;
};

const REASON_MIN_LENGTH = 10;

@Component({
    selector: 'fd-admin-user-impersonation-dialog',
    imports: [FormField, FormRoot, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiTextareaComponent],
    templateUrl: './admin-user-impersonation-dialog.html',
    styleUrl: './admin-user-impersonation-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserImpersonationDialogComponent {
    private readonly dialogRef =
        inject<FdUiDialogRef<AdminUserImpersonationDialogComponent, AdminImpersonationStart | null>>(FdUiDialogRef);
    private readonly user = inject<AdminUser>(FD_UI_DIALOG_DATA);
    private readonly usersService = inject(AdminUsersFacade);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly targetEmail = this.user.email;
    protected readonly isSubmitting = signal(false);
    protected readonly submitError = signal<string | null>(null);
    protected readonly formModel = signal<AdminUserImpersonationFormModel>({
        reason: '',
    });
    private readonly submitImpersonationFormAsync = async (): Promise<void> => {
        await this.submitAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            required(path.reason);
            minLength(path.reason, REASON_MIN_LENGTH);
            maxLength(path.reason, ADMIN_USER_IMPERSONATION_REASON_MAX_LENGTH);
        },
        {
            submission: {
                action: this.submitImpersonationFormAsync,
            },
        },
    );

    protected readonly reasonError = computed((): string | null => {
        const state = this.form.reason();
        if (!state.touched() && !state.dirty()) {
            return null;
        }

        const errors = state.errors();
        if (errors.some(error => error.kind === 'required')) {
            return 'Reason is required.';
        }

        if (errors.some(error => error.kind === 'minLength')) {
            return 'Reason must be at least 10 characters.';
        }

        if (errors.some(error => error.kind === 'maxLength')) {
            return 'Reason must be 500 characters or fewer.';
        }

        return null;
    });

    protected readonly submitLabel = computed(() => (this.isSubmitting() ? 'Starting...' : 'Start'));

    protected close(): void {
        this.dialogRef.close(null);
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
            const response = await firstValueFrom(
                this.usersService
                    .startImpersonation(this.user.id, this.formModel().reason.trim())
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.dialogRef.close(response);
        } catch {
            this.submitError.set('Could not start impersonation. Please try again.');
            this.isSubmitting.set(false);
        }
    }
}

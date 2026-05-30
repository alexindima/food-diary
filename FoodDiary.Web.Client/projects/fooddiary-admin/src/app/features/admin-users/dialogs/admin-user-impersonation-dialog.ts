import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, type FormControl, type FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { merge } from 'rxjs';

import { ADMIN_USER_IMPERSONATION_REASON_MAX_LENGTH } from '../lib/admin-user.constants';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminImpersonationStart, AdminUser } from '../models/admin-user.models';

type AdminUserImpersonationForm = {
    reason: FormControl<string>;
};

const REASON_MIN_LENGTH = 10;

@Component({
    selector: 'fd-admin-user-impersonation-dialog',
    imports: [ReactiveFormsModule, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiTextareaComponent],
    templateUrl: './admin-user-impersonation-dialog.html',
    styleUrl: './admin-user-impersonation-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserImpersonationDialogComponent {
    private readonly dialogRef =
        inject<FdUiDialogRef<AdminUserImpersonationDialogComponent, AdminImpersonationStart | null>>(FdUiDialogRef);
    private readonly user = inject<AdminUser>(FD_UI_DIALOG_DATA);
    private readonly usersService = inject(AdminUsersFacade);
    private readonly fb = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly targetEmail = this.user.email;
    protected readonly isSubmitting = signal(false);
    protected readonly submitError = signal<string | null>(null);
    protected readonly form: FormGroup<AdminUserImpersonationForm> = this.fb.group({
        reason: this.fb.nonNullable.control('', [
            Validators.required,
            Validators.minLength(REASON_MIN_LENGTH),
            Validators.maxLength(ADMIN_USER_IMPERSONATION_REASON_MAX_LENGTH),
        ]),
    });
    private readonly reasonValidationVersion = signal(0);

    protected readonly reasonError = computed((): string | null => {
        this.reasonValidationVersion();

        const control = this.form.controls.reason;
        if (!control.touched && !control.dirty) {
            return null;
        }

        if (control.hasError('required')) {
            return 'Reason is required.';
        }

        if (control.hasError('minlength')) {
            return 'Reason must be at least 10 characters.';
        }

        if (control.hasError('maxlength')) {
            return 'Reason must be 500 characters or fewer.';
        }

        return null;
    });

    protected readonly submitLabel = computed(() => (this.isSubmitting() ? 'Starting...' : 'Start'));

    public constructor() {
        const reason = this.form.controls.reason;
        merge(reason.statusChanges, reason.valueChanges)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.refreshReasonValidation();
            });
    }

    protected close(): void {
        this.dialogRef.close(null);
    }

    protected submit(): void {
        this.submitError.set(null);
        this.form.markAllAsTouched();
        this.refreshReasonValidation();
        if (this.form.invalid || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        this.usersService
            .startImpersonation(this.user.id, this.form.controls.reason.value.trim())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.dialogRef.close(response);
                },
                error: () => {
                    this.submitError.set('Could not start impersonation. Please try again.');
                    this.isSubmitting.set(false);
                },
            });
    }

    private refreshReasonValidation(): void {
        this.reasonValidationVersion.update(version => version + 1);
    }
}

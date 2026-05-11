import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, type FormControl, type FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { merge } from 'rxjs';

import { type AdminImpersonationStart, type AdminUser, AdminUsersService } from '../api/admin-users.service';

type AdminUserImpersonationForm = {
    reason: FormControl<string>;
};

const REASON_MIN_LENGTH = 10;
const REASON_MAX_LENGTH = 500;

@Component({
    selector: 'fd-admin-user-impersonation-dialog',
    standalone: true,
    imports: [ReactiveFormsModule, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiTextareaComponent],
    templateUrl: './admin-user-impersonation-dialog.component.html',
    styleUrl: './admin-user-impersonation-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserImpersonationDialogComponent {
    private readonly dialogRef =
        inject<FdUiDialogRef<AdminUserImpersonationDialogComponent, AdminImpersonationStart | null>>(FdUiDialogRef);
    private readonly user = inject<AdminUser>(FD_UI_DIALOG_DATA);
    private readonly usersService = inject(AdminUsersService);
    private readonly fb = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);

    public readonly targetEmail = this.user.email;
    public readonly isSubmitting = signal(false);
    public readonly submitError = signal<string | null>(null);
    public readonly form: FormGroup<AdminUserImpersonationForm> = this.fb.group({
        reason: this.fb.nonNullable.control('', [
            Validators.required,
            Validators.minLength(REASON_MIN_LENGTH),
            Validators.maxLength(REASON_MAX_LENGTH),
        ]),
    });
    private readonly reasonValidationVersion = signal(0);

    public readonly reasonError = computed((): string | null => {
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

    public readonly submitLabel = computed(() => (this.isSubmitting() ? 'Starting...' : 'Start'));

    public constructor() {
        const reason = this.form.controls.reason;
        merge(reason.statusChanges, reason.valueChanges)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.refreshReasonValidation();
            });
    }

    public close(): void {
        this.dialogRef.close(null);
    }

    public submit(): void {
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

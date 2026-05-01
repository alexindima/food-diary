import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';

import { AdminImpersonationStart, AdminUser, AdminUsersService } from '../api/admin-users.service';

type AdminUserImpersonationForm = {
    reason: FormControl<string>;
};

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
        reason: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]),
    });

    public get reasonError(): string | null {
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
    }

    public close(): void {
        this.dialogRef.close(null);
    }

    public submit(): void {
        this.submitError.set(null);
        this.form.markAllAsTouched();
        if (this.form.invalid || this.isSubmitting()) {
            return;
        }

        this.isSubmitting.set(true);
        this.usersService
            .startImpersonation(this.user.id, this.form.controls.reason.value.trim())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => this.dialogRef.close(response),
                error: () => {
                    this.submitError.set('Could not start impersonation. Please try again.');
                    this.isSubmitting.set(false);
                },
            });
    }
}

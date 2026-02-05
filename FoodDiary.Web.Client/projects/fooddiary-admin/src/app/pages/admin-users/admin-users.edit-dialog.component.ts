import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { AdminUser, AdminUsersService } from '../../services/admin-users.service';

type AdminUserForm = {
  isActive: FormControl<boolean>;
  roles: FormControl<string[]>;
};

@Component({
  selector: 'fd-admin-user-edit-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective],
  templateUrl: './admin-users.edit-dialog.component.html',
  styleUrl: './admin-users.edit-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserEditDialogComponent {
  private readonly dialogRef = inject<MatDialogRef<AdminUserEditDialogComponent, boolean>>(MatDialogRef);
  private readonly data = inject<AdminUser>(MAT_DIALOG_DATA);
  private readonly usersService = inject(AdminUsersService);
  private readonly fb = inject(FormBuilder);

  public readonly roles = ['Admin', 'Premium', 'Support'];
  public readonly form: FormGroup<AdminUserForm> = this.fb.group({
    isActive: this.fb.nonNullable.control(this.data.isActive),
    roles: this.fb.nonNullable.control(this.data.roles ?? []),
  });

  public readonly user = this.data;

  public close(): void {
    this.dialogRef.close(false);
  }

  public save(): void {
    const value = this.form.getRawValue();
    this.usersService
      .updateUser(this.user.id, { isActive: value.isActive, roles: value.roles })
      .subscribe({
        next: () => this.dialogRef.close(true),
        error: () => this.dialogRef.close(false),
      });
  }

  public toggleRole(role: string): void {
    const current = this.form.controls.roles.value ?? [];
    const hasRole = current.includes(role);
    const next = hasRole ? current.filter(item => item !== role) : [...current, role];
    this.form.controls.roles.setValue(next);
  }

  public hasRole(role: string): boolean {
    return (this.form.controls.roles.value ?? []).includes(role);
  }
}

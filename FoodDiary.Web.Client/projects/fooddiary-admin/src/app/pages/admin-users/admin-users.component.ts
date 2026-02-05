import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AdminUsersService, AdminUser } from '../../services/admin-users.service';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AdminUserEditDialogComponent } from './admin-users.edit-dialog.component';

@Component({
  selector: 'fd-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, FdUiButtonComponent, FdUiInputComponent],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersComponent {
  private readonly usersService = inject(AdminUsersService);
  private readonly dialogService = inject(FdUiDialogService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly users = signal<AdminUser[]>([]);
  public readonly totalPages = signal(1);
  public readonly totalItems = signal(0);
  public readonly page = signal(1);
  public readonly limit = 20;
  public readonly isLoading = signal(false);
  public readonly search = signal('');
  public readonly includeDeleted = signal(false);

  public constructor() {
    this.loadUsers();
  }

  public loadUsers(): void {
    this.isLoading.set(true);
    this.usersService
      .getUsers(this.page(), this.limit, this.search().trim() || null, this.includeDeleted())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: response => {
          this.users.set(response.items);
          this.totalPages.set(response.totalPages);
          this.totalItems.set(response.totalItems);
          this.isLoading.set(false);
        },
        error: () => {
          this.users.set([]);
          this.totalPages.set(1);
          this.totalItems.set(0);
          this.isLoading.set(false);
        },
      });
  }

  public onSearchChange(value: string): void {
    this.search.set(value);
    this.page.set(1);
    this.loadUsers();
  }

  public toggleIncludeDeleted(): void {
    this.includeDeleted.set(!this.includeDeleted());
    this.page.set(1);
    this.loadUsers();
  }

  public goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }
    this.page.set(page);
    this.loadUsers();
  }

  public openEdit(user: AdminUser): void {
    this.dialogService
      .open(AdminUserEditDialogComponent, {
        size: 'sm',
        data: user,
      })
      .afterClosed()
      .subscribe(updated => {
        if (updated) {
          this.loadUsers();
        }
      });
  }
}

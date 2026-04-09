import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { MatIconModule } from '@angular/material/icon';
import { NotificationItem, NotificationService } from '../../../services/notification.service';

@Component({
    selector: 'fd-notifications-dialog',
    standalone: true,
    imports: [DatePipe, TranslateModule, FdUiButtonComponent, FdUiDialogFooterDirective, FdUiDialogShellComponent, MatIconModule],
    templateUrl: './notifications-dialog.component.html',
    styleUrl: './notifications-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDialogComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject<FdUiDialogRef<NotificationsDialogComponent, void>>(FdUiDialogRef);
    private readonly notificationService = inject(NotificationService);

    protected readonly notifications = signal<NotificationItem[]>([]);
    protected readonly isLoading = signal(true);
    protected readonly isMarkingAllRead = signal(false);

    public constructor() {
        this.loadNotifications();
    }

    protected close(): void {
        this.dialogRef.close();
    }

    protected markAsRead(notification: NotificationItem): void {
        if (notification.isRead) {
            return;
        }

        this.notificationService
            .markAsRead(notification.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.notifications.update(items => items.map(item => (item.id === notification.id ? { ...item, isRead: true } : item)));
                },
            });
    }

    protected markAllAsRead(): void {
        if (!this.hasUnreadNotifications() || this.isMarkingAllRead()) {
            return;
        }

        this.isMarkingAllRead.set(true);
        this.notificationService
            .markAllRead()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.notifications.update(items => items.map(item => ({ ...item, isRead: true })));
                    this.isMarkingAllRead.set(false);
                },
                error: () => {
                    this.isMarkingAllRead.set(false);
                },
            });
    }

    protected hasUnreadNotifications(): boolean {
        return this.notifications().some(item => !item.isRead);
    }

    private loadNotifications(): void {
        this.isLoading.set(true);
        this.notificationService
            .getNotifications()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: notifications => {
                    this.notifications.set(notifications);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.notifications.set([]);
                    this.isLoading.set(false);
                },
            });
    }
}

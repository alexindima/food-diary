import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { NotificationItem, NotificationService } from '../../../services/notification.service';

@Component({
    selector: 'fd-notifications-dialog',
    standalone: true,
    imports: [DatePipe, TranslateModule, FdUiButtonComponent, FdUiIconComponent, FdUiDialogFooterDirective, FdUiDialogShellComponent],
    templateUrl: './notifications-dialog.component.html',
    styleUrl: './notifications-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDialogComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject<FdUiDialogRef<NotificationsDialogComponent, void>>(FdUiDialogRef);
    private readonly notificationService = inject(NotificationService);
    private readonly router = inject(Router);

    protected readonly notifications = this.notificationService.notifications;
    protected readonly isLoading = this.notificationService.notificationsLoading;
    protected readonly isMarkingAllRead = signal(false);

    public constructor() {
        this.notificationService.ensureNotificationsLoaded();
    }

    protected close(): void {
        this.dialogRef.close();
    }

    protected openNotification(notification: NotificationItem): void {
        const navigate = (): void => {
            if (!notification.targetUrl) {
                return;
            }

            void this.router.navigateByUrl(notification.targetUrl);
            this.close();
        };

        if (notification.isRead) {
            navigate();
            return;
        }

        this.notificationService
            .markAsRead(notification.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => navigate(),
                error: () => navigate(),
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

    protected isDietologistInvitation(notification: NotificationItem): boolean {
        return notification.type === 'DietologistInvitationReceived';
    }

    protected getNotificationIcon(notification: NotificationItem): string {
        return this.isDietologistInvitation(notification) ? 'medical_information' : 'notifications';
    }
}

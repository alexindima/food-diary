import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { type NotificationItem, NotificationService } from '../../../services/notification.service';

@Component({
    selector: 'fd-notifications-dialog',
    standalone: true,
    imports: [DatePipe, TranslateModule, FdUiButtonComponent, FdUiIconComponent, FdUiDialogFooterDirective, FdUiDialogComponent],
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

    protected openNotification(notification: NotificationItem): void {
        const navigate = (): void => {
            if (!notification.targetUrl) {
                return;
            }

            void this.router.navigateByUrl(notification.targetUrl);
            this.dialogRef.close();
        };

        if (notification.isRead) {
            navigate();
            return;
        }

        this.notificationService
            .markAsRead(notification.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    navigate();
                },
                error: () => {
                    navigate();
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

    protected isPasswordSetupSuggestion(notification: NotificationItem): boolean {
        return notification.type === 'PasswordSetupSuggested';
    }

    protected isDietologistInvitation(notification: NotificationItem): boolean {
        return notification.type === 'DietologistInvitationReceived';
    }

    protected getNotificationIcon(notification: NotificationItem): string {
        if (this.isDietologistInvitation(notification)) {
            return 'medical_information';
        }

        if (this.isPasswordSetupSuggestion(notification)) {
            return 'password';
        }

        return 'notifications';
    }

    protected hasNotificationAccentIcon(notification: NotificationItem): boolean {
        return this.isDietologistInvitation(notification) || this.isPasswordSetupSuggestion(notification);
    }

    protected getNotificationBadgeKey(notification: NotificationItem): string | null {
        if (this.isDietologistInvitation(notification)) {
            return 'NOTIFICATIONS.DIETOLOGIST_INVITATION_BADGE';
        }

        if (this.isPasswordSetupSuggestion(notification)) {
            return 'NOTIFICATIONS.PASSWORD_SETUP_BADGE';
        }

        return null;
    }

    protected getNotificationActionKey(notification: NotificationItem): string | null {
        if (this.isDietologistInvitation(notification)) {
            return 'NOTIFICATIONS.DIETOLOGIST_INVITATION_ACTION';
        }

        if (this.isPasswordSetupSuggestion(notification)) {
            return 'NOTIFICATIONS.PASSWORD_SETUP_ACTION';
        }

        return null;
    }

    protected getNotificationAriaLabel(notification: NotificationItem): string {
        const parts = [notification.title?.trim(), notification.body?.trim()].filter(Boolean);
        return parts.join('. ');
    }
}

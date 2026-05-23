import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { type NotificationItem, NotificationService } from '../../../services/notification.service';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import type { NotificationViewModel } from './notifications-dialog-lib/notifications-dialog.types';
import { NotificationsDialogListComponent } from './notifications-dialog-list/notifications-dialog-list.component';

@Component({
    selector: 'fd-notifications-dialog',
    imports: [
        TranslateModule,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiDialogFooterDirective,
        FdUiDialogComponent,
        NotificationsDialogListComponent,
    ],
    templateUrl: './notifications-dialog.component.html',
    styleUrl: './notifications-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDialogComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject<FdUiDialogRef<NotificationsDialogComponent, void>>(FdUiDialogRef);
    private readonly notificationService = inject(NotificationService);
    private readonly translateService = inject(TranslateService);
    private readonly router = inject(Router);
    private readonly languageVersion = signal(0);

    protected readonly notifications = this.notificationService.notifications;
    protected readonly isLoading = this.notificationService.notificationsLoading;
    protected readonly isMarkingAllRead = signal(false);
    protected readonly hasUnreadNotifications = computed(() => this.notifications().some(item => !item.isRead));
    protected readonly notificationItems = computed<NotificationViewModel[]>(() =>
        this.notifications().map(notification => this.buildNotificationViewModel(notification)),
    );

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
        this.notificationService.ensureNotificationsLoaded();
    }

    protected openNotification(notification: NotificationItem): void {
        const navigate = (): void => {
            if (notification.targetUrl === null || notification.targetUrl.trim().length === 0) {
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

    private buildNotificationViewModel(notification: NotificationItem): NotificationViewModel {
        this.languageVersion();
        const isDietologistInvitation = notification.type === 'DietologistInvitationReceived';
        const isDietologistRecommendation = notification.type === 'NewRecommendation';
        const isPasswordSetupSuggestion = notification.type === 'PasswordSetupSuggested';
        const hasAccentIcon = isDietologistInvitation || isDietologistRecommendation || isPasswordSetupSuggestion;

        return {
            notification,
            isDietologistInvitation,
            isDietologistRecommendation,
            isPasswordSetupSuggestion,
            hasAccentIcon,
            icon: this.resolveNotificationIcon(notification.type),
            badgeKey: this.resolveNotificationBadgeKey(notification.type),
            actionKey: this.resolveNotificationActionKey(notification.type),
            ariaLabel: [notification.title.trim(), notification.body?.trim()].filter(Boolean).join('. '),
            dateLabel: this.formatDateTime(notification.createdAtUtc),
        };
    }

    private formatDateTime(value: string): string {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
        }

        return new Intl.DateTimeFormat(this.resolveLocale(), {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        }).format(date);
    }

    private resolveLocale(): string {
        return resolveAppLocale(this.translateService.getCurrentLang());
    }

    private resolveNotificationIcon(type: string): string {
        if (type === 'DietologistInvitationReceived' || type === 'NewRecommendation') {
            return 'medical_information';
        }

        if (type === 'PasswordSetupSuggested') {
            return 'password';
        }

        return 'notifications';
    }

    private resolveNotificationBadgeKey(type: string): string | null {
        if (type === 'DietologistInvitationReceived') {
            return 'NOTIFICATIONS.DIETOLOGIST_INVITATION_BADGE';
        }

        if (type === 'NewRecommendation') {
            return 'NOTIFICATIONS.RECOMMENDATION_BADGE';
        }

        if (type === 'PasswordSetupSuggested') {
            return 'NOTIFICATIONS.PASSWORD_SETUP_BADGE';
        }

        return null;
    }

    private resolveNotificationActionKey(type: string): string | null {
        if (type === 'DietologistInvitationReceived') {
            return 'NOTIFICATIONS.DIETOLOGIST_INVITATION_ACTION';
        }

        if (type === 'NewRecommendation') {
            return 'NOTIFICATIONS.RECOMMENDATION_ACTION';
        }

        if (type === 'PasswordSetupSuggested') {
            return 'NOTIFICATIONS.PASSWORD_SETUP_ACTION';
        }

        return null;
    }
}

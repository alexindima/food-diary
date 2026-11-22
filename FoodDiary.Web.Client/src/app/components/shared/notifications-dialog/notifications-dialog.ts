import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationStart, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { filter } from 'rxjs';

import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { type NotificationItem, NotificationService } from '../../../shared/notifications/notification.service';
import { notificationPresentation } from './notifications-dialog-lib/notification-presentation';
import type { NotificationViewModel } from './notifications-dialog-lib/notifications-dialog.types';
import { NotificationsDialogListComponent } from './notifications-dialog-list/notifications-dialog-list';

@Component({
    selector: 'fd-notifications-dialog',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiDialogFooterDirective,
        FdUiDialogComponent,
        NotificationsDialogListComponent,
    ],
    templateUrl: './notifications-dialog.html',
    styleUrl: './notifications-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDialogComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject<FdUiDialogRef<NotificationsDialogComponent, void>>(FdUiDialogRef);
    private readonly notificationService = inject(NotificationService);
    private readonly translateService = inject(TranslateService);
    private readonly router = inject(Router);
    private readonly languageVersion = signal(0);
    private navigationRevision = 0;

    protected readonly notifications = this.notificationService.notifications;
    protected readonly isLoading = computed(
        () => this.notificationService.notificationsLoading() && !this.notificationService.notificationsLoaded(),
    );
    protected readonly isMarkingAllRead = signal(false);
    protected readonly markAllReadError = signal(false);
    protected readonly hasUnreadNotifications = computed(() => this.notifications().some(item => !item.isRead));
    protected readonly notificationItems = computed<NotificationViewModel[]>(() =>
        this.notifications().map(notification => this.buildNotificationViewModel(notification)),
    );

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
        this.router.events
            .pipe(
                filter(event => event instanceof NavigationStart),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                // Let an in-flight read finish without reopening an older destination.
                this.navigationRevision++;
            });
        this.notificationService.ensureNotificationsLoaded();
    }

    protected openNotification(notification: NotificationItem): void {
        const targetUrl = notification.targetUrl;
        const destination = targetUrl === null || targetUrl.trim().length === 0 ? null : { targetUrl, revision: ++this.navigationRevision };
        const navigate = (): void => {
            if (destination?.revision !== this.navigationRevision) {
                return;
            }

            void this.router.navigateByUrl(destination.targetUrl);
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
        this.markAllReadError.set(false);
        this.notificationService
            .markAllRead()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isMarkingAllRead.set(false);
                },
                error: () => {
                    this.isMarkingAllRead.set(false);
                    this.markAllReadError.set(true);
                },
            });
    }

    private buildNotificationViewModel(notification: NotificationItem): NotificationViewModel {
        this.languageVersion();
        return {
            notification,
            ...notificationPresentation(notification.type),
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
}

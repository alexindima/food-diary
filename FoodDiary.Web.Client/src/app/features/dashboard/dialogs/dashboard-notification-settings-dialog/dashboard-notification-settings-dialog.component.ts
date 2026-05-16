import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiHintDirective } from 'fd-ui-kit/hint/fd-ui-hint.directive';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize } from 'rxjs';

import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { NavigationService } from '../../../../services/navigation.service';
import { NotificationService } from '../../../../services/notification.service';
import { PushNotificationService } from '../../../../services/push-notification.service';

@Component({
    selector: 'fd-dashboard-notification-settings-dialog',
    templateUrl: './dashboard-notification-settings-dialog.component.html',
    styleUrl: './dashboard-notification-settings-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiDialogComponent, FdUiSwitchComponent, FdUiButtonComponent, FdUiDialogFooterDirective, FdUiHintDirective],
})
export class DashboardNotificationSettingsDialogComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject(FdUiDialogRef<DashboardNotificationSettingsDialogComponent>, { optional: true });
    private readonly notificationService = inject(NotificationService);
    private readonly pushNotifications = inject(PushNotificationService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly notificationPermission = signal<NotificationPermission>(this.readNotificationPermission());

    public readonly isLoading = signal(true);
    public readonly isUpdating = signal(false);
    public readonly isOpeningProfile = signal(false);
    public readonly submitError = signal<string | null>(null);
    public readonly pushNotificationsEnabled = signal(false);
    public readonly fastingPushNotificationsEnabled = signal(true);
    public readonly socialPushNotificationsEnabled = signal(true);
    public readonly pushNotificationsSupported = this.pushNotifications.isSupported;
    public readonly pushNotificationsSubscribed = this.pushNotifications.isSubscribed;
    public readonly pushNotificationsBusy = this.pushNotifications.isBusy;

    public readonly pushNotificationsAccountStatusKey = computed(() =>
        this.pushNotificationsEnabled()
            ? 'USER_MANAGE.NOTIFICATIONS_ACCOUNT_STATUS_ENABLED'
            : 'USER_MANAGE.NOTIFICATIONS_ACCOUNT_STATUS_DISABLED',
    );

    public readonly pushNotificationsDeviceStatusKey = computed(() => {
        if (!this.pushNotificationsSupported()) {
            return 'USER_MANAGE.NOTIFICATIONS_STATUS_UNSUPPORTED';
        }

        if (this.notificationPermission() === 'denied') {
            return 'USER_MANAGE.NOTIFICATIONS_STATUS_BLOCKED';
        }

        if (this.pushNotificationsSubscribed()) {
            return 'USER_MANAGE.NOTIFICATIONS_STATUS_ENABLED';
        }

        if (!this.pushNotificationsEnabled()) {
            return 'USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_IDLE';
        }

        return 'USER_MANAGE.NOTIFICATIONS_STATUS_SETUP_REQUIRED';
    });

    public readonly pushNotificationsHintKey = computed(() => {
        if (!this.pushNotificationsEnabled()) {
            return 'USER_MANAGE.NOTIFICATIONS_DISABLED_HINT';
        }

        if (this.notificationPermission() === 'denied') {
            return 'USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT';
        }

        if (!this.pushNotificationsSupported()) {
            return 'USER_MANAGE.NOTIFICATIONS_UNSUPPORTED_HINT';
        }

        if (this.pushNotificationsSubscribed()) {
            return 'USER_MANAGE.NOTIFICATIONS_ENABLED_HINT';
        }

        return 'USER_MANAGE.NOTIFICATIONS_SETUP_REQUIRED_HINT';
    });

    public constructor() {
        this.loadPreferences();
    }

    public togglePushNotifications(): void {
        if (this.isUpdating() || this.pushNotificationsBusy()) {
            return;
        }

        const nextEnabled = !this.pushNotificationsEnabled();
        this.isUpdating.set(true);
        this.submitError.set(null);

        this.notificationService
            .updateNotificationPreferences({ pushNotificationsEnabled: nextEnabled })
            .pipe(
                finalize(() => {
                    this.isUpdating.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: preferences => {
                    this.pushNotificationsEnabled.set(preferences.pushNotificationsEnabled);
                    this.fastingPushNotificationsEnabled.set(preferences.fastingPushNotificationsEnabled);
                    this.socialPushNotificationsEnabled.set(preferences.socialPushNotificationsEnabled);
                    this.notificationPermission.set(this.readNotificationPermission());

                    if (!nextEnabled) {
                        this.frontendObservability.recordNotificationPreferenceChanged('push', false, {
                            permission: this.notificationPermission(),
                            source: 'dashboard-dialog',
                        });
                        this.toastService.info(this.translateService.instant('DASHBOARD.ACTIONS.PUSH_DISABLED'));
                        return;
                    }

                    void this.finishEnablingPushAsync();
                },
                error: () => {
                    this.submitError.set(this.translateService.instant('DASHBOARD.NOTIFICATIONS.ERROR'));
                },
            });
    }

    public toggleFastingPushNotifications(): void {
        if (this.isUpdating()) {
            return;
        }

        const nextEnabled = !this.fastingPushNotificationsEnabled();
        this.isUpdating.set(true);
        this.submitError.set(null);

        this.notificationService
            .updateNotificationPreferences({ fastingPushNotificationsEnabled: nextEnabled })
            .pipe(
                finalize(() => {
                    this.isUpdating.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: preferences => {
                    this.pushNotificationsEnabled.set(preferences.pushNotificationsEnabled);
                    this.fastingPushNotificationsEnabled.set(preferences.fastingPushNotificationsEnabled);
                    this.socialPushNotificationsEnabled.set(preferences.socialPushNotificationsEnabled);
                    this.frontendObservability.recordNotificationPreferenceChanged('fasting', nextEnabled, {
                        source: 'dashboard-dialog',
                    });
                    this.toastService.info(
                        this.translateService.instant(
                            nextEnabled
                                ? 'USER_MANAGE.NOTIFICATIONS_FASTING_ENABLED_TOAST'
                                : 'USER_MANAGE.NOTIFICATIONS_FASTING_DISABLED_TOAST',
                        ),
                    );
                },
                error: () => {
                    this.submitError.set(this.translateService.instant('DASHBOARD.NOTIFICATIONS.ERROR'));
                },
            });
    }

    public toggleSocialPushNotifications(): void {
        if (this.isUpdating()) {
            return;
        }

        const nextEnabled = !this.socialPushNotificationsEnabled();
        this.isUpdating.set(true);
        this.submitError.set(null);

        this.notificationService
            .updateNotificationPreferences({ socialPushNotificationsEnabled: nextEnabled })
            .pipe(
                finalize(() => {
                    this.isUpdating.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: preferences => {
                    this.pushNotificationsEnabled.set(preferences.pushNotificationsEnabled);
                    this.fastingPushNotificationsEnabled.set(preferences.fastingPushNotificationsEnabled);
                    this.socialPushNotificationsEnabled.set(preferences.socialPushNotificationsEnabled);
                    this.frontendObservability.recordNotificationPreferenceChanged('social', nextEnabled, {
                        source: 'dashboard-dialog',
                    });
                    this.toastService.info(
                        this.translateService.instant(
                            nextEnabled
                                ? 'USER_MANAGE.NOTIFICATIONS_SOCIAL_ENABLED_TOAST'
                                : 'USER_MANAGE.NOTIFICATIONS_SOCIAL_DISABLED_TOAST',
                        ),
                    );
                },
                error: () => {
                    this.submitError.set(this.translateService.instant('DASHBOARD.NOTIFICATIONS.ERROR'));
                },
            });
    }

    public async openAdvancedSettingsAsync(): Promise<void> {
        if (this.isOpeningProfile()) {
            return;
        }

        this.isOpeningProfile.set(true);

        try {
            this.dialogRef?.close();
            await this.navigationService.navigateToProfileAsync();
        } finally {
            this.isOpeningProfile.set(false);
        }
    }

    private loadPreferences(): void {
        this.notificationService
            .getNotificationPreferences()
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: preferences => {
                    this.pushNotificationsEnabled.set(preferences.pushNotificationsEnabled);
                    this.fastingPushNotificationsEnabled.set(preferences.fastingPushNotificationsEnabled);
                    this.socialPushNotificationsEnabled.set(preferences.socialPushNotificationsEnabled);
                    this.notificationPermission.set(this.readNotificationPermission());
                    this.frontendObservability.recordNotificationSettingsViewed({
                        pushEnabled: preferences.pushNotificationsEnabled,
                        fastingEnabled: preferences.fastingPushNotificationsEnabled,
                        socialEnabled: preferences.socialPushNotificationsEnabled,
                        source: 'dashboard-dialog',
                    });
                },
                error: () => {
                    this.submitError.set(this.translateService.instant('DASHBOARD.NOTIFICATIONS.LOAD_ERROR'));
                },
            });
    }

    private async finishEnablingPushAsync(): Promise<void> {
        const result = await this.pushNotifications.ensureSubscriptionAsync();

        switch (result) {
            case 'subscribed':
            case 'already-subscribed':
                this.frontendObservability.recordNotificationPreferenceChanged('push', true, {
                    permission: this.notificationPermission(),
                    source: 'dashboard-dialog',
                });
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'success', {
                    result,
                    source: 'dashboard-dialog',
                });
                this.toastService.success(this.translateService.instant('DASHBOARD.ACTIONS.PUSH_ENABLED'));
                break;
            case 'unsupported':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'unsupported', {
                    result,
                    source: 'dashboard-dialog',
                });
                this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_UNSUPPORTED_HINT'));
                break;
            case 'blocked':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'blocked', {
                    result,
                    source: 'dashboard-dialog',
                });
                this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT'));
                break;
            case 'unavailable':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'unavailable', {
                    result,
                    source: 'dashboard-dialog',
                });
                this.toastService.info(
                    this.translateService.instant(
                        this.notificationPermission() === 'denied'
                            ? 'USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT'
                            : 'USER_MANAGE.NOTIFICATIONS_UNAVAILABLE_HINT',
                    ),
                );
                break;
        }
    }

    private readNotificationPermission(): NotificationPermission {
        if (typeof Notification === 'undefined') {
            return 'default';
        }

        return Notification.permission;
    }
}

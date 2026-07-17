import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize } from 'rxjs';

import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { NavigationService } from '../../../../services/navigation.service';
import { BrowserNotificationCapabilityService } from '../../../../shared/notifications/browser-notification-capability.service';
import {
    type NotificationPreferences,
    NotificationService,
    type UpdateNotificationPreferencesRequest,
} from '../../../../shared/notifications/notification.service';
import { type PushNotificationEnableResult, PushNotificationService } from '../../../../shared/notifications/push-notification.service';

@Injectable()
export class DashboardNotificationSettingsFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogRef = inject(FdUiDialogRef, { optional: true });
    private readonly notificationService = inject(NotificationService);
    private readonly pushNotifications = inject(PushNotificationService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly browserNotifications = inject(BrowserNotificationCapabilityService);
    private readonly notificationPermission = signal<NotificationPermission | 'unsupported'>(this.browserNotifications.getPermission());

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
        return this.pushNotificationsSubscribed()
            ? 'USER_MANAGE.NOTIFICATIONS_ENABLED_HINT'
            : 'USER_MANAGE.NOTIFICATIONS_SETUP_REQUIRED_HINT';
    });

    public load(): void {
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
                    this.applyPreferences(preferences);
                    this.frontendObservability.recordNotificationSettingsViewed({
                        pushEnabled: preferences.pushNotificationsEnabled,
                        fastingEnabled: preferences.fastingPushNotificationsEnabled,
                        socialEnabled: preferences.socialPushNotificationsEnabled,
                        source: 'dashboard-dialog',
                    });
                },
                error: () => {
                    this.setError('DASHBOARD.NOTIFICATIONS.LOAD_ERROR');
                },
            });
    }

    public togglePushNotifications(): void {
        if (this.isUpdating() || this.pushNotificationsBusy()) {
            return;
        }

        const nextEnabled = !this.pushNotificationsEnabled();
        this.updatePreferences({ pushNotificationsEnabled: nextEnabled }, preferences => {
            this.applyPreferences(preferences);
            if (nextEnabled) {
                void this.finishEnablingPushAsync();
                return;
            }
            this.frontendObservability.recordNotificationPreferenceChanged('push', false, {
                permission: this.notificationPermission(),
                source: 'dashboard-dialog',
            });
            this.toastService.info(this.translateService.instant('DASHBOARD.ACTIONS.PUSH_DISABLED'));
        });
    }

    public toggleFastingPushNotifications(): void {
        this.toggleCategory({
            category: 'fasting',
            nextEnabled: !this.fastingPushNotificationsEnabled(),
            property: 'fastingPushNotificationsEnabled',
            enabledToastKey: 'USER_MANAGE.NOTIFICATIONS_FASTING_ENABLED_TOAST',
            disabledToastKey: 'USER_MANAGE.NOTIFICATIONS_FASTING_DISABLED_TOAST',
        });
    }

    public toggleSocialPushNotifications(): void {
        this.toggleCategory({
            category: 'social',
            nextEnabled: !this.socialPushNotificationsEnabled(),
            property: 'socialPushNotificationsEnabled',
            enabledToastKey: 'USER_MANAGE.NOTIFICATIONS_SOCIAL_ENABLED_TOAST',
            disabledToastKey: 'USER_MANAGE.NOTIFICATIONS_SOCIAL_DISABLED_TOAST',
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

    private toggleCategory(options: NotificationCategoryToggle): void {
        if (this.isUpdating()) {
            return;
        }

        this.updatePreferences({ [options.property]: options.nextEnabled }, preferences => {
            this.applyPreferences(preferences);
            this.frontendObservability.recordNotificationPreferenceChanged(options.category, options.nextEnabled, {
                source: 'dashboard-dialog',
            });
            this.toastService.info(this.translateService.instant(options.nextEnabled ? options.enabledToastKey : options.disabledToastKey));
        });
    }

    private updatePreferences(
        update: UpdateNotificationPreferencesRequest,
        onSuccess: (preferences: NotificationPreferences) => void,
    ): void {
        this.isUpdating.set(true);
        this.submitError.set(null);
        this.notificationService
            .updateNotificationPreferences(update)
            .pipe(
                finalize(() => {
                    this.isUpdating.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: onSuccess,
                error: () => {
                    this.setError('DASHBOARD.NOTIFICATIONS.ERROR');
                },
            });
    }

    private applyPreferences(preferences: NotificationPreferences): void {
        this.pushNotificationsEnabled.set(preferences.pushNotificationsEnabled);
        this.fastingPushNotificationsEnabled.set(preferences.fastingPushNotificationsEnabled);
        this.socialPushNotificationsEnabled.set(preferences.socialPushNotificationsEnabled);
        this.notificationPermission.set(this.browserNotifications.getPermission());
    }

    private async finishEnablingPushAsync(): Promise<void> {
        const result = await this.pushNotifications.ensureSubscriptionAsync();
        if (result === 'subscribed' || result === 'already-subscribed') {
            this.frontendObservability.recordNotificationPreferenceChanged('push', true, {
                permission: this.notificationPermission(),
                source: 'dashboard-dialog',
            });
            this.recordSubscriptionResult(result, 'success', 'DASHBOARD.ACTIONS.PUSH_ENABLED', 'success');
            return;
        }

        const toastKey =
            result === 'unsupported'
                ? 'USER_MANAGE.NOTIFICATIONS_UNSUPPORTED_HINT'
                : result === 'blocked' || this.notificationPermission() === 'denied'
                  ? 'USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT'
                  : 'USER_MANAGE.NOTIFICATIONS_UNAVAILABLE_HINT';
        this.recordSubscriptionResult(result, result, toastKey, 'info');
    }

    private recordSubscriptionResult(
        result: PushNotificationEnableResult,
        outcome: 'success' | 'blocked' | 'unsupported' | 'unavailable',
        toastKey: string,
        toastType: 'success' | 'info',
    ): void {
        this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', outcome, {
            result,
            source: 'dashboard-dialog',
        });
        this.toastService[toastType](this.translateService.instant(toastKey));
    }

    private setError(key: string): void {
        this.submitError.set(this.translateService.instant(key));
    }
}

type NotificationCategoryToggle = {
    category: 'fasting' | 'social';
    nextEnabled: boolean;
    property: 'fastingPushNotificationsEnabled' | 'socialPushNotificationsEnabled';
    enabledToastKey: string;
    disabledToastKey: string;
};

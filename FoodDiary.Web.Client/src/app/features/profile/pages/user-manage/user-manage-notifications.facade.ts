import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { LocalizationService } from '../../../../services/localization.service';
import { NotificationService, type WebPushSubscriptionItem } from '../../../../services/notification.service';
import { PushNotificationService } from '../../../../services/push-notification.service';
import {
    FASTING_REMINDER_PRESETS,
    type FastingReminderPreset,
    resolveFastingReminderPresetId,
} from '../../../../shared/lib/fasting-reminder-presets';
import { parseIntegerInput } from '../../../../shared/lib/number.utils';
import type { User } from '../../../../shared/models/user.data';
import { ProfileManageFacade } from '../../lib/profile-manage.facade';
import {
    DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS,
    DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS,
    MAX_FASTING_REMINDER_HOURS,
    TEST_NOTIFICATION_DELAY_SECONDS,
} from './user-manage.config';
import type { ConnectedDeviceViewModel } from './user-manage.types';
import { formatUserManageDateTime } from './user-manage-date.mapper';
import { buildConnectedDeviceItems, isCurrentConnectedDevice } from './user-manage-notifications.mapper';

@Injectable({ providedIn: 'root' })
export class UserManageNotificationsFacade {
    private readonly facade = inject(ProfileManageFacade);
    private readonly notificationService = inject(NotificationService);
    private readonly pushNotifications = inject(PushNotificationService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly languageVersion = signal(0);
    private readonly hasTrackedNotificationsView = signal(false);

    public readonly notificationPermission = signal<NotificationPermission | 'unsupported'>(this.readNotificationPermission());
    public readonly notificationsChangedVersion = this.notificationService.notificationsChangedVersion;
    public readonly isUpdatingNotifications = this.facade.isUpdatingNotifications;
    public readonly isSchedulingTestNotification = signal(false);
    public readonly pushNotificationsEnabled = computed(() => this.facade.user()?.pushNotificationsEnabled ?? false);
    public readonly fastingPushNotificationsEnabled = computed(() => this.facade.user()?.fastingPushNotificationsEnabled ?? true);
    public readonly socialPushNotificationsEnabled = computed(() => this.facade.user()?.socialPushNotificationsEnabled ?? true);
    public readonly fastingCheckInReminderHours = signal(DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS);
    public readonly fastingCheckInFollowUpReminderHours = signal(DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS);
    public readonly fastingReminderPresets = FASTING_REMINDER_PRESETS;
    public readonly pushNotificationsSupported = this.pushNotifications.isSupported;
    public readonly pushNotificationsSubscribed = this.pushNotifications.isSubscribed;
    public readonly pushNotificationsBusy = this.pushNotifications.isBusy;
    public readonly currentSubscriptionEndpoint = this.pushNotifications.currentSubscriptionEndpoint;
    public readonly connectedDevices = this.facade.webPushSubscriptions;
    public readonly isLoadingConnectedDevices = this.facade.isLoadingWebPushSubscriptions;
    public readonly removingConnectedDeviceEndpoint = this.facade.removingWebPushSubscriptionEndpoint;
    public readonly connectedDeviceItems = computed<ConnectedDeviceViewModel[]>(() => {
        this.languageVersion();
        return buildConnectedDeviceItems(
            this.connectedDevices(),
            this.currentSubscriptionEndpoint(),
            value => this.formatDateTime(value),
            key => this.translateService.instant(key),
        );
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    public syncFromUser(user: User): void {
        this.fastingCheckInReminderHours.set(user.fastingCheckInReminderHours);
        this.fastingCheckInFollowUpReminderHours.set(user.fastingCheckInFollowUpReminderHours);

        if (this.hasTrackedNotificationsView()) {
            return;
        }

        this.frontendObservability.recordNotificationSettingsViewed({
            pushEnabled: user.pushNotificationsEnabled,
            fastingEnabled: user.fastingPushNotificationsEnabled,
            socialEnabled: user.socialPushNotificationsEnabled,
        });
        this.hasTrackedNotificationsView.set(true);
    }

    public async togglePushNotificationsAsync(): Promise<void> {
        if (this.isUpdatingNotifications() || this.pushNotificationsBusy()) {
            return;
        }

        const nextEnabled = !this.pushNotificationsEnabled();
        const user = await this.facade.updateNotificationPreferencesAsync({ pushNotificationsEnabled: nextEnabled });
        if (user === null) {
            return;
        }

        this.notificationPermission.set(this.readNotificationPermission());
        if (!nextEnabled) {
            this.handlePushNotificationsDisabled();
            return;
        }

        const result = await this.pushNotifications.ensureSubscriptionAsync();
        this.handlePushSubscriptionResult(result);
    }

    public async toggleFastingPushNotificationsAsync(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const user = await this.facade.updateNotificationPreferencesAsync({
            fastingPushNotificationsEnabled: !this.fastingPushNotificationsEnabled(),
        });
        if (user === null) {
            return;
        }

        this.toastService.info(
            this.translateService.instant(
                user.fastingPushNotificationsEnabled
                    ? 'USER_MANAGE.NOTIFICATIONS_FASTING_ENABLED_TOAST'
                    : 'USER_MANAGE.NOTIFICATIONS_FASTING_DISABLED_TOAST',
            ),
        );
        this.frontendObservability.recordNotificationPreferenceChanged('fasting', user.fastingPushNotificationsEnabled);
    }

    public onFastingReminderHoursChange(value: string | number, field: 'first' | 'followUp'): void {
        const parsed = parseIntegerInput(value);
        if (parsed === null) {
            return;
        }

        const normalized = Math.max(1, Math.min(MAX_FASTING_REMINDER_HOURS, parsed));
        if (field === 'first') {
            this.fastingCheckInReminderHours.set(normalized);
            return;
        }

        this.fastingCheckInFollowUpReminderHours.set(normalized);
    }

    public applyFastingReminderPreset(preset: FastingReminderPreset): void {
        this.fastingCheckInReminderHours.set(preset.firstReminderHours);
        this.fastingCheckInFollowUpReminderHours.set(preset.followUpReminderHours);
        this.frontendObservability.recordFastingReminderPresetSelected({
            presetId: preset.id,
            firstReminderHours: preset.firstReminderHours,
            followUpReminderHours: preset.followUpReminderHours,
        });
    }

    public async saveFastingReminderHoursAsync(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const firstReminder = this.fastingCheckInReminderHours();
        const followUpReminder = this.fastingCheckInFollowUpReminderHours();
        if (followUpReminder <= firstReminder) {
            this.toastService.error(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_ERROR'));
            return;
        }

        const user = await this.facade.updateNotificationPreferencesAsync({
            fastingCheckInReminderHours: firstReminder,
            fastingCheckInFollowUpReminderHours: followUpReminder,
        });
        if (user === null) {
            return;
        }

        const activePresetId = this.getActiveFastingReminderPresetId();
        this.frontendObservability.recordFastingReminderTimingSaved({
            firstReminderHours: firstReminder,
            followUpReminderHours: followUpReminder,
            source: activePresetId !== null ? 'preset' : 'manual',
            presetId: activePresetId ?? undefined,
        });
        this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_SAVED'));
    }

    public async removeConnectedDeviceAsync(subscription: WebPushSubscriptionItem): Promise<void> {
        const endpoint = subscription.endpoint;
        if (endpoint.length === 0 || this.removingConnectedDeviceEndpoint() !== null || this.pushNotificationsBusy()) {
            return;
        }

        const removed =
            this.currentSubscriptionEndpoint() === endpoint
                ? await this.pushNotifications.removeSubscriptionAsync(endpoint)
                : await this.facade.removeWebPushSubscriptionAsync(endpoint);

        if (!removed) {
            this.frontendObservability.recordNotificationSubscriptionEvent('subscription.remove', 'failed', {
                currentDevice: this.isCurrentDevice(subscription),
            });
            this.toastService.error(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVE_ERROR'));
            return;
        }

        this.facade.refreshWebPushSubscriptions();
        this.frontendObservability.recordNotificationSubscriptionEvent('subscription.remove', 'success', {
            currentDevice: this.isCurrentDevice(subscription),
        });
        this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVED_TOAST'));
    }

    public async toggleSocialPushNotificationsAsync(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const user = await this.facade.updateNotificationPreferencesAsync({
            socialPushNotificationsEnabled: !this.socialPushNotificationsEnabled(),
        });
        if (user === null) {
            return;
        }

        this.toastService.info(
            this.translateService.instant(
                user.socialPushNotificationsEnabled
                    ? 'USER_MANAGE.NOTIFICATIONS_SOCIAL_ENABLED_TOAST'
                    : 'USER_MANAGE.NOTIFICATIONS_SOCIAL_DISABLED_TOAST',
            ),
        );
        this.frontendObservability.recordNotificationPreferenceChanged('social', user.socialPushNotificationsEnabled);
    }

    public scheduleTestNotification(): void {
        if (this.isSchedulingTestNotification()) {
            return;
        }

        this.isSchedulingTestNotification.set(true);
        this.notificationService
            .scheduleTestNotification({
                delaySeconds: TEST_NOTIFICATION_DELAY_SECONDS,
                type: 'FastingCompleted',
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSchedulingTestNotification.set(false);
                    this.frontendObservability.recordNotificationSubscriptionEvent('test-push.schedule', 'success', {
                        type: 'FastingCompleted',
                        delaySeconds: TEST_NOTIFICATION_DELAY_SECONDS,
                    });
                    this.toastService.info(this.translateService.instant('DASHBOARD.ACTIONS.TEST_PUSH_SCHEDULED'));
                },
                error: () => {
                    this.isSchedulingTestNotification.set(false);
                    this.frontendObservability.recordNotificationSubscriptionEvent('test-push.schedule', 'failed', {
                        type: 'FastingCompleted',
                        delaySeconds: TEST_NOTIFICATION_DELAY_SECONDS,
                    });
                    this.toastService.error(this.translateService.instant('DASHBOARD.ACTIONS.TEST_PUSH_ERROR'));
                },
            });
    }

    public isCurrentDevice(subscription: WebPushSubscriptionItem): boolean {
        return isCurrentConnectedDevice(subscription, this.currentSubscriptionEndpoint());
    }

    private handlePushNotificationsDisabled(): void {
        this.frontendObservability.recordNotificationPreferenceChanged('push', false, {
            permission: this.notificationPermission(),
        });
        this.toastService.info(this.translateService.instant('DASHBOARD.ACTIONS.PUSH_DISABLED'));
    }

    private handlePushSubscriptionResult(result: Awaited<ReturnType<PushNotificationService['ensureSubscriptionAsync']>>): void {
        switch (result) {
            case 'subscribed':
            case 'already-subscribed':
                this.frontendObservability.recordNotificationPreferenceChanged('push', true, {
                    permission: this.notificationPermission(),
                });
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'success', { result });
                this.toastService.success(this.translateService.instant('DASHBOARD.ACTIONS.PUSH_ENABLED'));
                this.facade.refreshWebPushSubscriptions();
                break;
            case 'unsupported':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'unsupported', { result });
                this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_UNSUPPORTED_HINT'));
                break;
            case 'blocked':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'blocked', { result });
                this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT'));
                break;
            case 'unavailable':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'unavailable', { result });
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

    private readNotificationPermission(): NotificationPermission | 'unsupported' {
        if (typeof Notification === 'undefined') {
            return 'unsupported';
        }

        return Notification.permission;
    }

    private formatDateTime(value: string | null): string | null {
        return formatUserManageDateTime(value, this.localizationService.getCurrentLanguage());
    }

    private getActiveFastingReminderPresetId(): string | null {
        const presetId = resolveFastingReminderPresetId(this.fastingCheckInReminderHours(), this.fastingCheckInFollowUpReminderHours());
        return presetId === 'custom' ? null : presetId;
    }
}

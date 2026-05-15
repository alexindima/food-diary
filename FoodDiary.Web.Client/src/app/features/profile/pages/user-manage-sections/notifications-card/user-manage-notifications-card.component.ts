import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch.component';

import type { WebPushSubscriptionItem } from '../../../../../services/notification.service';
import { type FastingReminderPreset, resolveFastingReminderPresetId } from '../../../../../shared/lib/fasting-reminder-presets';
import type { ConnectedDeviceViewModel } from '../../user-manage/user-manage.types';
import { buildNotificationsStatusKey } from '../../user-manage/user-manage-notifications.mapper';
import { UserManageConnectedDevicesComponent } from '../connected-devices/user-manage-connected-devices.component';

export type FastingReminderHoursChange = {
    value: string | number;
    field: 'first' | 'followUp';
};

@Component({
    selector: 'fd-user-manage-notifications-card',
    imports: [
        FormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiSwitchComponent,
        UserManageConnectedDevicesComponent,
    ],
    templateUrl: './user-manage-notifications-card.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageNotificationsCardComponent {
    public readonly pushNotificationsEnabled = input.required<boolean>();
    public readonly isUpdatingNotifications = input.required<boolean>();
    public readonly pushNotificationsBusy = input.required<boolean>();
    public readonly pushNotificationsSubscribed = input.required<boolean>();
    public readonly pushNotificationsSupported = input.required<boolean>();
    public readonly notificationPermission = input.required<NotificationPermission | 'unsupported'>();
    public readonly fastingPushNotificationsEnabled = input.required<boolean>();
    public readonly socialPushNotificationsEnabled = input.required<boolean>();
    public readonly fastingReminderPresets = input.required<readonly FastingReminderPreset[]>();
    public readonly fastingCheckInReminderHours = input.required<number>();
    public readonly fastingCheckInFollowUpReminderHours = input.required<number>();
    public readonly isSchedulingTestNotification = input.required<boolean>();
    public readonly isLoadingConnectedDevices = input.required<boolean>();
    public readonly connectedDeviceItems = input.required<ConnectedDeviceViewModel[]>();
    public readonly removingConnectedDeviceEndpoint = input.required<string | null>();

    public readonly notificationsStatusKey = computed(() =>
        buildNotificationsStatusKey({
            isSchedulingTestNotification: this.isSchedulingTestNotification(),
            isRemovingConnectedDevice: this.removingConnectedDeviceEndpoint() !== null,
            isPushNotificationsBusy: this.pushNotificationsBusy(),
            isUpdatingNotifications: this.isUpdatingNotifications(),
        }),
    );
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
    public readonly activeFastingReminderPresetId = computed(() => {
        const presetId = resolveFastingReminderPresetId(this.fastingCheckInReminderHours(), this.fastingCheckInFollowUpReminderHours());
        return presetId === 'custom' ? null : presetId;
    });

    public readonly pushNotificationsToggle = output();
    public readonly fastingPushNotificationsToggle = output();
    public readonly socialPushNotificationsToggle = output();
    public readonly fastingReminderPresetApply = output<FastingReminderPreset>();
    public readonly fastingReminderHoursChange = output<FastingReminderHoursChange>();
    public readonly fastingReminderHoursSave = output();
    public readonly testNotificationSchedule = output();
    public readonly connectedDeviceRemove = output<WebPushSubscriptionItem>();
}

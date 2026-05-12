import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch.component';

import type { WebPushSubscriptionItem } from '../../../services/notification.service';
import type { FastingReminderPreset } from '../../../shared/lib/fasting-reminder-presets';
import type { ConnectedDeviceViewModel } from './user-manage.types';
import { UserManageConnectedDevicesComponent } from './user-manage-connected-devices.component';

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
    styleUrl: './user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageNotificationsCardComponent {
    public readonly notificationsStatusKey = input.required<string | null>();
    public readonly pushNotificationsAccountStatusKey = input.required<string>();
    public readonly pushNotificationsDeviceStatusKey = input.required<string>();
    public readonly pushNotificationsHintKey = input.required<string>();
    public readonly pushNotificationsEnabled = input.required<boolean>();
    public readonly isUpdatingNotifications = input.required<boolean>();
    public readonly pushNotificationsBusy = input.required<boolean>();
    public readonly fastingPushNotificationsEnabled = input.required<boolean>();
    public readonly socialPushNotificationsEnabled = input.required<boolean>();
    public readonly fastingReminderPresets = input.required<readonly FastingReminderPreset[]>();
    public readonly activeFastingReminderPresetId = input.required<string | null>();
    public readonly fastingCheckInReminderHours = input.required<number>();
    public readonly fastingCheckInFollowUpReminderHours = input.required<number>();
    public readonly isSchedulingTestNotification = input.required<boolean>();
    public readonly pushNotificationsSupported = input.required<boolean>();
    public readonly connectedDevicesSectionState = input.required<'loading' | 'content' | 'empty'>();
    public readonly connectedDeviceItems = input.required<ConnectedDeviceViewModel[]>();
    public readonly removingConnectedDeviceEndpoint = input.required<string | null>();

    public readonly pushNotificationsToggle = output();
    public readonly fastingPushNotificationsToggle = output();
    public readonly socialPushNotificationsToggle = output();
    public readonly fastingReminderPresetApply = output<FastingReminderPreset>();
    public readonly fastingReminderHoursChange = output<FastingReminderHoursChange>();
    public readonly fastingReminderHoursSave = output();
    public readonly testNotificationSchedule = output();
    public readonly connectedDeviceRemove = output<WebPushSubscriptionItem>();
}

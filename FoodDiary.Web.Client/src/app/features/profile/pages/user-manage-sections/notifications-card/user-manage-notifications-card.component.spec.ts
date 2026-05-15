import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { FASTING_REMINDER_PRESETS } from '../../../../../shared/lib/fasting-reminder-presets';
import { UserManageNotificationsCardComponent } from './user-manage-notifications-card.component';

const CUSTOM_FIRST_REMINDER_HOURS = 3;
const CUSTOM_FOLLOW_UP_REMINDER_HOURS = 11;

let fixture: ComponentFixture<UserManageNotificationsCardComponent>;
let component: UserManageNotificationsCardComponent;

describe('UserManageNotificationsCardComponent status state', () => {
    it('derives background action status from busy inputs', async () => {
        await createComponentAsync();

        expect(component.notificationsStatusKey()).toBeNull();

        fixture.componentRef.setInput('isUpdatingNotifications', true);
        fixture.detectChanges();
        expect(component.notificationsStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_SAVING');

        fixture.componentRef.setInput('isUpdatingNotifications', false);
        fixture.componentRef.setInput('isSchedulingTestNotification', true);
        fixture.detectChanges();
        expect(component.notificationsStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_TEST_SENDING');

        fixture.componentRef.setInput('isSchedulingTestNotification', false);
        fixture.componentRef.setInput('removingConnectedDeviceEndpoint', 'https://push.example/subscription');
        fixture.detectChanges();
        expect(component.notificationsStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_REMOVING');
    });

    it('derives push notification account, device, and hint states', async () => {
        await createComponentAsync({
            pushNotificationsEnabled: false,
            pushNotificationsSupported: true,
            pushNotificationsSubscribed: false,
            notificationPermission: 'default',
        });

        expect(component.pushNotificationsAccountStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_ACCOUNT_STATUS_DISABLED');
        expect(component.pushNotificationsDeviceStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_IDLE');
        expect(component.pushNotificationsHintKey()).toBe('USER_MANAGE.NOTIFICATIONS_DISABLED_HINT');

        fixture.componentRef.setInput('pushNotificationsEnabled', true);
        fixture.detectChanges();
        expect(component.pushNotificationsAccountStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_ACCOUNT_STATUS_ENABLED');
        expect(component.pushNotificationsDeviceStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_SETUP_REQUIRED');
        expect(component.pushNotificationsHintKey()).toBe('USER_MANAGE.NOTIFICATIONS_SETUP_REQUIRED_HINT');

        fixture.componentRef.setInput('pushNotificationsSubscribed', true);
        fixture.detectChanges();
        expect(component.pushNotificationsDeviceStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_ENABLED');
        expect(component.pushNotificationsHintKey()).toBe('USER_MANAGE.NOTIFICATIONS_ENABLED_HINT');
    });

    it('prioritizes blocked and unsupported device states', async () => {
        await createComponentAsync({
            pushNotificationsEnabled: true,
            pushNotificationsSupported: true,
            notificationPermission: 'denied',
        });

        expect(component.pushNotificationsDeviceStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_BLOCKED');
        expect(component.pushNotificationsHintKey()).toBe('USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT');

        fixture.componentRef.setInput('pushNotificationsSupported', false);
        fixture.componentRef.setInput('notificationPermission', 'unsupported');
        fixture.detectChanges();
        expect(component.pushNotificationsDeviceStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_UNSUPPORTED');
        expect(component.pushNotificationsHintKey()).toBe('USER_MANAGE.NOTIFICATIONS_UNSUPPORTED_HINT');
    });

    it('derives active fasting preset from reminder hours', async () => {
        const preset = FASTING_REMINDER_PRESETS[0];
        await createComponentAsync({
            fastingCheckInReminderHours: preset.firstReminderHours,
            fastingCheckInFollowUpReminderHours: preset.followUpReminderHours,
        });

        expect(component.activeFastingReminderPresetId()).toBe(preset.id);

        fixture.componentRef.setInput('fastingCheckInReminderHours', CUSTOM_FIRST_REMINDER_HOURS);
        fixture.componentRef.setInput('fastingCheckInFollowUpReminderHours', CUSTOM_FOLLOW_UP_REMINDER_HOURS);
        fixture.detectChanges();
        expect(component.activeFastingReminderPresetId()).toBeNull();
    });
});

type NotificationsCardInputs = {
    pushNotificationsEnabled: boolean;
    isUpdatingNotifications: boolean;
    pushNotificationsBusy: boolean;
    pushNotificationsSubscribed: boolean;
    pushNotificationsSupported: boolean;
    notificationPermission: NotificationPermission | 'unsupported';
    fastingPushNotificationsEnabled: boolean;
    socialPushNotificationsEnabled: boolean;
    fastingCheckInReminderHours: number;
    fastingCheckInFollowUpReminderHours: number;
    isSchedulingTestNotification: boolean;
    isLoadingConnectedDevices: boolean;
    removingConnectedDeviceEndpoint: string | null;
};

async function createComponentAsync(overrides: Partial<NotificationsCardInputs> = {}): Promise<void> {
    await TestBed.configureTestingModule({
        imports: [UserManageNotificationsCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    fixture = TestBed.createComponent(UserManageNotificationsCardComponent);
    component = fixture.componentInstance;

    const inputs: NotificationsCardInputs = {
        pushNotificationsEnabled: true,
        isUpdatingNotifications: false,
        pushNotificationsBusy: false,
        pushNotificationsSubscribed: false,
        pushNotificationsSupported: true,
        notificationPermission: 'default',
        fastingPushNotificationsEnabled: true,
        socialPushNotificationsEnabled: true,
        fastingCheckInReminderHours: 4,
        fastingCheckInFollowUpReminderHours: 8,
        isSchedulingTestNotification: false,
        isLoadingConnectedDevices: false,
        removingConnectedDeviceEndpoint: null,
        ...overrides,
    };

    fixture.componentRef.setInput('pushNotificationsEnabled', inputs.pushNotificationsEnabled);
    fixture.componentRef.setInput('isUpdatingNotifications', inputs.isUpdatingNotifications);
    fixture.componentRef.setInput('pushNotificationsBusy', inputs.pushNotificationsBusy);
    fixture.componentRef.setInput('pushNotificationsSubscribed', inputs.pushNotificationsSubscribed);
    fixture.componentRef.setInput('pushNotificationsSupported', inputs.pushNotificationsSupported);
    fixture.componentRef.setInput('notificationPermission', inputs.notificationPermission);
    fixture.componentRef.setInput('fastingPushNotificationsEnabled', inputs.fastingPushNotificationsEnabled);
    fixture.componentRef.setInput('socialPushNotificationsEnabled', inputs.socialPushNotificationsEnabled);
    fixture.componentRef.setInput('fastingReminderPresets', FASTING_REMINDER_PRESETS);
    fixture.componentRef.setInput('fastingCheckInReminderHours', inputs.fastingCheckInReminderHours);
    fixture.componentRef.setInput('fastingCheckInFollowUpReminderHours', inputs.fastingCheckInFollowUpReminderHours);
    fixture.componentRef.setInput('isSchedulingTestNotification', inputs.isSchedulingTestNotification);
    fixture.componentRef.setInput('isLoadingConnectedDevices', inputs.isLoadingConnectedDevices);
    fixture.componentRef.setInput('connectedDeviceItems', []);
    fixture.componentRef.setInput('removingConnectedDeviceEndpoint', inputs.removingConnectedDeviceEndpoint);
    fixture.detectChanges();
}

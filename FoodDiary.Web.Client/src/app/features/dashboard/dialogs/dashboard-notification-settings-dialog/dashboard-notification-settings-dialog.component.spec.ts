import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { NavigationService } from '../../../../services/navigation.service';
import { NotificationService } from '../../../../services/notification.service';
import { PushNotificationService } from '../../../../services/push-notification.service';
import { DashboardNotificationSettingsDialogComponent } from './dashboard-notification-settings-dialog.component';

type NotificationSettingsContext = {
    component: DashboardNotificationSettingsDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<DashboardNotificationSettingsDialogComponent>;
    navigationService: { navigateToProfileAsync: ReturnType<typeof vi.fn> };
    notificationService: {
        getNotificationPreferences: ReturnType<typeof vi.fn>;
        updateNotificationPreferences: ReturnType<typeof vi.fn>;
    };
    pushNotifications: {
        ensureSubscriptionAsync: ReturnType<typeof vi.fn>;
        isBusy: ReturnType<typeof signal<boolean>>;
        isSubscribed: ReturnType<typeof signal<boolean>>;
        isSupported: ReturnType<typeof signal<boolean>>;
    };
    toastService: { info: ReturnType<typeof vi.fn>; success: ReturnType<typeof vi.fn> };
};

async function setupNotificationSettingsAsync(): Promise<NotificationSettingsContext> {
    const notificationService = {
        getNotificationPreferences: vi.fn(() => of(createNotificationPreferences())),
        updateNotificationPreferences: vi.fn(),
    };
    const pushNotifications = {
        isSupported: signal(true),
        isSubscribed: signal(false),
        isBusy: signal(false),
        ensureSubscriptionAsync: vi.fn(() => Promise.resolve('subscribed')),
    };
    const navigationService = { navigateToProfileAsync: vi.fn(() => Promise.resolve()) };
    const dialogRef = { close: vi.fn() };
    const toastService = { success: vi.fn(), info: vi.fn() };
    const frontendObservability = {
        recordNotificationSettingsViewed: vi.fn(),
        recordNotificationPreferenceChanged: vi.fn(),
        recordNotificationSubscriptionEvent: vi.fn(),
    };

    await TestBed.configureTestingModule({
        imports: [DashboardNotificationSettingsDialogComponent, TranslateModule.forRoot()],
        providers: [
            { provide: NotificationService, useValue: notificationService },
            { provide: PushNotificationService, useValue: pushNotifications },
            { provide: NavigationService, useValue: navigationService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FdUiToastService, useValue: toastService },
            { provide: FrontendObservabilityService, useValue: frontendObservability },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardNotificationSettingsDialogComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, dialogRef, fixture, navigationService, notificationService, pushNotifications, toastService };
}

describe('DashboardNotificationSettingsDialogComponent loading', () => {
    it('loads notification preferences on init', async () => {
        const { component, notificationService } = await setupNotificationSettingsAsync();

        expect(notificationService.getNotificationPreferences).toHaveBeenCalledTimes(1);
        expect(component.pushNotificationsEnabled()).toBe(true);
        expect(component.fastingPushNotificationsEnabled()).toBe(true);
        expect(component.socialPushNotificationsEnabled()).toBe(false);
    });

    it('shows an error when preferences fail to load', async () => {
        const { notificationService } = await setupNotificationSettingsAsync();
        notificationService.getNotificationPreferences.mockReturnValueOnce(throwError(() => new Error('load failed')));

        const errorFixture = TestBed.createComponent(DashboardNotificationSettingsDialogComponent);
        const errorComponent = errorFixture.componentInstance;
        errorFixture.detectChanges();

        expect(errorComponent.submitError()).toBe('DASHBOARD.NOTIFICATIONS.LOAD_ERROR');
    });
});

describe('DashboardNotificationSettingsDialogComponent actions', () => {
    it('enables push notifications and ensures device subscription', async () => {
        const { component, fixture, notificationService, pushNotifications, toastService } = await setupNotificationSettingsAsync();
        component.pushNotificationsEnabled.set(false);
        notificationService.updateNotificationPreferences.mockReturnValue(of(createNotificationPreferences()));

        component.togglePushNotifications();
        await fixture.whenStable();

        expect(notificationService.updateNotificationPreferences).toHaveBeenCalledWith({ pushNotificationsEnabled: true });
        expect(pushNotifications.ensureSubscriptionAsync).toHaveBeenCalledTimes(1);
        expect(toastService.success).toHaveBeenCalled();
    });

    it('opens profile settings from the dialog footer action', async () => {
        const { component, dialogRef, navigationService } = await setupNotificationSettingsAsync();
        await component.openAdvancedSettingsAsync();

        expect(dialogRef.close).toHaveBeenCalledTimes(1);
        expect(navigationService.navigateToProfileAsync).toHaveBeenCalledTimes(1);
    });
});

function createNotificationPreferences(): {
    fastingCheckInFollowUpReminderHours: number;
    fastingCheckInReminderHours: number;
    fastingPushNotificationsEnabled: boolean;
    pushNotificationsEnabled: boolean;
    socialPushNotificationsEnabled: boolean;
} {
    return {
        pushNotificationsEnabled: true,
        fastingPushNotificationsEnabled: true,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: 12,
        fastingCheckInFollowUpReminderHours: 20,
    };
}

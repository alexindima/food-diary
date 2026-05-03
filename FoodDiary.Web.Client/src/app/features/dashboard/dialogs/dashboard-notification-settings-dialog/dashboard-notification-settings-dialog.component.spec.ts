import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { NavigationService } from '../../../../services/navigation.service';
import { NotificationService } from '../../../../services/notification.service';
import { PushNotificationService } from '../../../../services/push-notification.service';
import { DashboardNotificationSettingsDialogComponent } from './dashboard-notification-settings-dialog.component';

describe('DashboardNotificationSettingsDialogComponent', () => {
    let fixture: ComponentFixture<DashboardNotificationSettingsDialogComponent>;
    let component: DashboardNotificationSettingsDialogComponent;
    let notificationService: {
        getNotificationPreferences: ReturnType<typeof vi.fn>;
        updateNotificationPreferences: ReturnType<typeof vi.fn>;
    };
    let pushNotifications: {
        isSupported: ReturnType<typeof signal<boolean>>;
        isSubscribed: ReturnType<typeof signal<boolean>>;
        isBusy: ReturnType<typeof signal<boolean>>;
        ensureSubscription: ReturnType<typeof vi.fn>;
    };
    let navigationService: { navigateToProfile: ReturnType<typeof vi.fn> };
    let dialogRef: { close: ReturnType<typeof vi.fn> };
    let toastService: { success: ReturnType<typeof vi.fn>; info: ReturnType<typeof vi.fn> };
    let frontendObservability: {
        recordNotificationSettingsViewed: ReturnType<typeof vi.fn>;
        recordNotificationPreferenceChanged: ReturnType<typeof vi.fn>;
        recordNotificationSubscriptionEvent: ReturnType<typeof vi.fn>;
    };

    beforeEach(async () => {
        notificationService = {
            getNotificationPreferences: vi.fn(() =>
                of({
                    pushNotificationsEnabled: true,
                    fastingPushNotificationsEnabled: true,
                    socialPushNotificationsEnabled: false,
                    fastingCheckInReminderHours: 12,
                    fastingCheckInFollowUpReminderHours: 20,
                }),
            ),
            updateNotificationPreferences: vi.fn(),
        };

        pushNotifications = {
            isSupported: signal(true),
            isSubscribed: signal(false),
            isBusy: signal(false),
            ensureSubscription: vi.fn(async () => 'subscribed'),
        };

        navigationService = {
            navigateToProfile: vi.fn(async () => {}),
        };

        dialogRef = {
            close: vi.fn(),
        };

        toastService = {
            success: vi.fn(),
            info: vi.fn(),
        };

        frontendObservability = {
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

        fixture = TestBed.createComponent(DashboardNotificationSettingsDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('loads notification preferences on init', () => {
        expect(notificationService.getNotificationPreferences).toHaveBeenCalledTimes(1);
        expect(component.pushNotificationsEnabled()).toBe(true);
        expect(component.fastingPushNotificationsEnabled()).toBe(true);
        expect(component.socialPushNotificationsEnabled()).toBe(false);
    });

    it('enables push notifications and ensures device subscription', async () => {
        component.pushNotificationsEnabled.set(false);
        notificationService.updateNotificationPreferences.mockReturnValue(
            of({
                pushNotificationsEnabled: true,
                fastingPushNotificationsEnabled: true,
                socialPushNotificationsEnabled: false,
                fastingCheckInReminderHours: 12,
                fastingCheckInFollowUpReminderHours: 20,
            }),
        );

        component.togglePushNotifications();
        await fixture.whenStable();

        expect(notificationService.updateNotificationPreferences).toHaveBeenCalledWith({ pushNotificationsEnabled: true });
        expect(pushNotifications.ensureSubscription).toHaveBeenCalledTimes(1);
        expect(toastService.success).toHaveBeenCalled();
    });

    it('shows an error when preferences fail to load', () => {
        notificationService.getNotificationPreferences.mockReturnValueOnce(throwError(() => new Error('load failed')));

        fixture = TestBed.createComponent(DashboardNotificationSettingsDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();

        expect(component.submitError()).toBe('DASHBOARD.NOTIFICATIONS.LOAD_ERROR');
    });

    it('opens profile settings from the dialog footer action', async () => {
        await component.openAdvancedSettings();

        expect(dialogRef.close).toHaveBeenCalledTimes(1);
        expect(navigationService.navigateToProfile).toHaveBeenCalledTimes(1);
    });
});

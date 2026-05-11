import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';
import { NavigationService } from '../../../services/navigation.service';
import { NotificationService } from '../../../services/notification.service';
import { type UserProfileOverview, UserService } from '../../../shared/api/user.service';
import { UpdateUserDto, type User } from '../../../shared/models/user.data';
import { ProfileManageFacade } from './profile-manage.facade';

const AUTOSAVE_DEBOUNCE_MS = 700;

describe('ProfileManageFacade', () => {
    let facade: ProfileManageFacade;
    let userService: {
        getOverview: ReturnType<typeof vi.fn>;
        update: ReturnType<typeof vi.fn>;
        deleteCurrentUser: ReturnType<typeof vi.fn>;
    };
    let notificationService: {
        updateNotificationPreferences: ReturnType<typeof vi.fn>;
        removeWebPushSubscription: ReturnType<typeof vi.fn>;
    };
    let dialogService: { open: ReturnType<typeof vi.fn> };
    let authService: { onLogoutAsync: ReturnType<typeof vi.fn>; startAdminSso: ReturnType<typeof vi.fn> };
    let localizationService: { applyLanguagePreferenceAsync: ReturnType<typeof vi.fn> };
    let navigationService: { navigateToHomeAsync: ReturnType<typeof vi.fn> };

    const user: User = {
        id: 'u1',
        email: 'test@example.com',
        hasPassword: false,
        language: 'ru',
        isActive: true,
        isEmailConfirmed: true,
        pushNotificationsEnabled: true,
        fastingPushNotificationsEnabled: false,
        socialPushNotificationsEnabled: true,
        fastingCheckInReminderHours: 12,
        fastingCheckInFollowUpReminderHours: 20,
    };

    const overview: UserProfileOverview = {
        user,
        notificationPreferences: {
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: false,
            socialPushNotificationsEnabled: true,
            fastingCheckInReminderHours: 12,
            fastingCheckInFollowUpReminderHours: 20,
        },
        webPushSubscriptions: [
            {
                endpoint: 'https://push.example.com/subscriptions/current',
                endpointHost: 'push.example.com',
                expirationTimeUtc: null,
                locale: 'en',
                userAgent: 'Chrome',
                createdAtUtc: '2026-04-10T10:00:00Z',
                updatedAtUtc: null,
            },
        ],
        dietologistRelationship: null,
    };

    beforeEach(() => {
        vi.useFakeTimers();
        userService = {
            getOverview: vi.fn().mockReturnValue(of(overview)),
            update: vi.fn().mockReturnValue(of(user)),
            deleteCurrentUser: vi.fn().mockReturnValue(of(true)),
        };
        notificationService = {
            updateNotificationPreferences: vi.fn().mockReturnValue(
                of({
                    pushNotificationsEnabled: false,
                    fastingPushNotificationsEnabled: true,
                    socialPushNotificationsEnabled: true,
                    fastingCheckInReminderHours: 12,
                    fastingCheckInFollowUpReminderHours: 20,
                }),
            ),
            removeWebPushSubscription: vi.fn().mockReturnValue(of(undefined)),
        };
        dialogService = {
            open: vi.fn(),
        };
        authService = {
            onLogoutAsync: vi.fn().mockResolvedValue(undefined),
            startAdminSso: vi.fn().mockReturnValue(of({ code: 'abc123', expiresAtUtc: '2026-04-02T00:00:00Z' })),
        };
        localizationService = {
            applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined),
        };
        navigationService = {
            navigateToHomeAsync: vi.fn().mockResolvedValue(undefined),
        };

        dialogService.open.mockReturnValue({ afterClosed: () => of(false) });

        TestBed.configureTestingModule({
            providers: [
                ProfileManageFacade,
                { provide: UserService, useValue: userService },
                { provide: NotificationService, useValue: notificationService },
                { provide: FdUiDialogService, useValue: dialogService },
                { provide: AuthService, useValue: authService },
                { provide: LocalizationService, useValue: localizationService },
                { provide: NavigationService, useValue: navigationService },
                {
                    provide: TranslateService,
                    useValue: {
                        instant: vi.fn((key: string) => key),
                    },
                },
            ],
        });

        facade = TestBed.inject(ProfileManageFacade);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('loads user and applies language on initialize', () => {
        facade.initialize();

        expect(userService.getOverview).toHaveBeenCalledTimes(1);
        expect(facade.user()).toEqual(expect.objectContaining(user));
        expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
        expect(facade.user()?.pushNotificationsEnabled).toBe(true);
        expect(facade.webPushSubscriptions()).toHaveLength(1);
        expect(facade.globalError()).toBeNull();
    });

    it('sets global error when update returns null', () => {
        userService.update.mockReturnValueOnce(of(null));

        facade.submitUpdate(new UpdateUserDto({ username: 'alex' }));

        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');
    });

    it('shows success dialog and navigates home after successful update', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });

        facade.submitUpdate(new UpdateUserDto({ username: 'alex' }));

        expect(userService.update).toHaveBeenCalledTimes(1);
        expect(dialogService.open).toHaveBeenCalled();
        expect(navigationService.navigateToHomeAsync).toHaveBeenCalled();
    });

    it('opens password success dialog after successful password dialog close', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of(undefined) });
        facade.user.set(user);

        facade.openChangePasswordDialog();

        expect(dialogService.open).toHaveBeenCalledTimes(2);
        expect(dialogService.open.mock.calls[0][1]).toEqual(
            expect.objectContaining({
                data: { hasPassword: false },
            }),
        );
        expect(facade.user()?.hasPassword).toBe(true);
    });

    it('logs out after confirmed successful account deletion', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });

        facade.deleteAccount();

        expect(userService.deleteCurrentUser).toHaveBeenCalledTimes(1);
        expect(authService.onLogoutAsync).toHaveBeenCalledWith(true);
        expect(facade.isDeleting()).toBe(false);
    });

    it('sets global error when account deletion fails', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });
        userService.deleteCurrentUser.mockReturnValueOnce(throwError(() => new Error('delete failed')));

        facade.deleteAccount();

        expect(facade.globalError()).toBe('USER_MANAGE.DELETE_ACCOUNT_ERROR');
        expect(facade.isDeleting()).toBe(false);
    });

    it('updates notification preferences through notification endpoint', async () => {
        facade.initialize();

        const updatedUser = await facade.updateNotificationPreferencesAsync({ pushNotificationsEnabled: false });

        expect(notificationService.updateNotificationPreferences).toHaveBeenCalledWith({ pushNotificationsEnabled: false });
        expect(updatedUser?.pushNotificationsEnabled).toBe(false);
        expect(updatedUser?.fastingPushNotificationsEnabled).toBe(true);
        expect(facade.globalError()).toBeNull();
    });

    it('removes web push subscription and updates local device list', async () => {
        facade.initialize();

        const removed = await facade.removeWebPushSubscriptionAsync('https://push.example.com/subscriptions/current');

        expect(removed).toBe(true);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith('https://push.example.com/subscriptions/current');
        expect(facade.webPushSubscriptions()).toHaveLength(0);
    });

    it('sets update error when notification preferences request fails', async () => {
        facade.initialize();
        notificationService.updateNotificationPreferences.mockReturnValueOnce(throwError(() => new Error('preferences failed')));

        const updatedUser = await facade.updateNotificationPreferencesAsync({ socialPushNotificationsEnabled: false });

        expect(updatedUser).toBeNull();
        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');
    });

    it('debounces profile autosave and updates user without success dialog', async () => {
        facade.initialize();

        facade.queueProfileAutosave(new UpdateUserDto({ firstName: 'Alex' }));
        facade.queueProfileAutosave(new UpdateUserDto({ firstName: 'Alexa' }));

        expect(userService.update).not.toHaveBeenCalled();

        await vi.advanceTimersByTimeAsync(AUTOSAVE_DEBOUNCE_MS);

        expect(userService.update).toHaveBeenCalledTimes(1);
        expect(userService.update.mock.calls[0][0]).toEqual(expect.objectContaining({ firstName: 'Alexa' }));
        expect(dialogService.open).not.toHaveBeenCalled();
    });

    it('queues the latest autosave payload while a save is in flight', async () => {
        facade.initialize();

        const inFlightUpdate = new Subject<User | null>();
        userService.update.mockReturnValueOnce(inFlightUpdate.asObservable());

        facade.queueProfileAutosave(new UpdateUserDto({ firstName: 'Alex' }));
        await vi.advanceTimersByTimeAsync(AUTOSAVE_DEBOUNCE_MS);
        expect(userService.update).toHaveBeenCalledTimes(1);

        facade.queueProfileAutosave(new UpdateUserDto({ firstName: 'Alexa' }));
        expect(userService.update).toHaveBeenCalledTimes(1);

        inFlightUpdate.next(user);
        inFlightUpdate.complete();
        await Promise.resolve();
        await vi.advanceTimersByTimeAsync(AUTOSAVE_DEBOUNCE_MS);

        expect(userService.update).toHaveBeenCalledTimes(2);
        expect(userService.update.mock.calls[1][0]).toEqual(expect.objectContaining({ firstName: 'Alexa' }));
    });
});

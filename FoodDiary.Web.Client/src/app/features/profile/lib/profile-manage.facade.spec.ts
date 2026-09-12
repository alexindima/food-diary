import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { type UserProfileOverview, UserService } from '../../../shared/api/user.service';
import { TelegramBackupEmailFlowService } from '../../../shared/auth/telegram-backup-email-flow.service';
import { TelegramWebAppService } from '../../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { UpdateUserDto, type User } from '../../../shared/models/user.data';
import { NotificationService } from '../../../shared/notifications/notification.service';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { ThemeService } from '../../../shared/theme/theme.service';
import { ProfileMeasurementsService } from '../api/profile-measurements.service';
import { ProfileManageFacade } from './profile-manage.facade';

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

let facade: ProfileManageFacade;
let userService: {
    getOverview: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    deleteCurrentUser: ReturnType<typeof vi.fn>;
};
let notificationService: {
    updateNotificationPreferences: ReturnType<typeof vi.fn>;
    getWebPushSubscriptions: ReturnType<typeof vi.fn>;
    removeWebPushSubscription: ReturnType<typeof vi.fn>;
};
let dialogService: { open: ReturnType<typeof vi.fn> };
let authService: {
    requestTelegramBackupEmail: ReturnType<typeof vi.fn>;
    unlinkTelegram: ReturnType<typeof vi.fn>;
    onLogoutAsync: ReturnType<typeof vi.fn>;
    startAdminSso: ReturnType<typeof vi.fn>;
    linkGoogle: ReturnType<typeof vi.fn>;
};
let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
let localizationService: { applyLanguagePreferenceAsync: ReturnType<typeof vi.fn> };
let navigationService: { navigateToHomeAsync: ReturnType<typeof vi.fn> };
const telegram = { initializeAsync: vi.fn() };
const browser = { getTelegramInitData: vi.fn() };
const backupEmailFlow = { startAsync: vi.fn(), markSent: vi.fn(), read: vi.fn().mockReturnValue(null), clear: vi.fn() };

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
        getWebPushSubscriptions: vi.fn().mockReturnValue(of(overview.webPushSubscriptions)),
        removeWebPushSubscription: vi.fn().mockReturnValue(of(void 0)),
    };
    dialogService = {
        open: vi.fn(),
    };
    authService = {
        requestTelegramBackupEmail: vi.fn().mockReturnValue(of(void 0)),
        unlinkTelegram: vi.fn().mockReturnValue(of(void 0)),
        onLogoutAsync: vi.fn().mockResolvedValue(void 0),
        startAdminSso: vi.fn().mockReturnValue(of({ code: 'abc123', expiresAtUtc: '2026-04-02T00:00:00Z' })),
        linkGoogle: vi.fn().mockReturnValue(of({ ...user, hasGoogleIdentity: true })),
    };
    toastService = {
        success: vi.fn(),
        error: vi.fn(),
    };
    localizationService = {
        applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(void 0),
    };
    navigationService = {
        navigateToHomeAsync: vi.fn().mockResolvedValue(void 0),
    };

    dialogService.open.mockReturnValue({ afterClosed: () => of(false) });
    telegram.initializeAsync.mockResolvedValue(void 0);
    browser.getTelegramInitData.mockReturnValue('signed-proof');
    backupEmailFlow.startAsync.mockResolvedValue(void 0);

    TestBed.configureTestingModule({
        providers: [
            ProfileManageFacade,
            { provide: TelegramBackupEmailFlowService, useValue: backupEmailFlow },
            { provide: ThemeService, useValue: { syncWithUserPreferences: vi.fn() } },
            {
                provide: ProfileMeasurementsService,
                useValue: { getLatest: vi.fn().mockReturnValue(of({ weightKg: null, waistCm: null })) },
            },
            { provide: TelegramWebAppService, useValue: telegram },
            { provide: BrowserWindowService, useValue: browser },
            { provide: UserService, useValue: userService },
            { provide: NotificationService, useValue: notificationService },
            { provide: FdUiDialogService, useValue: dialogService },
            { provide: FdUiToastService, useValue: toastService },
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

describe('ProfileManageFacade Google linking', () => {
    it('updates the current user and confirms successful linking', () => {
        facade.initialize();

        facade.linkGoogle('google-credential');

        expect(authService.linkGoogle).toHaveBeenCalledWith('google-credential');
        expect(facade.user()?.hasGoogleIdentity).toBe(true);
        expect(facade.isLinkingGoogle()).toBe(false);
        expect(toastService.success).toHaveBeenCalledWith('USER_MANAGE.GOOGLE_LINK_SUCCESS');
    });

    it('keeps the account unlinked and reports a failed linking attempt', () => {
        facade.initialize();
        authService.linkGoogle.mockReturnValue(throwError(() => new Error('failed')));

        facade.linkGoogle('google-credential');

        expect(facade.user()?.hasGoogleIdentity).not.toBe(true);
        expect(facade.globalError()).toBe('USER_MANAGE.GOOGLE_LINK_ERROR');
        expect(toastService.error).toHaveBeenCalledWith('USER_MANAGE.GOOGLE_LINK_ERROR');
    });
});

afterEach(() => {
    vi.useRealTimers();
});

describe('ProfileManageFacade loading and submit', () => {
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
});

describe('ProfileManageFacade backup email', () => {
    it('requests backup email without changing the current account address', async () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true });
        await facade.requestBackupEmailAsync(' backup@example.com ');
        expect(authService.requestTelegramBackupEmail).toHaveBeenCalledWith('backup@example.com', 'signed-proof');
        expect(facade.backupEmailSentTo()).toBe('backup@example.com');
        expect(facade.user()?.email).toBeNull();
        expect(facade.isRequestingBackupEmail()).toBe(false);
    });

    it('starts browser reauthentication when Mini App proof is unavailable', async () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true });
        browser.getTelegramInitData.mockReturnValue(null);
        await facade.requestBackupEmailAsync('backup@example.com');
        expect(authService.requestTelegramBackupEmail).not.toHaveBeenCalled();
        expect(facade.backupEmailSentTo()).toBeNull();
        expect(backupEmailFlow.startAsync).toHaveBeenCalledWith('backup@example.com');
        expect(facade.globalError()).toBeNull();
    });

    it('reports a failed backup email request without claiming delivery', async () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true });
        authService.requestTelegramBackupEmail.mockReturnValue(throwError(() => new Error('expired proof')));
        await facade.requestBackupEmailAsync('backup@example.com');
        expect(facade.backupEmailSentTo()).toBeNull();
        expect(facade.user()?.email).toBeNull();
        expect(facade.globalError()).toBe('USER_MANAGE.BACKUP_EMAIL_ERROR');
    });

    it('requires confirmed email before showing password setup', () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true });
        facade.openChangePasswordDialog();
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(facade.globalError()).toBe('USER_MANAGE.BACKUP_EMAIL_PASSWORD_REQUIRED');
    });
});

describe('ProfileManageFacade account actions', () => {
    it('does not unlink the last sign-in method', async () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true, hasGoogleIdentity: false });
        await facade.unlinkTelegramAsync();
        expect(facade.globalError()).toBe('USER_MANAGE.TELEGRAM_BACKUP_REQUIRED');
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(authService.unlinkTelegram).not.toHaveBeenCalled();
    });

    it('does not unlink after cancelling confirmation', async () => {
        facade.user.set({ ...user, hasTelegramIdentity: true, hasGoogleIdentity: true });
        await facade.unlinkTelegramAsync();
        expect(authService.unlinkTelegram).not.toHaveBeenCalled();
        expect(facade.isUnlinkingTelegram()).toBe(false);
    });

    it('requires Mini App proof before requesting unlink', async () => {
        facade.user.set({ ...user, hasTelegramIdentity: true, hasGoogleIdentity: true });
        dialogService.open.mockReturnValue({ afterClosed: () => of(true) });
        browser.getTelegramInitData.mockReturnValue(null);
        await facade.unlinkTelegramAsync();
        expect(authService.unlinkTelegram).not.toHaveBeenCalled();
        expect(facade.globalError()).toBe('USER_MANAGE.TELEGRAM_REOPEN');
    });

    it('unlinks with proof and ends the revoked session only after success', async () => {
        facade.user.set({ ...user, hasTelegramIdentity: true, hasGoogleIdentity: true });
        dialogService.open.mockReturnValue({ afterClosed: () => of(true) });
        await facade.unlinkTelegramAsync();
        expect(authService.unlinkTelegram).toHaveBeenCalledWith('signed-proof');
        expect(authService.onLogoutAsync).toHaveBeenCalledWith(true);
        expect(facade.user()?.hasTelegramIdentity).toBe(false);
    });

    it('keeps the current binding and session when unlink fails', async () => {
        facade.user.set({ ...user, hasTelegramIdentity: true, hasGoogleIdentity: true });
        dialogService.open.mockReturnValue({ afterClosed: () => of(true) });
        authService.unlinkTelegram.mockReturnValue(throwError(() => new Error('stale proof')));
        await facade.unlinkTelegramAsync();
        expect(authService.onLogoutAsync).not.toHaveBeenCalled();
        expect(facade.user()?.hasTelegramIdentity).toBe(true);
        expect(facade.globalError()).toBe('USER_MANAGE.TELEGRAM_UNLINK_ERROR');
    });
});

describe('ProfileManageFacade password and deletion actions', () => {
    it('opens password success dialog after successful password dialog close', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of(void 0) });
        facade.user.set(user);

        facade.openChangePasswordDialog();

        expect(dialogService.open).toHaveBeenCalledTimes(2);
        expect(dialogService.open.mock.calls[0][1]).toEqual(
            expect.objectContaining({
                data: { hasPassword: false },
            }),
        );
        expect(facade.user()?.hasPassword).toBe(true);
        expect(authService.onLogoutAsync).toHaveBeenCalledWith(true);
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

    it('does not start account deletion while profile autosave is in flight', () => {
        facade.isSavingProfile.set(true);

        facade.deleteAccount();

        expect(dialogService.open).not.toHaveBeenCalled();
        expect(userService.deleteCurrentUser).not.toHaveBeenCalled();
    });

    it('deletes the account without submitting a profile update', async () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });

        facade.deleteAccount();
        await waitForAsyncTasksAsync();

        expect(userService.deleteCurrentUser).toHaveBeenCalledTimes(1);
        expect(userService.update).not.toHaveBeenCalled();
        expect(authService.onLogoutAsync).toHaveBeenCalledWith(true);
    });
});

describe('ProfileManageFacade notification preferences', () => {
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

    it('keeps latest web push device list when refresh responses complete out of order', () => {
        facade.initialize();
        const firstRefresh = new Subject<typeof overview.webPushSubscriptions>();
        const secondRefresh = new Subject<typeof overview.webPushSubscriptions>();
        notificationService.getWebPushSubscriptions
            .mockReturnValueOnce(firstRefresh.asObservable())
            .mockReturnValueOnce(secondRefresh.asObservable());

        facade.refreshWebPushSubscriptions();
        facade.refreshWebPushSubscriptions();

        firstRefresh.next([
            {
                ...overview.webPushSubscriptions[0],
                endpoint: 'https://push.example.com/subscriptions/stale',
            },
        ]);
        firstRefresh.complete();
        expect(facade.webPushSubscriptions()[0].endpoint).toBe('https://push.example.com/subscriptions/current');

        secondRefresh.next([
            {
                ...overview.webPushSubscriptions[0],
                endpoint: 'https://push.example.com/subscriptions/latest',
            },
        ]);
        secondRefresh.complete();

        expect(facade.webPushSubscriptions()[0].endpoint).toBe('https://push.example.com/subscriptions/latest');
        expect(facade.isLoadingWebPushSubscriptions()).toBe(false);
    });

    it('sets update error when notification preferences request fails', async () => {
        facade.initialize();
        notificationService.updateNotificationPreferences.mockReturnValueOnce(throwError(() => new Error('preferences failed')));

        const updatedUser = await facade.updateNotificationPreferencesAsync({ socialPushNotificationsEnabled: false });

        expect(updatedUser).toBeNull();
        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');
    });
});

describe('ProfileManageFacade explicit profile save', () => {
    it('updates the user immediately without a success dialog', () => {
        facade.initialize();

        facade.saveProfileNow(new UpdateUserDto({ firstName: 'Alexa' }));

        expect(userService.update).toHaveBeenCalledTimes(1);
        expect(userService.update.mock.calls[0][0]).toEqual(expect.objectContaining({ firstName: 'Alexa' }));
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(facade.profileSavedVersion()).toBe(1);
    });

    it('ignores another save while a profile save is in flight', () => {
        facade.initialize();

        const inFlightUpdate = new Subject<User | null>();
        userService.update.mockReturnValueOnce(inFlightUpdate.asObservable());

        facade.saveProfileNow(new UpdateUserDto({ firstName: 'Alex' }));
        expect(userService.update).toHaveBeenCalledTimes(1);

        facade.saveProfileNow(new UpdateUserDto({ firstName: 'Alexa' }));
        expect(userService.update).toHaveBeenCalledTimes(1);
    });
});

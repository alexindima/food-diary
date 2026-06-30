import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { FASTING_REMINDER_PRESETS } from '../../../../shared/lib/fasting-reminder-presets';
import type { DietologistRelationship } from '../../../../shared/models/dietologist.data';
import { Gender, type User } from '../../../../shared/models/user.data';
import { NotificationService, type WebPushSubscriptionItem } from '../../../../shared/notifications/notification.service';
import { PushNotificationService } from '../../../../shared/notifications/push-notification.service';
import { DietologistFacade } from '../../../dietologist/lib/dietologist.facade';
import { PremiumBillingFacade } from '../../../premium/lib/premium-billing.facade';
import { ProfileManageFacade } from '../../lib/profile-manage.facade';
import { UserManageComponent } from './user-manage';
import {
    DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS,
    DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS,
} from './user-manage-lib/user-manage.config';
import { UserManageNotificationsFacade } from './user-manage-lib/user-manage-notifications.facade';

let fixture: ComponentFixture<UserManageComponent>;
let component: UserManageComponent;
let dietologistService: DietologistServiceMock;
let facade: ProfileManageFacadeMock;
let dialogService: { open: ReturnType<typeof vi.fn> };
let router: { navigate: ReturnType<typeof vi.fn> };
let notificationService: NotificationServiceMock;
let notificationsFacade: UserManageNotificationsFacadeMock;

describe('UserManageComponent dietologist invite state', () => {
    it('keeps invite mode when no dietologist relationship exists', async () => {
        await createComponentAsync(null);

        expect(dietologistService.getRelationship).not.toHaveBeenCalled();
        expect(component['hasDietologistRelationship']()).toBe(false);
        expect(component['dietologistForm'].email().disabled()).toBe(false);
        expect(component['dietologistFormModel']().shareProfile).toBe(true);
        expect(component['dietologistFormModel']().shareMeals).toBe(true);
        expect(component['dietologistFormModel']().shareFasting).toBe(true);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.DIETOLOGIST_INVITE_ACTION');
    });

    it('applies pending relationship state and disables email editing', async () => {
        await createComponentAsync({
            invitationId: 'inv-1',
            status: 'Pending',
            email: 'diet@example.com',
            firstName: null,
            lastName: null,
            dietologistUserId: null,
            permissions: {
                shareProfile: true,
                shareMeals: true,
                shareStatistics: false,
                shareWeight: true,
                shareWaist: false,
                shareGoals: true,
                shareHydration: false,
                shareFasting: true,
            },
            createdAtUtc: '2026-04-15T00:00:00Z',
            expiresAtUtc: '2026-04-22T00:00:00Z',
            acceptedAtUtc: null,
        });

        expect(component['hasDietologistRelationship']()).toBe(true);
        expect(component['isDietologistPending']()).toBe(true);
        expect(component['dietologistForm'].email().disabled()).toBe(true);
        expect(component['dietologistFormModel']().email).toBe('diet@example.com');
        expect(component['dietologistFormModel']().shareProfile).toBe(true);
        expect(component['dietologistFormModel']().shareStatistics).toBe(false);
        expect(component['dietologistFormModel']().shareHydration).toBe(false);
        expect(component['dietologistFormModel']().shareFasting).toBe(true);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.DIETOLOGIST_CANCEL_INVITE');
        expect(host.textContent).not.toContain('USER_MANAGE.DIETOLOGIST_SAVE_PERMISSIONS');
    });
});

describe('UserManageComponent dietologist profile sharing', () => {
    it('asks for confirmation before disabling profile sharing', async () => {
        await createComponentAsync(null, false);

        component['onDietologistProfileToggle'](false);

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(component['dietologistFormModel']().shareProfile).toBe(true);
    });

    it('disables profile sharing after confirmation', async () => {
        await createComponentAsync(null, true);

        component['onDietologistProfileToggle'](false);
        fixture.detectChanges();

        expect(component['dietologistFormModel']().shareProfile).toBe(false);
    });
});

describe('UserManageComponent dietologist permissions', () => {
    it('autosaves permissions when a relationship toggle changes', async () => {
        await createComponentAsync({
            invitationId: 'inv-1',
            status: 'Accepted',
            email: 'diet@example.com',
            firstName: null,
            lastName: null,
            dietologistUserId: 'diet-1',
            permissions: {
                shareProfile: true,
                shareMeals: true,
                shareStatistics: true,
                shareWeight: true,
                shareWaist: true,
                shareGoals: true,
                shareHydration: true,
                shareFasting: true,
            },
            createdAtUtc: '2026-04-15T00:00:00Z',
            expiresAtUtc: '2026-04-22T00:00:00Z',
            acceptedAtUtc: '2026-04-15T01:00:00Z',
        });

        component['updateDietologistPermission']('shareFasting', false);

        expect(dietologistService.updatePermissions).toHaveBeenCalledWith({
            shareProfile: true,
            shareMeals: true,
            shareStatistics: true,
            shareWeight: true,
            shareWaist: true,
            shareGoals: true,
            shareHydration: true,
            shareFasting: false,
        });
    });

    it('updates relationship permissions without reloading the dietologist section', async () => {
        await createComponentAsync({
            invitationId: 'inv-1',
            status: 'Accepted',
            email: 'diet@example.com',
            firstName: null,
            lastName: null,
            dietologistUserId: 'diet-1',
            permissions: {
                shareProfile: true,
                shareMeals: true,
                shareStatistics: true,
                shareWeight: true,
                shareWaist: true,
                shareGoals: true,
                shareHydration: true,
                shareFasting: true,
            },
            createdAtUtc: '2026-04-15T00:00:00Z',
            expiresAtUtc: '2026-04-22T00:00:00Z',
            acceptedAtUtc: '2026-04-15T01:00:00Z',
        });

        component['updateDietologistPermission']('shareMeals', false);

        expect(dietologistService.getRelationship).not.toHaveBeenCalled();
        expect(facade.dietologistRelationship()?.permissions.shareMeals).toBe(false);
        expect(component['isLoadingDietologist']()).toBe(false);
    });
});

describe('UserManageComponent dietologist disconnect', () => {
    it('asks for confirmation before disconnecting an accepted dietologist relationship', async () => {
        await createComponentAsync(
            {
                invitationId: 'inv-1',
                status: 'Accepted',
                email: 'diet@example.com',
                firstName: null,
                lastName: null,
                dietologistUserId: 'diet-1',
                permissions: {
                    shareProfile: true,
                    shareMeals: true,
                    shareStatistics: true,
                    shareWeight: true,
                    shareWaist: true,
                    shareGoals: true,
                    shareHydration: true,
                    shareFasting: true,
                },
                createdAtUtc: '2026-04-15T00:00:00Z',
                expiresAtUtc: '2026-04-22T00:00:00Z',
                acceptedAtUtc: '2026-04-15T01:00:00Z',
            },
            false,
        );

        component['revokeDietologistRelationship']();

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(dietologistService.revokeRelationship).not.toHaveBeenCalled();
    });

    it('disconnects after confirmation for an accepted relationship', async () => {
        await createComponentAsync(
            {
                invitationId: 'inv-1',
                status: 'Accepted',
                email: 'diet@example.com',
                firstName: null,
                lastName: null,
                dietologistUserId: 'diet-1',
                permissions: {
                    shareProfile: true,
                    shareMeals: true,
                    shareStatistics: true,
                    shareWeight: true,
                    shareWaist: true,
                    shareGoals: true,
                    shareHydration: true,
                    shareFasting: true,
                },
                createdAtUtc: '2026-04-15T00:00:00Z',
                expiresAtUtc: '2026-04-22T00:00:00Z',
                acceptedAtUtc: '2026-04-15T01:00:00Z',
            },
            true,
        );

        component['revokeDietologistRelationship']();

        expect(dietologistService.revokeRelationship).toHaveBeenCalledTimes(1);
    });
});

describe('UserManageComponent notification relationship refresh', () => {
    it('reloads dietologist relationship when notifications realtime changes', async () => {
        await createComponentAsync(null);
        expect(dietologistService.getRelationship).toHaveBeenCalledTimes(0);

        notificationService.notificationsChangedVersion.set(1);
        fixture.detectChanges();

        expect(dietologistService.getRelationship).toHaveBeenCalledTimes(1);
    });
});

describe('UserManageComponent profile autosave feedback', () => {
    it('should prevent native profile form submit when saving now', async () => {
        await createComponentAsync(null);

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(facade.saveProfileNow).toHaveBeenCalledTimes(1);
    });

    it('queues profile autosave when editable user fields change', async () => {
        await createComponentAsync(null);

        component['userForm'].firstName().markAsDirty();
        component['userForm'].firstName().value.set('Alex');
        fixture.detectChanges();

        expect(facade.queueProfileAutosave).toHaveBeenCalledTimes(1);
        expect(facade.queueProfileAutosave.mock.calls[0][0]).toEqual(expect.objectContaining({ firstName: 'Alex' }));
    });

    it('ignores bubbled input events outside profile fields', async () => {
        await createComponentAsync(null);
        const unrelatedInput = document.createElement('input');
        unrelatedInput.value = 'ignored';
        const inputEvent = new Event('input', { bubbles: true });
        Object.defineProperty(inputEvent, 'target', { value: unrelatedInput });

        component['onUserFormInput'](inputEvent);

        expect(facade.queueProfileAutosave).not.toHaveBeenCalled();
    });

    it('queues select changes using the emitted value', async () => {
        await createComponentAsync(null, false, {
            id: 'u1',
            email: 'user@example.com',
            hasPassword: true,
            gender: 'M',
            theme: 'dark',
            uiStyle: 'modern',
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: true,
            socialPushNotificationsEnabled: false,
            fastingCheckInReminderHours: 4,
            fastingCheckInFollowUpReminderHours: 8,
            isActive: true,
            isEmailConfirmed: true,
        });

        component['onUserFormPatch']({ gender: Gender.Female });

        expect(facade.queueProfileAutosave).toHaveBeenCalledTimes(1);
        expect(facade.queueProfileAutosave.mock.calls[0][0]).toEqual(expect.objectContaining({ gender: 'F' }));
    });

    it('reports pending and saving profile states for autosave feedback', async () => {
        await createComponentAsync(null);

        expect(component['profileStatus']().key).toBe('USER_MANAGE.PROFILE_STATUS_SAVED');

        component['userForm'].firstName().markAsDirty();
        component['userForm'].firstName().value.set('Alex');
        fixture.detectChanges();

        expect(component['profileStatus']().key).toBe('USER_MANAGE.PROFILE_STATUS_PENDING');

        facade.isSavingProfile.set(true);
        fixture.detectChanges();
        expect(component['profileStatus']().key).toBe('USER_MANAGE.PROFILE_STATUS_SAVING');
    });
});

describe('UserManageComponent profile normalization and intents', () => {
    it('normalizes legacy profile select values from user overview', async () => {
        await createComponentAsync(null, false, {
            id: 'u1',
            email: 'user@example.com',
            hasPassword: true,
            username: 'alexi',
            firstName: 'Alex',
            lastName: 'User',
            gender: 'female',
            language: 'en-US',
            theme: 'default',
            uiStyle: 'classic',
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: true,
            socialPushNotificationsEnabled: false,
            fastingCheckInReminderHours: 4,
            fastingCheckInFollowUpReminderHours: 8,
            isActive: true,
            isEmailConfirmed: true,
        });

        expect(component['userFormModel']().gender).toBe('F');
        expect(component['userFormModel']().language).toBe('en');
        expect(component['userFormModel']().theme).toBe('ocean');
        expect(component['userFormModel']().uiStyle).toBe('classic');
    });

    it('opens set password dialog from notifications intent for google-only account', async () => {
        await createComponentAsync(
            null,
            false,
            {
                id: 'u1',
                email: 'user@example.com',
                hasPassword: false,
                pushNotificationsEnabled: true,
                fastingPushNotificationsEnabled: true,
                socialPushNotificationsEnabled: true,
                fastingCheckInReminderHours: 4,
                fastingCheckInFollowUpReminderHours: 8,
                isActive: true,
                isEmailConfirmed: true,
            },
            { intent: 'set-password' },
        );

        expect(facade.openChangePasswordDialog).toHaveBeenCalledTimes(1);
        const navigateOptions = router.navigate.mock.calls[0]?.[1] as
            { queryParams?: Record<string, unknown>; queryParamsHandling?: string; replaceUrl?: boolean } | undefined;
        if (navigateOptions === undefined) {
            throw new Error('Expected router navigation options.');
        }

        expect(router.navigate.mock.calls[0]?.[0]).toEqual([]);
        expect(navigateOptions.queryParams).toEqual({ intent: null });
        expect(navigateOptions.queryParamsHandling).toBe('merge');
        expect(navigateOptions.replaceUrl).toBe(true);
    });
});

type DietologistServiceMock = {
    getRelationship: ReturnType<typeof vi.fn>;
    invite: ReturnType<typeof vi.fn>;
    updatePermissions: ReturnType<typeof vi.fn>;
    revokeRelationship: ReturnType<typeof vi.fn>;
};

type NotificationServiceMock = {
    scheduleTestNotification: ReturnType<typeof vi.fn>;
    notificationsChangedVersion: ReturnType<typeof signal<number>>;
};

type UserManageNotificationsFacadeMock = {
    notificationPermission: ReturnType<typeof signal<NotificationPermission | 'unsupported'>>;
    notificationsChangedVersion: ReturnType<typeof signal<number>>;
    isUpdatingNotifications: ReturnType<typeof signal<boolean>>;
    isSchedulingTestNotification: ReturnType<typeof signal<boolean>>;
    pushNotificationsEnabled: ReturnType<typeof signal<boolean>>;
    fastingPushNotificationsEnabled: ReturnType<typeof signal<boolean>>;
    socialPushNotificationsEnabled: ReturnType<typeof signal<boolean>>;
    fastingCheckInReminderHours: ReturnType<typeof signal<number>>;
    fastingCheckInFollowUpReminderHours: ReturnType<typeof signal<number>>;
    fastingReminderPresets: typeof FASTING_REMINDER_PRESETS;
    pushNotificationsSupported: ReturnType<typeof signal<boolean>>;
    pushNotificationsSubscribed: ReturnType<typeof signal<boolean>>;
    pushNotificationsBusy: ReturnType<typeof signal<boolean>>;
    currentSubscriptionEndpoint: ReturnType<typeof signal<string | null>>;
    connectedDeviceItems: ReturnType<typeof signal<[]>>;
    isLoadingConnectedDevices: ReturnType<typeof signal<boolean>>;
    removingConnectedDeviceEndpoint: ReturnType<typeof signal<string | null>>;
    syncFromUser: ReturnType<typeof vi.fn>;
    togglePushNotificationsAsync: ReturnType<typeof vi.fn>;
    toggleFastingPushNotificationsAsync: ReturnType<typeof vi.fn>;
    toggleSocialPushNotificationsAsync: ReturnType<typeof vi.fn>;
    applyFastingReminderPreset: ReturnType<typeof vi.fn>;
    onFastingReminderHoursChange: ReturnType<typeof vi.fn>;
    saveFastingReminderHoursAsync: ReturnType<typeof vi.fn>;
    scheduleTestNotification: ReturnType<typeof vi.fn>;
    removeConnectedDeviceAsync: ReturnType<typeof vi.fn>;
};

type ProfileManageFacadeMock = {
    user: WritableSignal<User | null>;
    globalError: ReturnType<typeof signal<string | null>>;
    isDeleting: ReturnType<typeof signal<boolean>>;
    isSavingProfile: ReturnType<typeof signal<boolean>>;
    isRevokingAiConsent: ReturnType<typeof signal<boolean>>;
    isUpdatingNotifications: ReturnType<typeof signal<boolean>>;
    webPushSubscriptions: WritableSignal<WebPushSubscriptionItem[]>;
    dietologistRelationship: WritableSignal<DietologistRelationship | null>;
    isLoadingWebPushSubscriptions: ReturnType<typeof signal<boolean>>;
    removingWebPushSubscriptionEndpoint: ReturnType<typeof signal<string | null>>;
    initialize: ReturnType<typeof vi.fn>;
    clearGlobalError: ReturnType<typeof vi.fn>;
    submitUpdate: ReturnType<typeof vi.fn>;
    queueProfileAutosave: ReturnType<typeof vi.fn>;
    saveProfileNow: ReturnType<typeof vi.fn>;
    openChangePasswordDialog: ReturnType<typeof vi.fn>;
    revokeAiConsent: ReturnType<typeof vi.fn>;
    deleteAccount: ReturnType<typeof vi.fn>;
    updateNotificationPreferences: ReturnType<typeof vi.fn>;
    refreshWebPushSubscriptions: ReturnType<typeof vi.fn>;
    removeWebPushSubscription: ReturnType<typeof vi.fn>;
    openAdminPanel: ReturnType<typeof vi.fn>;
};

async function createComponentAsync(
    relationship: DietologistRelationship | null,
    dialogResult = false,
    user: User | null = null,
    queryParams: Record<string, string> = {},
): Promise<void> {
    facade = createFacadeMock(relationship, user);
    dietologistService = createDietologistServiceMock(relationship);
    dialogService = createDialogServiceMock(dialogResult);
    router = {
        navigate: vi.fn().mockResolvedValue(true),
    };
    notificationService = {
        scheduleTestNotification: vi.fn().mockReturnValue(of(void 0)),
        notificationsChangedVersion: signal(0),
    };
    notificationsFacade = createUserManageNotificationsFacadeMock(notificationService.notificationsChangedVersion);

    await TestBed.configureTestingModule({
        imports: [UserManageComponent],
        providers: [...createTestingProviders(queryParams), provideTranslateTesting()],
    })
        .overrideComponent(UserManageComponent, {
            remove: { providers: [ProfileManageFacade, UserManageNotificationsFacade] },
            add: {
                providers: [
                    { provide: ProfileManageFacade, useValue: facade },
                    { provide: UserManageNotificationsFacade, useValue: notificationsFacade },
                ],
            },
        })
        .compileComponents();

    configureTranslateService();

    fixture = TestBed.createComponent(UserManageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

function createTestingProviders(queryParams: Record<string, string>): unknown[] {
    return [
        { provide: DietologistFacade, useValue: dietologistService },
        { provide: PremiumBillingFacade, useValue: createPremiumBillingFacadeMock() },
        { provide: AuthService, useValue: { isAdmin: vi.fn(() => false) } },
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap(queryParams)) } },
        { provide: Router, useValue: router },
        { provide: LocalizationService, useValue: createLocalizationServiceMock() },
        { provide: NotificationService, useValue: notificationService },
        { provide: PushNotificationService, useValue: createPushNotificationServiceMock() },
        { provide: FdUiToastService, useValue: createToastServiceMock() },
        { provide: FdUiDialogService, useValue: dialogService },
        { provide: FrontendObservabilityService, useValue: createFrontendObservabilityServiceMock() },
    ];
}

function createDietologistServiceMock(relationship: DietologistRelationship | null): DietologistServiceMock {
    return {
        getRelationship: vi.fn().mockReturnValue(of(relationship)),
        invite: vi.fn().mockReturnValue(of(void 0)),
        updatePermissions: vi.fn().mockReturnValue(of(void 0)),
        revokeRelationship: vi.fn().mockReturnValue(of(void 0)),
    };
}

function createPremiumBillingFacadeMock(): { getOverview: ReturnType<typeof vi.fn>; createPortalSession: ReturnType<typeof vi.fn> } {
    return {
        getOverview: vi.fn().mockReturnValue(
            of({
                isPremium: false,
                subscriptionStatus: null,
                plan: null,
                subscriptionProvider: null,
                currentPeriodStartUtc: null,
                currentPeriodEndUtc: null,
                nextBillingAttemptUtc: null,
                cancelAtPeriodEnd: false,
                renewalEnabled: false,
                manageBillingAvailable: false,
                premiumTrialStartUtc: null,
                premiumTrialEndUtc: null,
                premiumTrialActive: false,
                premiumTrialUsed: false,
                canStartPremiumTrial: true,
                provider: 'none',
                paddleClientToken: null,
                availableProviders: [],
            }),
        ),
        createPortalSession: vi.fn().mockReturnValue(of({ url: 'https://billing.example/session' })),
    };
}

function createDialogServiceMock(dialogResult: boolean): { open: ReturnType<typeof vi.fn> } {
    return {
        open: vi.fn().mockReturnValue({
            afterClosed: () => of(dialogResult),
        }),
    };
}

function createLocalizationServiceMock(): {
    applyLanguagePreferenceAsync: ReturnType<typeof vi.fn>;
    getCurrentLanguage: ReturnType<typeof vi.fn>;
} {
    return {
        applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(void 0),
        getCurrentLanguage: vi.fn(() => 'en'),
    };
}

function createPushNotificationServiceMock(): {
    isSupported: ReturnType<typeof signal<boolean>>;
    isSubscribed: ReturnType<typeof signal<boolean>>;
    isBusy: ReturnType<typeof signal<boolean>>;
    currentSubscriptionEndpoint: ReturnType<typeof signal<string | null>>;
    ensureSubscriptionAsync: ReturnType<typeof vi.fn>;
    removeSubscriptionAsync: ReturnType<typeof vi.fn>;
} {
    return {
        isSupported: signal(false),
        isSubscribed: signal(false),
        isBusy: signal(false),
        currentSubscriptionEndpoint: signal<string | null>(null),
        ensureSubscriptionAsync: vi.fn().mockResolvedValue('unsupported'),
        removeSubscriptionAsync: vi.fn().mockResolvedValue(true),
    };
}

function createUserManageNotificationsFacadeMock(
    notificationsChangedVersion: ReturnType<typeof signal<number>>,
): UserManageNotificationsFacadeMock {
    return {
        notificationPermission: signal('unsupported'),
        notificationsChangedVersion,
        isUpdatingNotifications: signal(false),
        isSchedulingTestNotification: signal(false),
        pushNotificationsEnabled: signal(false),
        fastingPushNotificationsEnabled: signal(true),
        socialPushNotificationsEnabled: signal(true),
        fastingCheckInReminderHours: signal(DEFAULT_FASTING_CHECK_IN_REMINDER_HOURS),
        fastingCheckInFollowUpReminderHours: signal(DEFAULT_FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS),
        fastingReminderPresets: FASTING_REMINDER_PRESETS,
        pushNotificationsSupported: signal(false),
        pushNotificationsSubscribed: signal(false),
        pushNotificationsBusy: signal(false),
        currentSubscriptionEndpoint: signal<string | null>(null),
        connectedDeviceItems: signal([]),
        isLoadingConnectedDevices: signal(false),
        removingConnectedDeviceEndpoint: signal<string | null>(null),
        syncFromUser: vi.fn(),
        togglePushNotificationsAsync: vi.fn().mockResolvedValue(void 0),
        toggleFastingPushNotificationsAsync: vi.fn().mockResolvedValue(void 0),
        toggleSocialPushNotificationsAsync: vi.fn().mockResolvedValue(void 0),
        applyFastingReminderPreset: vi.fn(),
        onFastingReminderHoursChange: vi.fn(),
        saveFastingReminderHoursAsync: vi.fn().mockResolvedValue(void 0),
        scheduleTestNotification: vi.fn(),
        removeConnectedDeviceAsync: vi.fn().mockResolvedValue(void 0),
    };
}

function createToastServiceMock(): {
    success: ReturnType<typeof vi.fn>;
    info: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
} {
    return {
        success: vi.fn(),
        info: vi.fn(),
        error: vi.fn(),
    };
}

function createFrontendObservabilityServiceMock(): {
    recordNotificationSettingsViewed: ReturnType<typeof vi.fn>;
    recordNotificationPreferenceChanged: ReturnType<typeof vi.fn>;
    recordNotificationSubscriptionEvent: ReturnType<typeof vi.fn>;
    recordFastingReminderPresetSelected: ReturnType<typeof vi.fn>;
    recordFastingReminderTimingSaved: ReturnType<typeof vi.fn>;
} {
    return {
        recordNotificationSettingsViewed: vi.fn(),
        recordNotificationPreferenceChanged: vi.fn(),
        recordNotificationSubscriptionEvent: vi.fn(),
        recordFastingReminderPresetSelected: vi.fn(),
        recordFastingReminderTimingSaved: vi.fn(),
    };
}

function configureTranslateService(): void {
    const translateService = TestBed.inject(TranslateService);
    vi.spyOn(translateService, 'instant').mockImplementation((key: string) => key);
    translateService.setFallbackLang('en');
    translateService.use('en');
}

function createFacadeMock(relationship: DietologistRelationship | null, user: User | null = null): ProfileManageFacadeMock {
    return {
        user: signal(user),
        globalError: signal<string | null>(null),
        isDeleting: signal(false),
        isSavingProfile: signal(false),
        isRevokingAiConsent: signal(false),
        isUpdatingNotifications: signal(false),
        webPushSubscriptions: signal([]),
        dietologistRelationship: signal(relationship),
        isLoadingWebPushSubscriptions: signal(false),
        removingWebPushSubscriptionEndpoint: signal<string | null>(null),
        initialize: vi.fn(),
        clearGlobalError: vi.fn(),
        submitUpdate: vi.fn(),
        queueProfileAutosave: vi.fn(),
        saveProfileNow: vi.fn(),
        openChangePasswordDialog: vi.fn(),
        revokeAiConsent: vi.fn(),
        deleteAccount: vi.fn(),
        updateNotificationPreferences: vi.fn(),
        refreshWebPushSubscriptions: vi.fn(),
        removeWebPushSubscription: vi.fn(),
        openAdminPanel: vi.fn(),
    };
}

import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { LocalizationService } from '../../../../services/localization.service';
import { NotificationService, type WebPushSubscriptionItem } from '../../../../services/notification.service';
import { PushNotificationService } from '../../../../services/push-notification.service';
import { ImageUploadService } from '../../../../shared/api/image-upload.service';
import type { DietologistRelationship } from '../../../../shared/models/dietologist.data';
import type { User } from '../../../../shared/models/user.data';
import { DietologistService } from '../../../dietologist/api/dietologist.service';
import { ProfileManageFacade } from '../../lib/profile-manage.facade';
import { UserManageComponent } from './user-manage.component';

let fixture: ComponentFixture<UserManageComponent>;
let component: UserManageComponent;
let dietologistService: DietologistServiceMock;
let facade: ProfileManageFacadeMock;
let dialogService: { open: ReturnType<typeof vi.fn> };
let router: { navigate: ReturnType<typeof vi.fn> };
let notificationService: NotificationServiceMock;

describe('UserManageComponent dietologist invite state', () => {
    it('keeps invite mode when no dietologist relationship exists', async () => {
        await createComponentAsync(null);

        expect(dietologistService.getRelationship).not.toHaveBeenCalled();
        expect(component.hasDietologistRelationship()).toBe(false);
        expect(component.dietologistForm.controls.email.enabled).toBe(true);
        expect(component.dietologistForm.controls.shareProfile.value).toBe(true);
        expect(component.dietologistForm.controls.shareMeals.value).toBe(true);
        expect(component.dietologistForm.controls.shareFasting.value).toBe(true);
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

        expect(component.hasDietologistRelationship()).toBe(true);
        expect(component.isDietologistPending()).toBe(true);
        expect(component.dietologistForm.controls.email.disabled).toBe(true);
        expect(component.dietologistForm.controls.email.getRawValue()).toBe('diet@example.com');
        expect(component.dietologistForm.controls.shareProfile.value).toBe(true);
        expect(component.dietologistForm.controls.shareStatistics.value).toBe(false);
        expect(component.dietologistForm.controls.shareHydration.value).toBe(false);
        expect(component.dietologistForm.controls.shareFasting.value).toBe(true);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('USER_MANAGE.DIETOLOGIST_CANCEL_INVITE');
        expect(host.textContent).not.toContain('USER_MANAGE.DIETOLOGIST_SAVE_PERMISSIONS');
    });
});

describe('UserManageComponent dietologist profile sharing', () => {
    it('asks for confirmation before disabling profile sharing', async () => {
        await createComponentAsync(null, false);

        component.onDietologistProfileToggle(false);

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(component.dietologistForm.controls.shareProfile.value).toBe(true);
    });

    it('disables profile sharing after confirmation', async () => {
        await createComponentAsync(null, true);

        component.onDietologistProfileToggle(false);
        fixture.detectChanges();

        expect(component.dietologistForm.controls.shareProfile.value).toBe(false);
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

        component.updateDietologistPermission('shareFasting', false);

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

        component.revokeDietologistRelationship();

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

        component.revokeDietologistRelationship();

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
    it('queues profile autosave when editable user fields change', async () => {
        await createComponentAsync(null);

        component.userForm.controls.firstName.markAsDirty();
        component.userForm.controls.firstName.setValue('Alex');

        expect(facade.queueProfileAutosave).toHaveBeenCalledTimes(1);
        expect(facade.queueProfileAutosave.mock.calls[0][0]).toEqual(expect.objectContaining({ firstName: 'Alex' }));
    });

    it('reports pending and saving profile states for autosave feedback', async () => {
        await createComponentAsync(null);

        expect(component.profileStatus().key).toBe('USER_MANAGE.PROFILE_STATUS_SAVED');

        component.userForm.controls.firstName.markAsDirty();
        component.userForm.controls.firstName.setValue('Alex');
        expect(component.profileStatus().key).toBe('USER_MANAGE.PROFILE_STATUS_PENDING');

        facade.isSavingProfile.set(true);
        fixture.detectChanges();
        expect(component.profileStatus().key).toBe('USER_MANAGE.PROFILE_STATUS_SAVING');
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

        expect(component.userForm.controls.gender.value).toBe('F');
        expect(component.userForm.controls.language.value).toBe('en');
        expect(component.userForm.controls.theme.value).toBe('ocean');
        expect(component.userForm.controls.uiStyle.value).toBe('classic');
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
            | { queryParams?: Record<string, unknown>; queryParamsHandling?: string; replaceUrl?: boolean }
            | undefined;
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
        scheduleTestNotification: vi.fn().mockReturnValue(of(undefined)),
        notificationsChangedVersion: signal(0),
    };

    await TestBed.configureTestingModule({
        imports: [UserManageComponent, TranslateModule.forRoot()],
        providers: createTestingProviders(queryParams),
    })
        .overrideComponent(UserManageComponent, {
            remove: { providers: [ProfileManageFacade] },
            add: { providers: [{ provide: ProfileManageFacade, useValue: facade }] },
        })
        .compileComponents();

    configureTranslateService();

    fixture = TestBed.createComponent(UserManageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

function createTestingProviders(queryParams: Record<string, string>): unknown[] {
    return [
        { provide: DietologistService, useValue: dietologistService },
        { provide: ImageUploadService, useValue: { deleteAsset: vi.fn().mockReturnValue(of(undefined)) } },
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
        invite: vi.fn().mockReturnValue(of(undefined)),
        updatePermissions: vi.fn().mockReturnValue(of(undefined)),
        revokeRelationship: vi.fn().mockReturnValue(of(undefined)),
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
        applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined),
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

import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { UserManageComponent } from './user-manage.component';
import { AuthService } from '../../../services/auth.service';
import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import { LocalizationService } from '../../../services/localization.service';
import { NotificationService } from '../../../services/notification.service';
import { PushNotificationService } from '../../../services/push-notification.service';
import { DietologistService } from '../../dietologist/api/dietologist.service';
import { ProfileManageFacade } from '../lib/profile-manage.facade';

describe('UserManageComponent dietologist section', () => {
    let fixture: ComponentFixture<UserManageComponent>;
    let component: UserManageComponent;
    let dietologistService: {
        getRelationship: ReturnType<typeof vi.fn>;
        invite: ReturnType<typeof vi.fn>;
        updatePermissions: ReturnType<typeof vi.fn>;
        revokeRelationship: ReturnType<typeof vi.fn>;
    };
    let facade: ReturnType<typeof createFacadeMock>;
    let dialogService: { open: ReturnType<typeof vi.fn> };
    let notificationService: {
        scheduleTestNotification: ReturnType<typeof vi.fn>;
        notificationsChangedVersion: ReturnType<typeof signal<number>>;
    };

    async function createComponent(relationship: any, dialogResult = false): Promise<void> {
        facade = createFacadeMock(relationship);
        dietologistService = {
            getRelationship: vi.fn().mockReturnValue(of(relationship)),
            invite: vi.fn().mockReturnValue(of(undefined)),
            updatePermissions: vi.fn().mockReturnValue(of(undefined)),
            revokeRelationship: vi.fn().mockReturnValue(of(undefined)),
        };
        dialogService = {
            open: vi.fn().mockReturnValue({
                afterClosed: () => of(dialogResult),
            }),
        };
        notificationService = {
            scheduleTestNotification: vi.fn().mockReturnValue(of(undefined)),
            notificationsChangedVersion: signal(0),
        };

        await TestBed.configureTestingModule({
            imports: [UserManageComponent, TranslateModule.forRoot()],
            providers: [
                { provide: DietologistService, useValue: dietologistService },
                { provide: ImageUploadService, useValue: { deleteAsset: vi.fn().mockReturnValue(of(undefined)) } },
                { provide: AuthService, useValue: { isAdmin: vi.fn(() => false) } },
                {
                    provide: LocalizationService,
                    useValue: {
                        applyLanguagePreference: vi.fn().mockResolvedValue(undefined),
                        getCurrentLanguage: vi.fn(() => 'en'),
                    },
                },
                {
                    provide: NotificationService,
                    useValue: notificationService,
                },
                {
                    provide: PushNotificationService,
                    useValue: {
                        isSupported: signal(false),
                        isSubscribed: signal(false),
                        isBusy: signal(false),
                        currentSubscriptionEndpoint: signal<string | null>(null),
                        ensureSubscription: vi.fn().mockResolvedValue('unsupported'),
                        removeSubscription: vi.fn().mockResolvedValue(true),
                    },
                },
                {
                    provide: FdUiToastService,
                    useValue: {
                        success: vi.fn(),
                        info: vi.fn(),
                        error: vi.fn(),
                    },
                },
                { provide: FdUiDialogService, useValue: dialogService },
                {
                    provide: FrontendObservabilityService,
                    useValue: {
                        recordNotificationSettingsViewed: vi.fn(),
                        recordNotificationPreferenceChanged: vi.fn(),
                        recordNotificationSubscriptionEvent: vi.fn(),
                        recordFastingReminderPresetSelected: vi.fn(),
                        recordFastingReminderTimingSaved: vi.fn(),
                    },
                },
            ],
        })
            .overrideComponent(UserManageComponent, {
                remove: { providers: [ProfileManageFacade] },
                add: { providers: [{ provide: ProfileManageFacade, useValue: facade }] },
            })
            .compileComponents();

        const translateService = TestBed.inject(TranslateService);
        vi.spyOn(translateService, 'instant').mockImplementation(((key: string | string[]) => key as string) as never);
        translateService.setDefaultLang('en');
        translateService.use('en');

        fixture = TestBed.createComponent(UserManageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('keeps invite mode when no dietologist relationship exists', async () => {
        await createComponent(null);

        expect(dietologistService.getRelationship).not.toHaveBeenCalled();
        expect(component.hasDietologistRelationship()).toBe(false);
        expect(component.dietologistForm.controls.email.enabled).toBe(true);
        expect(component.dietologistForm.controls.shareProfile.value).toBe(true);
        expect(component.dietologistForm.controls.shareMeals.value).toBe(true);
        expect(component.dietologistForm.controls.shareFasting.value).toBe(true);
        expect(fixture.nativeElement.textContent).toContain('USER_MANAGE.DIETOLOGIST_INVITE_ACTION');
    });

    it('applies pending relationship state and disables email editing', async () => {
        await createComponent({
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
        expect(fixture.nativeElement.textContent).toContain('USER_MANAGE.DIETOLOGIST_CANCEL_INVITE');
        expect(fixture.nativeElement.textContent).not.toContain('USER_MANAGE.DIETOLOGIST_SAVE_PERMISSIONS');
    });

    it('asks for confirmation before disabling profile sharing', async () => {
        await createComponent(null, false);

        component.onDietologistProfileToggle(false);

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(component.dietologistForm.controls.shareProfile.value).toBe(true);
    });

    it('disables profile sharing after confirmation', async () => {
        await createComponent(null, true);

        component.onDietologistProfileToggle(false);
        fixture.detectChanges();

        expect(component.dietologistForm.controls.shareProfile.value).toBe(false);
    });

    it('autosaves permissions when a relationship toggle changes', async () => {
        await createComponent({
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

    it('asks for confirmation before disconnecting an accepted dietologist relationship', async () => {
        await createComponent(
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
        await createComponent(
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

    it('reloads dietologist relationship when notifications realtime changes', async () => {
        await createComponent(null);
        expect(dietologistService.getRelationship).toHaveBeenCalledTimes(0);

        notificationService.notificationsChangedVersion.set(1);
        fixture.detectChanges();

        expect(dietologistService.getRelationship).toHaveBeenCalledTimes(1);
    });

    it('queues profile autosave when editable user fields change', async () => {
        await createComponent(null);

        component.userForm.controls.firstName.markAsDirty();
        component.userForm.controls.firstName.setValue('Alex');

        expect(facade.queueProfileAutosave).toHaveBeenCalledTimes(1);
        expect(facade.queueProfileAutosave.mock.calls[0][0]).toEqual(expect.objectContaining({ firstName: 'Alex' }));
    });

    it('reports pending and saving profile states for autosave feedback', async () => {
        await createComponent(null);

        expect(component.getProfileStatusKey()).toBe('USER_MANAGE.PROFILE_STATUS_SAVED');

        component.userForm.controls.firstName.markAsDirty();
        component.userForm.controls.firstName.setValue('Alex');
        expect(component.getProfileStatusKey()).toBe('USER_MANAGE.PROFILE_STATUS_PENDING');

        facade.isSavingProfile.set(true);
        fixture.detectChanges();
        expect(component.getProfileStatusKey()).toBe('USER_MANAGE.PROFILE_STATUS_SAVING');
    });

    it('reports the current notifications background action status', async () => {
        await createComponent(null);

        expect(component.getNotificationsStatusKey()).toBeNull();

        facade.isUpdatingNotifications.set(true);
        fixture.detectChanges();
        expect(component.getNotificationsStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_SAVING');

        facade.isUpdatingNotifications.set(false);
        component.isSchedulingTestNotification.set(true);
        fixture.detectChanges();
        expect(component.getNotificationsStatusKey()).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_TEST_SENDING');
    });
});

function createFacadeMock(relationship: any): {
    user: ReturnType<typeof signal<any>>;
    globalError: ReturnType<typeof signal<string | null>>;
    isDeleting: ReturnType<typeof signal<boolean>>;
    isSavingProfile: ReturnType<typeof signal<boolean>>;
    isRevokingAiConsent: ReturnType<typeof signal<boolean>>;
    isUpdatingNotifications: ReturnType<typeof signal<boolean>>;
    webPushSubscriptions: ReturnType<typeof signal<never[]>>;
    dietologistRelationship: ReturnType<typeof signal<any>>;
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
} {
    return {
        user: signal<any>(null),
        globalError: signal<string | null>(null),
        isDeleting: signal(false),
        isSavingProfile: signal(false),
        isRevokingAiConsent: signal(false),
        isUpdatingNotifications: signal(false),
        webPushSubscriptions: signal([]),
        dietologistRelationship: signal<any>(relationship),
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

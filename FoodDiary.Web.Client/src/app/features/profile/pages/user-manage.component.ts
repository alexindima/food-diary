import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    computed,
    DestroyRef,
    effect,
    FactoryProvider,
    inject,
    OnInit,
    signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent, FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { ImageUploadFieldComponent } from '../../../components/shared/image-upload-field/image-upload-field.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { AuthService } from '../../../services/auth.service';
import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import { LocalizationService } from '../../../services/localization.service';
import { NotificationService, WebPushSubscriptionItem } from '../../../services/notification.service';
import { PushNotificationService } from '../../../services/push-notification.service';
import { DietologistService } from '../../dietologist/api/dietologist.service';
import { DietologistPermissions, DietologistRelationship } from '../../dietologist/models/dietologist.data';
import {
    FASTING_REMINDER_PRESETS,
    FastingReminderPreset,
    resolveFastingReminderPresetId,
} from '../../../shared/lib/fasting-reminder-presets';
import { ImageSelection } from '../../../shared/models/image-upload.data';
import { FormGroupControls } from '../../../shared/lib/common.data';
import { ActivityLevelOption, Gender, UpdateUserDto, User } from '../../../shared/models/user.data';
import { ProfileManageFacade } from '../lib/profile-manage.facade';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';
import { APP_THEMES, AppThemeName } from '../../../theme/app-theme.config';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        email: () => 'FORM_ERRORS.EMAIL',
        minlength: (error?: unknown) => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: (error as { requiredLength?: number } | undefined)?.requiredLength },
        }),
    }),
};

@Component({
    selector: 'fd-user-manage',
    imports: [
        ReactiveFormsModule,
        FormsModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiDateInputComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        FdUiSwitchComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ImageUploadFieldComponent,
    ],
    templateUrl: './user-manage.component.html',
    styleUrl: './user-manage.component.scss',
    providers: [VALIDATION_ERRORS_PROVIDER, ProfileManageFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageComponent implements OnInit {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly authService = inject(AuthService);
    private readonly localizationService = inject(LocalizationService);
    private readonly facade = inject(ProfileManageFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly notificationService = inject(NotificationService);
    private readonly pushNotifications = inject(PushNotificationService);
    private readonly dietologistService = inject(DietologistService);
    private readonly toastService = inject(FdUiToastService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private lastUserData: Partial<UserFormValues> | null = null;
    private lastNotificationSyncVersion = -1;
    private readonly notificationPermission = signal<NotificationPermission | 'unsupported'>(this.readNotificationPermission());
    private readonly hasTrackedNotificationsView = signal(false);

    public genders = Object.values(Gender);
    public activityLevels: ActivityLevelOption[] = ['MINIMAL', 'LIGHT', 'MODERATE', 'HIGH', 'EXTREME'];
    public languageCodes: string[] = ['en', 'ru'];
    public genderOptions: FdUiSelectOption<Gender | null>[] = [];
    public activityLevelOptions: FdUiSelectOption<ActivityLevelOption | null>[] = [];
    public languageOptions: FdUiSelectOption<string | null>[] = [];
    public themeOptions: FdUiSelectOption<AppThemeName | null>[] = [];
    public userForm: FormGroup<UserFormData>;
    public dietologistForm: FormGroup<DietologistFormData>;
    public readonly globalError = this.facade.globalError;
    public readonly dietologistRelationship = this.facade.dietologistRelationship;
    public readonly dietologistError = signal<string | null>(null);
    public readonly isLoadingDietologist = signal(false);
    public readonly isSavingDietologist = signal(false);
    public readonly isDeleting = this.facade.isDeleting;
    public readonly isSavingProfile = this.facade.isSavingProfile;
    public readonly isUpdatingNotifications = this.facade.isUpdatingNotifications;
    public readonly isSchedulingTestNotification = signal(false);
    public readonly hasAiConsent = computed(() => !!this.facade.user()?.aiConsentAcceptedAt);
    public readonly pushNotificationsEnabled = computed(() => this.facade.user()?.pushNotificationsEnabled ?? false);
    public readonly fastingPushNotificationsEnabled = computed(() => this.facade.user()?.fastingPushNotificationsEnabled ?? true);
    public readonly socialPushNotificationsEnabled = computed(() => this.facade.user()?.socialPushNotificationsEnabled ?? true);
    public readonly fastingCheckInReminderHours = signal(12);
    public readonly fastingCheckInFollowUpReminderHours = signal(20);
    public readonly fastingReminderPresets = FASTING_REMINDER_PRESETS;
    public readonly pushNotificationsSupported = this.pushNotifications.isSupported;
    public readonly pushNotificationsSubscribed = this.pushNotifications.isSubscribed;
    public readonly pushNotificationsBusy = this.pushNotifications.isBusy;
    public readonly currentSubscriptionEndpoint = this.pushNotifications.currentSubscriptionEndpoint;
    public readonly connectedDevices = this.facade.webPushSubscriptions;
    public readonly isLoadingConnectedDevices = this.facade.isLoadingWebPushSubscriptions;
    public readonly removingConnectedDeviceEndpoint = this.facade.removingWebPushSubscriptionEndpoint;
    public readonly hasDietologistRelationship = computed(() => this.dietologistRelationship() !== null);
    public readonly isDietologistPending = computed(() => this.dietologistRelationship()?.status === 'Pending');
    public readonly isDietologistConnected = computed(() => this.dietologistRelationship()?.status === 'Accepted');
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
        const firstReminder = this.fastingCheckInReminderHours();
        const followUpReminder = this.fastingCheckInFollowUpReminderHours();
        const presetId = resolveFastingReminderPresetId(firstReminder, followUpReminder);
        return presetId === 'custom' ? null : presetId;
    });

    public constructor() {
        this.userForm = new FormGroup<UserFormData>({
            email: new FormControl<string | null>({ value: '', disabled: true }),
            username: new FormControl<string | null>(null),
            firstName: new FormControl<string | null>(null),
            lastName: new FormControl<string | null>(null),
            birthDate: new FormControl<string | null>(null),
            gender: new FormControl<Gender | null>(null),
            language: new FormControl<string | null>(null),
            theme: new FormControl<AppThemeName | null>(null),
            height: new FormControl<number | null>(null),
            activityLevel: new FormControl<ActivityLevelOption | null>(null),
            stepGoal: new FormControl<number | null>(null),
            profileImage: new FormControl<ImageSelection | null>(null),
        });
        this.dietologistForm = new FormGroup<DietologistFormData>({
            email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
            shareProfile: new FormControl<boolean>(true, { nonNullable: true }),
            shareMeals: new FormControl<boolean>(true, { nonNullable: true }),
            shareStatistics: new FormControl<boolean>(true, { nonNullable: true }),
            shareWeight: new FormControl<boolean>(true, { nonNullable: true }),
            shareWaist: new FormControl<boolean>(true, { nonNullable: true }),
            shareGoals: new FormControl<boolean>(true, { nonNullable: true }),
            shareHydration: new FormControl<boolean>(true, { nonNullable: true }),
            shareFasting: new FormControl<boolean>(true, { nonNullable: true }),
        });

        this.buildSelectOptions();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.buildSelectOptions());
        effect(() => {
            const user = this.facade.user();
            if (!user) {
                return;
            }

            const userData = this.mapUserToForm(user);
            this.lastUserData = userData;
            this.applyUserData(userData);
            this.fastingCheckInReminderHours.set(user.fastingCheckInReminderHours ?? 12);
            this.fastingCheckInFollowUpReminderHours.set(user.fastingCheckInFollowUpReminderHours ?? 20);

            if (!this.hasTrackedNotificationsView()) {
                this.frontendObservability.recordNotificationSettingsViewed({
                    pushEnabled: user.pushNotificationsEnabled,
                    fastingEnabled: user.fastingPushNotificationsEnabled,
                    socialEnabled: user.socialPushNotificationsEnabled,
                });
                this.hasTrackedNotificationsView.set(true);
            }
        });

        effect(() => {
            this.syncDietologistFormFromRelationship(this.facade.dietologistRelationship());
        });

        effect(() => {
            const version = this.notificationService.notificationsChangedVersion();
            if (version === this.lastNotificationSyncVersion) {
                return;
            }

            this.lastNotificationSyncVersion = version;
            if (version === 0) {
                return;
            }

            this.loadDietologistRelationship();
        });
    }

    public ngOnInit(): void {
        this.userForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.facade.clearGlobalError();
            this.queueUserAutosave();
        });
        this.dietologistForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.dietologistError.set(null));

        this.facade.initialize();
    }

    public async onSubmit(): Promise<void> {
        this.userForm.markAllAsTouched();

        if (this.userForm.valid) {
            this.facade.saveProfileNow(this.buildUserUpdateDto());
        }
    }

    public resetForm(): void {
        this.facade.clearGlobalError();
        const currentImageId = this.userForm.controls.profileImage.value?.assetId;
        const baselineImageId = this.lastUserData?.profileImage?.assetId ?? null;
        if (currentImageId && currentImageId !== baselineImageId) {
            this.imageUploadService.deleteAsset(currentImageId).subscribe({
                error: () => {
                    // swallow errors to avoid blocking reset
                },
            });
        }

        if (this.lastUserData) {
            this.applyUserData(this.lastUserData);
        } else {
            this.facade.initialize();
        }
    }

    public openChangePasswordDialog(): void {
        this.facade.openChangePasswordDialog();
    }

    public onRevokeAiConsent(): void {
        this.facade.revokeAiConsent();
    }

    public onDeleteAccount(): void {
        this.facade.deleteAccount();
    }

    public async togglePushNotifications(): Promise<void> {
        if (this.isUpdatingNotifications() || this.pushNotificationsBusy()) {
            return;
        }

        const nextEnabled = !this.pushNotificationsEnabled();
        const user = await this.facade.updateNotificationPreferences({ pushNotificationsEnabled: nextEnabled });
        if (!user) {
            return;
        }

        this.notificationPermission.set(this.readNotificationPermission());
        if (!nextEnabled) {
            this.frontendObservability.recordNotificationPreferenceChanged('push', false, {
                permission: this.notificationPermission(),
            });
            this.toastService.info(this.translateService.instant('DASHBOARD.ACTIONS.PUSH_DISABLED'));
            return;
        }

        const result = await this.pushNotifications.ensureSubscription();
        switch (result) {
            case 'subscribed':
            case 'already-subscribed':
                this.frontendObservability.recordNotificationPreferenceChanged('push', true, {
                    permission: this.notificationPermission(),
                });
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'success', { result });
                this.toastService.success(this.translateService.instant('DASHBOARD.ACTIONS.PUSH_ENABLED'));
                this.facade.refreshWebPushSubscriptions();
                break;
            case 'unsupported':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'unsupported', { result });
                this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_UNSUPPORTED_HINT'));
                break;
            case 'blocked':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'blocked', { result });
                this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT'));
                break;
            case 'unavailable':
                this.frontendObservability.recordNotificationSubscriptionEvent('subscription.ensure', 'unavailable', { result });
                this.toastService.info(
                    this.translateService.instant(
                        this.notificationPermission() === 'denied'
                            ? 'USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT'
                            : 'USER_MANAGE.NOTIFICATIONS_UNAVAILABLE_HINT',
                    ),
                );
                break;
        }
    }

    public async toggleFastingPushNotifications(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const user = await this.facade.updateNotificationPreferences({
            fastingPushNotificationsEnabled: !this.fastingPushNotificationsEnabled(),
        });
        if (!user) {
            return;
        }

        this.toastService.info(
            this.translateService.instant(
                user.fastingPushNotificationsEnabled
                    ? 'USER_MANAGE.NOTIFICATIONS_FASTING_ENABLED_TOAST'
                    : 'USER_MANAGE.NOTIFICATIONS_FASTING_DISABLED_TOAST',
            ),
        );
        this.frontendObservability.recordNotificationPreferenceChanged('fasting', user.fastingPushNotificationsEnabled);
    }

    public onFastingReminderHoursChange(value: string | number, field: 'first' | 'followUp'): void {
        const parsed = typeof value === 'number' ? value : parseInt(value, 10);
        if (Number.isNaN(parsed)) {
            return;
        }

        const normalized = Math.max(1, Math.min(168, parsed));
        if (field === 'first') {
            this.fastingCheckInReminderHours.set(normalized);
            return;
        }

        this.fastingCheckInFollowUpReminderHours.set(normalized);
    }

    public applyFastingReminderPreset(preset: FastingReminderPreset): void {
        this.fastingCheckInReminderHours.set(preset.firstReminderHours);
        this.fastingCheckInFollowUpReminderHours.set(preset.followUpReminderHours);
        this.frontendObservability.recordFastingReminderPresetSelected({
            presetId: preset.id,
            firstReminderHours: preset.firstReminderHours,
            followUpReminderHours: preset.followUpReminderHours,
        });
    }

    public async saveFastingReminderHours(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const firstReminder = this.fastingCheckInReminderHours();
        const followUpReminder = this.fastingCheckInFollowUpReminderHours();
        if (followUpReminder <= firstReminder) {
            this.toastService.error(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_ERROR'));
            return;
        }

        const user = await this.facade.updateNotificationPreferences({
            fastingCheckInReminderHours: firstReminder,
            fastingCheckInFollowUpReminderHours: followUpReminder,
        });
        if (!user) {
            return;
        }

        this.frontendObservability.recordFastingReminderTimingSaved({
            firstReminderHours: firstReminder,
            followUpReminderHours: followUpReminder,
            source: this.activeFastingReminderPresetId() ? 'preset' : 'manual',
            presetId: this.activeFastingReminderPresetId() ?? undefined,
        });
        this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_SAVED'));
    }

    public async removeConnectedDevice(subscription: WebPushSubscriptionItem): Promise<void> {
        const endpoint = subscription.endpoint;
        if (!endpoint || this.removingConnectedDeviceEndpoint() || this.pushNotificationsBusy()) {
            return;
        }

        const removed =
            this.currentSubscriptionEndpoint() === endpoint
                ? await this.pushNotifications.removeSubscription(endpoint)
                : await this.facade.removeWebPushSubscription(endpoint);

        if (!removed) {
            this.frontendObservability.recordNotificationSubscriptionEvent('subscription.remove', 'failed', {
                currentDevice: this.isCurrentDevice(subscription),
            });
            this.toastService.error(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVE_ERROR'));
            return;
        }

        this.facade.refreshWebPushSubscriptions();
        this.frontendObservability.recordNotificationSubscriptionEvent('subscription.remove', 'success', {
            currentDevice: this.isCurrentDevice(subscription),
        });
        this.toastService.info(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVED_TOAST'));
    }

    public getConnectedDeviceLabel(subscription: WebPushSubscriptionItem): string {
        const browser = this.getBrowserLabel(subscription.userAgent);
        const platform = this.getPlatformLabel(subscription.userAgent);
        return platform ? `${browser} / ${platform}` : browser;
    }

    public getConnectedDeviceMeta(subscription: WebPushSubscriptionItem): string {
        const segments = [
            subscription.endpointHost,
            subscription.locale?.toUpperCase() ?? null,
            this.formatDateTime(subscription.updatedAtUtc ?? subscription.createdAtUtc),
        ].filter((value): value is string => !!value);

        return segments.join(' | ');
    }

    public isCurrentDevice(subscription: WebPushSubscriptionItem): boolean {
        return !!subscription.endpoint && subscription.endpoint === this.currentSubscriptionEndpoint();
    }

    public async toggleSocialPushNotifications(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const user = await this.facade.updateNotificationPreferences({
            socialPushNotificationsEnabled: !this.socialPushNotificationsEnabled(),
        });
        if (!user) {
            return;
        }

        this.toastService.info(
            this.translateService.instant(
                user.socialPushNotificationsEnabled
                    ? 'USER_MANAGE.NOTIFICATIONS_SOCIAL_ENABLED_TOAST'
                    : 'USER_MANAGE.NOTIFICATIONS_SOCIAL_DISABLED_TOAST',
            ),
        );
        this.frontendObservability.recordNotificationPreferenceChanged('social', user.socialPushNotificationsEnabled);
    }

    public scheduleTestNotification(): void {
        if (this.isSchedulingTestNotification()) {
            return;
        }

        this.isSchedulingTestNotification.set(true);
        this.notificationService
            .scheduleTestNotification({
                delaySeconds: 20,
                type: 'FastingCompleted',
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSchedulingTestNotification.set(false);
                    this.frontendObservability.recordNotificationSubscriptionEvent('test-push.schedule', 'success', {
                        type: 'FastingCompleted',
                        delaySeconds: 20,
                    });
                    this.toastService.info(this.translateService.instant('DASHBOARD.ACTIONS.TEST_PUSH_SCHEDULED'));
                },
                error: () => {
                    this.isSchedulingTestNotification.set(false);
                    this.frontendObservability.recordNotificationSubscriptionEvent('test-push.schedule', 'failed', {
                        type: 'FastingCompleted',
                        delaySeconds: 20,
                    });
                    this.toastService.error(this.translateService.instant('DASHBOARD.ACTIONS.TEST_PUSH_ERROR'));
                },
            });
    }

    public inviteDietologist(): void {
        if (this.isSavingDietologist()) {
            return;
        }

        this.dietologistForm.controls.email.markAsTouched();
        if (this.dietologistForm.invalid) {
            return;
        }

        this.isSavingDietologist.set(true);
        this.dietologistService
            .invite({
                dietologistEmail: this.dietologistForm.controls.email.getRawValue(),
                permissions: this.getDietologistPermissions(),
            })
            .pipe(finalize(() => this.isSavingDietologist.set(false)))
            .subscribe({
                next: () => {
                    this.toastService.success(this.translateService.instant('USER_MANAGE.DIETOLOGIST_INVITE_SUCCESS'));
                    this.loadDietologistRelationship();
                },
                error: () => {
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_INVITE_ERROR');
                },
            });
    }

    public updateDietologistPermission(controlName: DietologistPermissionControlName, nextValue: boolean): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologist()) {
            return;
        }

        const previousPermissions = this.getDietologistPermissions();
        this.dietologistForm.controls[controlName].setValue(nextValue);
        this.persistDietologistPermissions(previousPermissions);
    }

    public persistDietologistPermissions(previousPermissions?: DietologistPermissions): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologist()) {
            return;
        }

        this.isSavingDietologist.set(true);
        this.dietologistService
            .updatePermissions(this.getDietologistPermissions())
            .pipe(finalize(() => this.isSavingDietologist.set(false)))
            .subscribe({
                next: () => {
                    this.dietologistError.set(null);
                    this.loadDietologistRelationship();
                },
                error: () => {
                    if (previousPermissions) {
                        this.dietologistForm.patchValue(previousPermissions, { emitEvent: false });
                        this.dietologistForm.markAsPristine();
                        this.dietologistForm.markAsUntouched();
                        this.cdr.markForCheck();
                    }

                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');
                },
            });
    }

    public revokeDietologistRelationship(): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologist()) {
            return;
        }

        if (this.isDietologistConnected()) {
            this.dialogService
                .open(FdUiConfirmDialogComponent, {
                    size: 'sm',
                    data: {
                        title: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_TITLE'),
                        message: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_MESSAGE'),
                        confirmLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_CONFIRM'),
                        cancelLabel: this.translateService.instant('COMMON.CANCEL'),
                    },
                })
                .afterClosed()
                .subscribe(confirmed => {
                    if (confirmed) {
                        this.executeDietologistRevoke();
                    }

                    this.cdr.markForCheck();
                });
            return;
        }

        this.executeDietologistRevoke();
    }

    public onDietologistProfileToggle(nextValue: boolean): void {
        if (this.isSavingDietologist()) {
            return;
        }

        if (nextValue) {
            this.dietologistForm.controls.shareProfile.setValue(nextValue);
            if (this.hasDietologistRelationship()) {
                this.persistDietologistPermissions({
                    ...this.getDietologistPermissions(),
                    shareProfile: !nextValue,
                });
            }
            this.cdr.markForCheck();
            return;
        }

        this.dialogService
            .open(FdUiConfirmDialogComponent, {
                size: 'sm',
                data: {
                    title: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_TITLE'),
                    message: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_MESSAGE'),
                    confirmLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_CONFIRM'),
                    cancelLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_CANCEL'),
                },
            })
            .afterClosed()
            .subscribe(confirmed => {
                if (confirmed) {
                    const previousPermissions = this.getDietologistPermissions();
                    this.dietologistForm.controls.shareProfile.setValue(false);
                    if (this.hasDietologistRelationship()) {
                        this.persistDietologistPermissions(previousPermissions);
                    }
                }

                this.cdr.markForCheck();
            });
    }

    public getControlError(control: AbstractControl | null): string | null {
        if (!control || !control.invalid) {
            return null;
        }

        const shouldShow = control.touched || control.dirty;
        if (!shouldShow) {
            return null;
        }

        const errors = control.errors;
        if (!errors) {
            return null;
        }

        for (const key of Object.keys(errors)) {
            const resolver = this.validationErrors?.[key];
            if (!resolver) {
                continue;
            }

            const controlParams = typeof errors[key] === 'object' ? errors[key] : {};
            const result = resolver(errors[key]);

            if (typeof result === 'string') {
                return this.translateService.instant(result, controlParams);
            }

            return this.translateService.instant(result.key, {
                ...controlParams,
                ...(result.params ?? {}),
            });
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    public formatMetric(value: number | null | undefined): string {
        if (value === null || value === undefined || Number.isNaN(value)) {
            return '—';
        }

        return Number.isInteger(value) ? `${value}` : value.toFixed(1);
    }

    private applyUserData(userData: Partial<UserFormValues>): void {
        this.userForm.patchValue(userData, { emitEvent: false });
        this.userForm.markAsPristine();
        this.userForm.markAsUntouched();
    }

    private queueUserAutosave(): void {
        if (!this.userForm.dirty || !this.userForm.valid) {
            return;
        }

        this.facade.queueProfileAutosave(this.buildUserUpdateDto());
    }

    private buildUserUpdateDto(): UpdateUserDto {
        const formData = this.userForm.getRawValue();
        return new UpdateUserDto({
            ...formData,
            profileImage: formData.profileImage as ImageSelection | null,
        });
    }

    private loadDietologistRelationship(): void {
        this.isLoadingDietologist.set(true);
        this.dietologistService
            .getRelationship()
            .pipe(finalize(() => this.isLoadingDietologist.set(false)))
            .subscribe({
                next: relationship => {
                    this.facade.dietologistRelationship.set(relationship);
                    this.dietologistError.set(null);
                },
                error: () => {
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_LOAD_ERROR');
                },
            });
    }

    private syncDietologistFormFromRelationship(relationship: DietologistRelationship | null): void {
        if (relationship) {
            this.dietologistForm.patchValue({
                email: relationship.email,
                ...relationship.permissions,
            });
            this.dietologistForm.controls.email.disable({ emitEvent: false });
        } else {
            this.dietologistForm.reset(
                {
                    email: '',
                    shareProfile: true,
                    shareMeals: true,
                    shareStatistics: true,
                    shareWeight: true,
                    shareWaist: true,
                    shareGoals: true,
                    shareHydration: true,
                    shareFasting: true,
                },
                { emitEvent: false },
            );
            this.dietologistForm.controls.email.enable({ emitEvent: false });
        }

        this.dietologistForm.markAsPristine();
        this.dietologistForm.markAsUntouched();
        this.cdr.markForCheck();
    }

    private getDietologistPermissions(): DietologistPermissions {
        return {
            shareProfile: this.dietologistForm.controls.shareProfile.getRawValue(),
            shareMeals: this.dietologistForm.controls.shareMeals.getRawValue(),
            shareStatistics: this.dietologistForm.controls.shareStatistics.getRawValue(),
            shareWeight: this.dietologistForm.controls.shareWeight.getRawValue(),
            shareWaist: this.dietologistForm.controls.shareWaist.getRawValue(),
            shareGoals: this.dietologistForm.controls.shareGoals.getRawValue(),
            shareHydration: this.dietologistForm.controls.shareHydration.getRawValue(),
            shareFasting: this.dietologistForm.controls.shareFasting.getRawValue(),
        };
    }

    private setDietologistError(errorKey: string): void {
        this.dietologistError.set(this.translateService.instant(errorKey));
    }

    private executeDietologistRevoke(): void {
        this.isSavingDietologist.set(true);
        this.dietologistService
            .revokeRelationship()
            .pipe(finalize(() => this.isSavingDietologist.set(false)))
            .subscribe({
                next: () => {
                    this.toastService.info(this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECTED'));
                    this.facade.dietologistRelationship.set(null);
                },
                error: () => {
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_DISCONNECT_ERROR');
                },
            });
    }

    private buildSelectOptions(): void {
        this.genderOptions = this.genders.map(gender => ({
            label: this.translateService.instant(`USER_MANAGE.GENDER_OPTIONS.${gender}`),
            value: gender,
        }));

        this.activityLevelOptions = this.activityLevels.map(level => ({
            label: this.translateService.instant(`USER_MANAGE.ACTIVITY_LEVEL_OPTIONS.${level}`),
            value: level,
        }));

        this.languageOptions = this.languageCodes.map(code => ({
            label: this.translateService.instant(`USER_MANAGE.LANGUAGE_OPTIONS.${code.toUpperCase()}`),
            value: code,
        }));

        this.themeOptions = APP_THEMES.map(theme => ({
            label: this.translateService.instant(`USER_MANAGE.THEME_OPTIONS.${theme.name.toUpperCase()}`),
            value: theme.name,
        }));
    }

    private normalizeLanguage(value: string | null | undefined): string | null {
        if (!value) {
            return null;
        }

        const normalized = value.trim().toLowerCase();
        if (!normalized) {
            return null;
        }

        const [code] = normalized.split(/[-_]/);
        return code || null;
    }

    private mapUserToForm(user: User): Partial<UserFormValues> {
        return {
            email: user.email ?? null,
            username: user.username ?? null,
            firstName: user.firstName ?? null,
            lastName: user.lastName ?? null,
            gender: user.gender as Gender | null,
            language: this.normalizeLanguage(user.language),
            theme: this.normalizeTheme(user.theme),
            birthDate: user.birthDate ? this.formatDateInput(new Date(user.birthDate)) : null,
            height: user.height ?? null,
            activityLevel: user.activityLevel ? (user.activityLevel.toUpperCase() as ActivityLevelOption) : null,
            stepGoal: user.stepGoal ?? null,
            profileImage: user.profileImage ? { url: user.profileImage, assetId: user.profileImageAssetId ?? null } : null,
            pushNotificationsEnabled: user.pushNotificationsEnabled,
            fastingPushNotificationsEnabled: user.fastingPushNotificationsEnabled,
            socialPushNotificationsEnabled: user.socialPushNotificationsEnabled,
        };
    }

    private formatDateInput(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private normalizeTheme(value: string | null | undefined): AppThemeName | null {
        return value === 'ocean' || value === 'leaf' ? value : null;
    }

    private readNotificationPermission(): NotificationPermission | 'unsupported' {
        if (typeof Notification === 'undefined') {
            return 'unsupported';
        }

        return Notification.permission;
    }

    public formatDateTime(value: string | null): string | null {
        if (!value) {
            return null;
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return null;
        }

        return new Intl.DateTimeFormat(this.localizationService.getCurrentLanguage() === 'ru' ? 'ru-RU' : 'en-US', {
            dateStyle: 'medium',
            timeStyle: 'short',
        }).format(date);
    }

    public formatLocalizedDate(value: string | null | undefined): string | null {
        if (!value) {
            return null;
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return null;
        }

        return new Intl.DateTimeFormat(this.localizationService.getCurrentLanguage() === 'ru' ? 'ru-RU' : 'en-US', {
            dateStyle: 'medium',
        }).format(date);
    }

    private getBrowserLabel(userAgent: string | null): string {
        const normalized = userAgent?.toLowerCase() ?? '';
        if (normalized.includes('edg/')) {
            return 'Edge';
        }

        if (normalized.includes('opr/') || normalized.includes('opera')) {
            return 'Opera';
        }

        if (normalized.includes('chrome/') && !normalized.includes('edg/') && !normalized.includes('opr/')) {
            return 'Chrome';
        }

        if (normalized.includes('firefox/')) {
            return 'Firefox';
        }

        if (normalized.includes('safari/') && !normalized.includes('chrome/')) {
            return 'Safari';
        }

        return this.translateService.instant('USER_MANAGE.NOTIFICATIONS_DEVICE_GENERIC');
    }

    private getPlatformLabel(userAgent: string | null): string | null {
        const normalized = userAgent?.toLowerCase() ?? '';
        if (normalized.includes('iphone') || normalized.includes('ipad') || normalized.includes('ios')) {
            return 'iOS';
        }

        if (normalized.includes('android')) {
            return 'Android';
        }

        if (normalized.includes('windows')) {
            return 'Windows';
        }

        if (normalized.includes('mac os') || normalized.includes('macintosh')) {
            return 'macOS';
        }

        if (normalized.includes('linux')) {
            return 'Linux';
        }

        return null;
    }
}

export interface UserFormValues {
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    email: string | null;
    birthDate: string | null;
    gender: Gender | null;
    language: string | null;
    theme: AppThemeName | null;
    height: number | null;
    activityLevel: ActivityLevelOption | null;
    stepGoal: number | null;
    profileImage: ImageSelection | null;
    pushNotificationsEnabled?: boolean | null;
    fastingPushNotificationsEnabled?: boolean | null;
    socialPushNotificationsEnabled?: boolean | null;
}

export type UserFormData = FormGroupControls<UserFormValues>;

interface DietologistFormValues {
    email: string;
    shareProfile: boolean;
    shareMeals: boolean;
    shareStatistics: boolean;
    shareWeight: boolean;
    shareWaist: boolean;
    shareGoals: boolean;
    shareHydration: boolean;
    shareFasting: boolean;
}

type DietologistFormData = FormGroupControls<DietologistFormValues>;
type DietologistPermissionControlName = Exclude<keyof DietologistFormValues, 'email'>;

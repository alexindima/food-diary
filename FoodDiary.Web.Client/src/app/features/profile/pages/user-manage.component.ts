import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    computed,
    DestroyRef,
    effect,
    type FactoryProvider,
    inject,
    PLATFORM_ID,
    signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { type AbstractControl, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_VALIDATION_ERRORS, FdUiFormErrorComponent, type FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSectionStateComponent } from 'fd-ui-kit/section-state/fd-ui-section-state.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiStatusBadgeComponent } from 'fd-ui-kit/status-badge/fd-ui-status-badge.component';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { EMPTY, finalize, merge, type Observable } from 'rxjs';

import { ImageUploadFieldComponent } from '../../../components/shared/image-upload-field/image-upload-field.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { AuthService } from '../../../services/auth.service';
import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { LocalizationService } from '../../../services/localization.service';
import { NotificationService, type WebPushSubscriptionItem } from '../../../services/notification.service';
import { PushNotificationService } from '../../../services/push-notification.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import { type FormGroupControls } from '../../../shared/lib/common.data';
import {
    FASTING_REMINDER_PRESETS,
    type FastingReminderPreset,
    resolveFastingReminderPresetId,
} from '../../../shared/lib/fasting-reminder-presets';
import { parseIntegerInput } from '../../../shared/lib/number.utils';
import { type ImageSelection } from '../../../shared/models/image-upload.data';
import { type ActivityLevelOption, Gender, type UiStyleOption, UpdateUserDto, type User } from '../../../shared/models/user.data';
import {
    APP_THEMES,
    APP_UI_STYLES,
    type AppThemeName,
    type AppUiStyleName,
    isAppThemeName,
    isAppUiStyleName,
} from '../../../theme/app-theme.config';
import { DietologistService } from '../../dietologist/api/dietologist.service';
import { type DietologistPermissions, type DietologistRelationship } from '../../dietologist/models/dietologist.data';
import { PremiumBillingService } from '../../premium/api/premium-billing.service';
import { type BillingOverview, type BillingPlan, type BillingProvider } from '../../premium/models/billing.models';
import { ProfileManageFacade } from '../lib/profile-manage.facade';

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
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiDateInputComponent,
        FdUiButtonComponent,
        FdUiSectionStateComponent,
        FdUiFormErrorComponent,
        FdUiStatusBadgeComponent,
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
export class UserManageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly authService = inject(AuthService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly localizationService = inject(LocalizationService);
    private readonly facade = inject(ProfileManageFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly notificationService = inject(NotificationService);
    private readonly pushNotifications = inject(PushNotificationService);
    private readonly dietologistService = inject(DietologistService);
    private readonly billingService = inject(PremiumBillingService);
    private readonly toastService = inject(FdUiToastService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private lastUserData: Partial<UserFormValues> | null = null;
    private lastNotificationSyncVersion = -1;
    private readonly notificationPermission = signal<NotificationPermission | 'unsupported'>(this.readNotificationPermission());
    private readonly hasTrackedNotificationsView = signal(false);
    private readonly pendingPasswordSetupIntent = signal(false);
    private readonly languageVersion = signal(0);

    public genders = Object.values(Gender);
    public activityLevels: ActivityLevelOption[] = ['MINIMAL', 'LIGHT', 'MODERATE', 'HIGH', 'EXTREME'];
    public languageCodes: string[] = ['en', 'ru'];
    public genderOptions: FdUiSelectOption<Gender | null>[] = [];
    public activityLevelOptions: FdUiSelectOption<ActivityLevelOption | null>[] = [];
    public languageOptions: FdUiSelectOption<string | null>[] = [];
    public themeOptions: FdUiSelectOption<AppThemeName | null>[] = [];
    public uiStyleOptions: FdUiSelectOption<AppUiStyleName | null>[] = [];
    public userForm: FormGroup<UserFormData>;
    public dietologistForm: FormGroup<DietologistFormData>;
    public readonly globalError = this.facade.globalError;
    public readonly dietologistRelationship = this.facade.dietologistRelationship;
    public readonly dietologistError = signal<string | null>(null);
    public readonly dietologistPermissions = signal<DietologistPermissions>(this.createDefaultDietologistPermissions());
    public readonly isLoadingDietologist = signal(false);
    public readonly isSavingDietologist = signal(false);
    public readonly billingOverview = signal<BillingOverview | null>(null);
    public readonly isLoadingBilling = signal(false);
    public readonly isOpeningBillingPortal = signal(false);
    public readonly billingError = signal<string | null>(null);
    public readonly isDeleting = this.facade.isDeleting;
    public readonly isSavingProfile = this.facade.isSavingProfile;
    public readonly isRevokingAiConsent = this.facade.isRevokingAiConsent;
    public readonly isUpdatingNotifications = this.facade.isUpdatingNotifications;
    public readonly isSchedulingTestNotification = signal(false);
    public readonly isSurfaceBusy = computed(
        () =>
            this.isSavingProfile() ||
            this.isDeleting() ||
            this.isRevokingAiConsent() ||
            this.isUpdatingNotifications() ||
            this.isSchedulingTestNotification() ||
            this.isSavingDietologist() ||
            this.isLoadingDietologist() ||
            this.isLoadingBilling() ||
            this.isOpeningBillingPortal() ||
            this.isLoadingConnectedDevices() ||
            !!this.removingConnectedDeviceEndpoint(),
    );
    public readonly hasAiConsent = computed(() => !!this.facade.user()?.aiConsentAcceptedAt);
    public readonly hasPassword = computed(() => this.facade.user()?.hasPassword ?? true);
    public readonly passwordActionState = computed<PasswordActionState>(() => {
        const hasPassword = this.hasPassword();

        return {
            buttonLabelKey: hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD' : 'USER_MANAGE.SET_PASSWORD',
            descriptionKey: hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_DESCRIPTION' : 'USER_MANAGE.SET_PASSWORD_DESCRIPTION',
        };
    });
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
    public readonly profileStatus = signal<ProfileStatusViewModel>({
        key: 'USER_MANAGE.PROFILE_STATUS_SAVED',
        tone: 'success',
    });
    public readonly connectedDeviceItems = computed<ConnectedDeviceViewModel[]>(() => {
        this.languageVersion();
        return this.connectedDevices().map(subscription => ({
            subscription,
            label: this.buildConnectedDeviceLabel(subscription),
            meta: this.buildConnectedDeviceMeta(subscription),
            isCurrent: this.isCurrentDevice(subscription),
        }));
    });
    public readonly dietologistInviteEmailError = signal<string | null>(null);
    public readonly dietologistAcceptedDateLabel = computed(() => {
        this.languageVersion();
        return this.formatLocalizedDate(this.dietologistRelationship()?.acceptedAtUtc);
    });
    public readonly dietologistExpiresDateLabel = computed(() => {
        this.languageVersion();
        return this.formatLocalizedDate(this.dietologistRelationship()?.expiresAtUtc);
    });
    public readonly billingStatusLabelKey = computed(() =>
        this.getBillingStatusLabelKey(this.billingOverview()?.subscriptionStatus ?? null),
    );
    public readonly billingPlanLabelKey = computed(() => this.getBillingPlanLabelKey(this.billingOverview()?.plan ?? null));
    public readonly billingProviderLabel = computed(() => {
        const overview = this.billingOverview();
        return this.getBillingProviderLabel(overview?.subscriptionProvider ?? overview?.provider ?? null);
    });
    public readonly billingCurrentPeriodStartLabel = computed(() => {
        this.languageVersion();
        return this.formatLocalizedDate(this.billingOverview()?.currentPeriodStartUtc);
    });
    public readonly billingCurrentPeriodEndLabel = computed(() => {
        this.languageVersion();
        return this.formatLocalizedDate(this.billingOverview()?.currentPeriodEndUtc);
    });
    public readonly billingNextAttemptLabel = computed(() => {
        this.languageVersion();
        return this.formatLocalizedDate(this.billingOverview()?.nextBillingAttemptUtc);
    });
    public readonly billingRenewalLabelKey = computed(() => {
        const overview = this.billingOverview();
        if (!overview || !overview.isPremium) {
            return 'USER_MANAGE.BILLING_RENEWAL_FREE';
        }

        if (overview.cancelAtPeriodEnd) {
            return 'USER_MANAGE.BILLING_RENEWAL_CANCELING';
        }

        if (overview.renewalEnabled) {
            return 'USER_MANAGE.BILLING_RENEWAL_ENABLED';
        }

        return 'USER_MANAGE.BILLING_RENEWAL_MANUAL';
    });
    public readonly billingView = computed<BillingViewModel | null>(() => {
        const overview = this.billingOverview();
        if (!overview) {
            return null;
        }

        return {
            overview,
            statusTone: overview.isPremium ? 'success' : 'muted',
            endLabelKey: overview.cancelAtPeriodEnd ? 'USER_MANAGE.BILLING_ACCESS_ENDS' : 'USER_MANAGE.BILLING_PERIOD_END',
            showNextAttempt: !overview.cancelAtPeriodEnd,
            premiumActionVariant: overview.isPremium ? 'secondary' : 'primary',
            premiumActionLabelKey: overview.isPremium ? 'USER_MANAGE.BILLING_VIEW_PREMIUM' : 'USER_MANAGE.BILLING_UPGRADE',
            showManagedSupportNote: overview.isPremium && !overview.manageBillingAvailable,
        };
    });
    public readonly notificationsStatusKey = computed(() => this.buildNotificationsStatusKey());
    public readonly connectedDevicesSectionState = computed<'loading' | 'content' | 'empty'>(() => {
        if (this.isLoadingConnectedDevices()) {
            return 'loading';
        }

        return this.connectedDevices().length === 0 ? 'empty' : 'content';
    });
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
            uiStyle: new FormControl<AppUiStyleName | null>(null),
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
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildSelectOptions();
            this.languageVersion.update(version => version + 1);
            this.updateDietologistInviteEmailError();
        });
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
            this.pendingPasswordSetupIntent.set(params.get('intent') === 'set-password');
        });
        effect(() => {
            const user = this.facade.user();
            if (!user) {
                return;
            }

            const userData = this.mapUserToForm(user);
            this.lastUserData = userData;
            this.applyUserData(userData);
            this.fastingCheckInReminderHours.set(user.fastingCheckInReminderHours);
            this.fastingCheckInFollowUpReminderHours.set(user.fastingCheckInFollowUpReminderHours);

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
            const user = this.facade.user();
            if (!user || !this.pendingPasswordSetupIntent()) {
                return;
            }

            void this.clearProfileIntentQueryParamAsync();
            this.pendingPasswordSetupIntent.set(false);

            if (!user.hasPassword) {
                this.facade.openChangePasswordDialog();
            }
        });

        effect(() => {
            this.syncDietologistFormFromRelationship(this.facade.dietologistRelationship());
        });
        effect(() => {
            this.globalError();
            this.isSavingProfile();
            this.updateProfileStatus();
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
        this.userForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.facade.clearGlobalError();
            this.queueUserAutosave();
            this.updateProfileStatus();
        });
        this.userForm.statusChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updateProfileStatus();
        });
        this.dietologistForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.dietologistError.set(null);
            this.updateDietologistPermissionsState();
        });
        const dietologistFormEvents = (this.dietologistForm as { events?: Observable<unknown> }).events ?? EMPTY;
        merge(dietologistFormEvents, this.dietologistForm.statusChanges, this.dietologistForm.valueChanges)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.updateDietologistInviteEmailError();
            });
        this.updateDietologistInviteEmailError();

        this.facade.initialize();
        this.loadBillingOverview();
    }

    public onSubmit(): void {
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

    public async togglePushNotificationsAsync(): Promise<void> {
        if (this.isUpdatingNotifications() || this.pushNotificationsBusy()) {
            return;
        }

        const nextEnabled = !this.pushNotificationsEnabled();
        const user = await this.facade.updateNotificationPreferencesAsync({ pushNotificationsEnabled: nextEnabled });
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

        const result = await this.pushNotifications.ensureSubscriptionAsync();
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

    public async toggleFastingPushNotificationsAsync(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const user = await this.facade.updateNotificationPreferencesAsync({
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
        const parsed = parseIntegerInput(value);
        if (parsed === null) {
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

    public async saveFastingReminderHoursAsync(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const firstReminder = this.fastingCheckInReminderHours();
        const followUpReminder = this.fastingCheckInFollowUpReminderHours();
        if (followUpReminder <= firstReminder) {
            this.toastService.error(this.translateService.instant('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_ERROR'));
            return;
        }

        const user = await this.facade.updateNotificationPreferencesAsync({
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

    public async removeConnectedDeviceAsync(subscription: WebPushSubscriptionItem): Promise<void> {
        const endpoint = subscription.endpoint;
        if (!endpoint || this.removingConnectedDeviceEndpoint() || this.pushNotificationsBusy()) {
            return;
        }

        const removed =
            this.currentSubscriptionEndpoint() === endpoint
                ? await this.pushNotifications.removeSubscriptionAsync(endpoint)
                : await this.facade.removeWebPushSubscriptionAsync(endpoint);

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

    private buildConnectedDeviceLabel(subscription: WebPushSubscriptionItem): string {
        const browser = this.getBrowserLabel(subscription.userAgent);
        const platform = this.getPlatformLabel(subscription.userAgent);
        return platform ? `${browser} / ${platform}` : browser;
    }

    private buildConnectedDeviceMeta(subscription: WebPushSubscriptionItem): string {
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

    public async toggleSocialPushNotificationsAsync(): Promise<void> {
        if (this.isUpdatingNotifications()) {
            return;
        }

        const user = await this.facade.updateNotificationPreferencesAsync({
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
        this.updateDietologistInviteEmailError();
        if (this.dietologistForm.invalid) {
            return;
        }

        this.isSavingDietologist.set(true);
        this.dietologistService
            .invite({
                dietologistEmail: this.dietologistForm.controls.email.getRawValue(),
                permissions: this.getDietologistPermissions(),
            })
            .pipe(
                finalize(() => {
                    this.isSavingDietologist.set(false);
                }),
            )
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
            .pipe(
                finalize(() => {
                    this.isSavingDietologist.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.dietologistError.set(null);
                    this.loadDietologistRelationship();
                },
                error: () => {
                    if (previousPermissions) {
                        this.dietologistForm.patchValue(previousPermissions, { emitEvent: false });
                        this.updateDietologistPermissionsState();
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
                    preset: 'confirm',
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
                preset: 'confirm',
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

    private updateDietologistInviteEmailError(): void {
        this.dietologistInviteEmailError.set(this.resolveControlError(this.dietologistForm.controls.email));
    }

    private resolveControlError(control: AbstractControl | null): string | null {
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

    public getNotificationsStatusKey(): string | null {
        return this.notificationsStatusKey();
    }

    private updateProfileStatus(): void {
        this.profileStatus.set(this.buildProfileStatus());
    }

    private buildProfileStatus(): ProfileStatusViewModel {
        if (this.globalError() && this.userForm.dirty) {
            return { key: 'USER_MANAGE.PROFILE_STATUS_ERROR', tone: 'danger' };
        }

        if (this.isSavingProfile()) {
            return { key: 'USER_MANAGE.PROFILE_STATUS_SAVING', tone: 'muted' };
        }

        if (this.userForm.dirty) {
            return {
                key: this.userForm.valid ? 'USER_MANAGE.PROFILE_STATUS_PENDING' : 'USER_MANAGE.PROFILE_STATUS_INVALID',
                tone: 'warning',
            };
        }

        return { key: 'USER_MANAGE.PROFILE_STATUS_SAVED', tone: 'success' };
    }

    private buildNotificationsStatusKey(): string | null {
        if (this.isSchedulingTestNotification()) {
            return 'USER_MANAGE.NOTIFICATIONS_STATUS_TEST_SENDING';
        }

        if (this.removingConnectedDeviceEndpoint()) {
            return 'USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_REMOVING';
        }

        if (this.pushNotificationsBusy()) {
            return 'USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_SYNCING';
        }

        if (this.isUpdatingNotifications()) {
            return 'USER_MANAGE.NOTIFICATIONS_STATUS_SAVING';
        }

        return null;
    }

    public reloadBillingOverview(): void {
        this.loadBillingOverview();
    }

    public openPremiumPage(): void {
        void this.router.navigate(['/premium']);
    }

    public openBillingPortal(): void {
        if (!this.isBrowser || this.isOpeningBillingPortal()) {
            return;
        }

        this.billingError.set(null);
        this.isOpeningBillingPortal.set(true);
        this.billingService
            .createPortalSession()
            .pipe(
                finalize(() => {
                    this.isOpeningBillingPortal.set(false);
                }),
            )
            .subscribe({
                next: session => {
                    if (!session.url) {
                        this.billingError.set('USER_MANAGE.BILLING_PORTAL_ERROR');
                        this.toastService.error(this.translateService.instant('USER_MANAGE.BILLING_PORTAL_ERROR'));
                        return;
                    }

                    this.document.location.href = session.url;
                },
                error: () => {
                    this.billingError.set('USER_MANAGE.BILLING_PORTAL_ERROR');
                    this.toastService.error(this.translateService.instant('USER_MANAGE.BILLING_PORTAL_ERROR'));
                },
            });
    }

    private applyUserData(userData: Partial<UserFormValues>): void {
        this.userForm.patchValue(userData, { emitEvent: false });
        this.userForm.markAsPristine();
        this.userForm.markAsUntouched();
        this.updateProfileStatus();
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
            .pipe(
                finalize(() => {
                    this.isLoadingDietologist.set(false);
                }),
            )
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

    private loadBillingOverview(): void {
        this.isLoadingBilling.set(true);
        this.billingError.set(null);
        this.billingService
            .getOverview()
            .pipe(
                finalize(() => {
                    this.isLoadingBilling.set(false);
                }),
            )
            .subscribe({
                next: overview => {
                    this.billingOverview.set(overview);
                },
                error: () => {
                    this.billingError.set('USER_MANAGE.BILLING_LOAD_ERROR');
                },
            });
    }

    private getBillingPlanLabelKey(plan: BillingPlan | null): string {
        if (!this.billingOverview()?.isPremium) {
            return 'USER_MANAGE.BILLING_PLAN_FREE';
        }

        return plan === 'yearly' ? 'USER_MANAGE.BILLING_PLAN_PREMIUM_YEARLY' : 'USER_MANAGE.BILLING_PLAN_PREMIUM_MONTHLY';
    }

    private getBillingStatusLabelKey(status: string | null): string {
        switch (status) {
            case 'active':
                return 'USER_MANAGE.BILLING_STATUS_ACTIVE';
            case 'trialing':
                return 'USER_MANAGE.BILLING_STATUS_TRIALING';
            case 'past_due':
                return 'USER_MANAGE.BILLING_STATUS_PAST_DUE';
            case 'canceled':
                return 'USER_MANAGE.BILLING_STATUS_CANCELED';
            case 'unpaid':
                return 'USER_MANAGE.BILLING_STATUS_UNPAID';
            case 'incomplete':
                return 'USER_MANAGE.BILLING_STATUS_INCOMPLETE';
            default:
                return 'USER_MANAGE.BILLING_STATUS_FREE';
        }
    }

    private getBillingProviderLabel(provider: BillingProvider | null): string {
        if (!provider?.trim()) {
            return this.translateService.instant('USER_MANAGE.BILLING_PROVIDER_NONE');
        }

        switch (provider.toLowerCase()) {
            case 'yookassa':
                return 'YooKassa';
            case 'paddle':
                return 'Paddle';
            case 'stripe':
                return 'Stripe';
            default:
                return provider;
        }
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

        this.updateDietologistPermissionsState();
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

    private updateDietologistPermissionsState(): void {
        this.dietologistPermissions.set(this.getDietologistPermissions());
    }

    private createDefaultDietologistPermissions(): DietologistPermissions {
        return {
            shareProfile: true,
            shareMeals: true,
            shareStatistics: true,
            shareWeight: true,
            shareWaist: true,
            shareGoals: true,
            shareHydration: true,
            shareFasting: true,
        };
    }

    private setDietologistError(errorKey: string): void {
        this.dietologistError.set(this.translateService.instant(errorKey));
    }

    private executeDietologistRevoke(): void {
        this.isSavingDietologist.set(true);
        this.dietologistService
            .revokeRelationship()
            .pipe(
                finalize(() => {
                    this.isSavingDietologist.set(false);
                }),
            )
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

        this.uiStyleOptions = APP_UI_STYLES.map(style => ({
            label: this.translateService.instant(`USER_MANAGE.UI_STYLE_OPTIONS.${style.name.toUpperCase()}`),
            value: style.name,
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
            email: user.email,
            username: user.username,
            firstName: user.firstName ?? null,
            lastName: user.lastName ?? null,
            gender: this.normalizeGender(user.gender),
            language: this.normalizeLanguage(user.language),
            theme: this.normalizeTheme(user.theme),
            uiStyle: this.normalizeUiStyle(user.uiStyle),
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

    private async clearProfileIntentQueryParamAsync(): Promise<void> {
        await this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { intent: null },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    }

    private normalizeGender(value: string | null | undefined): Gender | null {
        if (!value) {
            return null;
        }

        const normalized = value.trim().toLowerCase();
        const genderMap: Record<string, Gender> = {
            m: Gender.Male,
            male: Gender.Male,
            f: Gender.Female,
            female: Gender.Female,
            o: Gender.Other,
            other: Gender.Other,
        };

        return genderMap[normalized] ?? null;
    }

    private normalizeTheme(value: string | null | undefined): AppThemeName | null {
        if (!value) {
            return null;
        }

        const normalized = value.trim().toLowerCase();
        const legacyThemeMap: Record<string, AppThemeName> = {
            default: 'ocean',
        };
        const resolved = legacyThemeMap[normalized] ?? normalized;

        return isAppThemeName(resolved) ? resolved : null;
    }

    private normalizeUiStyle(value: string | null | undefined): AppUiStyleName | null {
        if (!value) {
            return null;
        }

        const normalized = value.trim().toLowerCase();
        const legacyUiStyleMap: Record<string, AppUiStyleName> = {
            default: 'classic',
        };
        const resolved = legacyUiStyleMap[normalized] ?? normalized;

        return isAppUiStyleName(resolved) ? resolved : null;
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

    private formatLocalizedDate(value: string | null | undefined): string | null {
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
    uiStyle: UiStyleOption | null;
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

interface ProfileStatusViewModel {
    key: string;
    tone: 'success' | 'warning' | 'danger' | 'muted';
}

interface ConnectedDeviceViewModel {
    subscription: WebPushSubscriptionItem;
    label: string;
    meta: string;
    isCurrent: boolean;
}

interface PasswordActionState {
    buttonLabelKey: string;
    descriptionKey: string;
}

interface BillingViewModel {
    overview: BillingOverview;
    statusTone: 'success' | 'muted';
    endLabelKey: string;
    showNextAttempt: boolean;
    premiumActionVariant: 'secondary' | 'primary';
    premiumActionLabelKey: string;
    showManagedSupportNote: boolean;
}

import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type ElementRef,
    inject,
    PLATFORM_ID,
    Renderer2,
    signal,
    untracked,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormRoot, validate } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, EMPTY } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { UnsavedChangesBarComponent } from '../../../../components/shared/unsaved-changes-bar/unsaved-changes-bar';
import { type UnsavedChangesHandler, UnsavedChangesService } from '../../../../services/unsaved-changes.service';
import { ImageUploadFacade } from '../../../../shared/lib/image-upload.facade';
import type { DietologistPermissions } from '../../../../shared/models/dietologist.data';
import type { ActivityLevelOption, Gender } from '../../../../shared/models/user.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import type { AppThemeName, AppUiStyleName } from '../../../../theme/app-theme.config';
import { ProfileBillingFacade } from '../../lib/profile-billing.facade';
import { ProfileDietologistFacade } from '../../lib/profile-dietologist.facade';
import { ProfileManageFacade } from '../../lib/profile-manage.facade';
import type {
    DietologistPermissionChange,
    DietologistPermissionControlName,
    PasswordActionState,
    UserFormValues,
    UserManageAccountFormPatch,
    UserManageBodyFormPatch,
} from '../../lib/user-manage.types';
import {
    buildUserManageSelectOptions,
    buildUserUpdateDto,
    createUserManageFormModel,
    mapUserToForm,
    normalizeOptionalTextInput,
    parseOptionalNumberInput,
} from '../../lib/user-manage-form.mapper';
import { UserManageBillingCardComponent } from '../user-manage-sections/billing-card/user-manage-billing-card';
import { UserManageComparisonWidgetsComponent } from '../user-manage-sections/comparison-widgets/user-manage-comparison-widgets';
import { UserManageDietologistCardComponent } from '../user-manage-sections/dietologist-card/user-manage-dietologist-card';
import { UserManageNotificationsCardComponent } from '../user-manage-sections/notifications-card/user-manage-notifications-card';
import { UserManagePrivacyCardComponent } from '../user-manage-sections/privacy-card/user-manage-privacy-card';
import { UserManageBackupEmailComponent } from '../user-manage-sections/security-card/user-manage-backup-email';
import { UserManageSecurityCardComponent } from '../user-manage-sections/security-card/user-manage-security-card';
import { UserManageNotificationsFacade } from './user-manage-lib/user-manage-notifications.facade';
import { USER_MANAGE_TOUR } from './user-manage-tour';

type UserManageFormPatch = UserManageAccountFormPatch | UserManageBodyFormPatch;
const MAX_PROFILE_HEIGHT_CM = 300;

@Component({
    selector: 'fd-user-manage',
    imports: [
        TranslatePipe,
        FormRoot,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        PageHeaderComponent,
        PageBodyComponent,
        UnsavedChangesBarComponent,
        FdPageContainerDirective,
        UserManageBillingCardComponent,
        UserManageDietologistCardComponent,
        UserManageNotificationsCardComponent,
        UserManagePrivacyCardComponent,
        UserManageSecurityCardComponent,
        UserManageBackupEmailComponent,
        UserManageComparisonWidgetsComponent,
    ],
    templateUrl: './user-manage.html',
    styleUrl: './user-manage.scss',
    providers: [ProfileManageFacade, UserManageNotificationsFacade, ProfileDietologistFacade, ProfileBillingFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(window:beforeunload)': 'onBeforeUnload($event)' },
})
export class UserManageComponent {
    private readonly dietologist = inject(ProfileDietologistFacade);
    private readonly billing = inject(ProfileBillingFacade);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly facade = inject(ProfileManageFacade);
    private readonly imageUploadFacade = inject(ImageUploadFacade);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    protected readonly notifications = inject(UserManageNotificationsFacade);

    private readonly toastService = inject(FdUiToastService);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly renderer = inject<Renderer2>(Renderer2);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private readonly userFormElement = viewChild<ElementRef<HTMLFormElement>>('userFormElement');
    private lastNotificationSyncVersion = -1;
    private lastProfileSavedVersion = 0;
    private userFormDomListenersRegistered = false;
    private readonly pendingPasswordSetupIntent = signal(false);

    protected genderOptions: Array<FdUiSelectOption<Gender | null>> = [];
    protected activityLevelOptions: Array<FdUiSelectOption<ActivityLevelOption | null>> = [];
    protected languageOptions: Array<FdUiSelectOption<string | null>> = [];
    protected themeOptions: Array<FdUiSelectOption<AppThemeName | null>> = [];
    protected uiStyleOptions: Array<FdUiSelectOption<AppUiStyleName | null>> = [];
    protected readonly userFormModel = signal<UserFormValues>(createUserManageFormModel());
    protected readonly birthDateInputInvalid = signal(false);
    protected readonly profileValidationErrorKey = computed(() =>
        this.birthDateInputInvalid() ? 'USER_MANAGE.BIRTH_DATE_INVALID' : 'USER_MANAGE.HEIGHT_INVALID',
    );
    private readonly lastSyncedUserFormData = signal<UserFormValues>(createUserManageFormModel());
    private readonly userFormInputVersion = signal(0);
    protected readonly userForm = form(this.userFormModel, path => {
        validate(path.birthDate, () => (this.birthDateInputInvalid() ? { kind: 'birthDateInput' } : undefined));
        validate(path.heightCm, ({ value }) => {
            const height = value();
            return height !== null && (!Number.isFinite(height) || height <= 0 || height > MAX_PROFILE_HEIGHT_CM)
                ? { kind: 'heightRange' }
                : undefined;
        });
    });
    protected readonly dietologistFormModel = this.dietologist.dietologistFormModel;
    protected readonly dietologistForm = this.dietologist.dietologistForm;
    protected readonly globalError = this.facade.globalError;
    protected readonly dietologistRelationship = this.dietologist.dietologistRelationship;
    protected readonly dietologistError = this.dietologist.dietologistError;
    protected readonly dietologistPermissions = this.dietologist.dietologistPermissions;
    protected readonly isLoadingDietologist = this.dietologist.isLoadingDietologist;
    protected readonly isSavingDietologistPermissions = this.dietologist.isSavingDietologistPermissions;
    protected readonly isSavingDietologistRelationshipAction = this.dietologist.isSavingDietologistRelationshipAction;
    protected readonly isSavingDietologist = this.dietologist.isSavingDietologist;
    protected readonly billingOverview = this.billing.billingOverview;
    protected readonly isLoadingBilling = this.billing.isLoadingBilling;
    protected readonly isOpeningBillingPortal = this.billing.isOpeningBillingPortal;
    protected readonly billingError = this.billing.billingError;
    protected readonly isDeleting = this.facade.isDeleting;
    protected readonly isSavingProfile = this.facade.isSavingProfile;
    protected readonly isRevokingAiConsent = this.facade.isRevokingAiConsent;
    protected readonly isSurfaceBusy = computed(() => this.surfaceBusySignals().includes(true));
    protected readonly hasAiConsent = computed(() => {
        const acceptedAt = this.facade.user()?.aiConsentAcceptedAt;
        return acceptedAt !== null && acceptedAt !== undefined && acceptedAt.length > 0;
    });
    protected readonly hasPassword = computed(() => this.facade.user()?.hasPassword ?? true);
    protected readonly hasGoogleIdentity = computed(() => this.facade.user()?.hasGoogleIdentity ?? false);
    protected readonly hasTelegramIdentity = computed(() => this.facade.user()?.hasTelegramIdentity ?? false);
    protected readonly canUnlinkTelegram = computed(() => {
        const user = this.facade.user();
        return user !== null && (user.hasGoogleIdentity === true || (user.hasPassword && user.isEmailConfirmed && user.email !== null));
    });
    protected readonly isUnlinkingTelegram = this.facade.isUnlinkingTelegram;

    protected unlinkTelegram(): void {
        void this.facade.unlinkTelegramAsync();
    }
    protected readonly accountEmail = computed(() => this.facade.user()?.email ?? '');
    protected readonly canUsePassword = computed(
        () => this.hasPassword() || (Boolean(this.facade.user()?.email) && this.facade.user()?.isEmailConfirmed === true),
    );
    protected readonly isLinkingGoogle = this.facade.isLinkingGoogle;
    protected readonly passwordActionState = computed<PasswordActionState>(() => {
        const hasPassword = this.hasPassword();

        return {
            buttonLabelKey: hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD' : 'USER_MANAGE.SET_PASSWORD',
            descriptionKey: hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_DESCRIPTION' : 'USER_MANAGE.SET_PASSWORD_DESCRIPTION',
        };
    });
    protected readonly hasDietologistRelationship = this.dietologist.hasDietologistRelationship;
    protected readonly isDietologistPending = this.dietologist.isDietologistPending;
    protected readonly isDietologistConnected = this.dietologist.isDietologistConnected;
    protected readonly currentWeight = this.facade.currentWeight;
    protected readonly currentWaist = this.facade.currentWaist;
    protected readonly dietologistInviteEmailError = this.dietologist.dietologistInviteEmailError;
    protected readonly billingView = this.billing.billingView;
    protected readonly hasUnsavedProfileChanges = computed(() => {
        this.userFormInputVersion();
        return this.birthDateInputInvalid() || this.hasUserFormChanges();
    });

    public constructor() {
        this.buildSelectOptions();
        this.watchUserFormElement();
        this.watchLanguageChanges();
        this.watchPasswordSetupIntent();
        this.watchGoogleLinkResult();
        this.watchUserProfile();
        this.watchPasswordSetupDialog();

        this.watchNotificationRelationshipRefresh();
        this.watchUserFormChanges();

        this.updateDietologistInviteEmailError();

        const unsavedChangesHandler: UnsavedChangesHandler = {
            hasChanges: () => this.hasUnsavedProfileChanges(),
            save: () => {
                if (this.userForm().invalid()) {
                    return false;
                }
                this.onSubmit();
                return true;
            },
            discard: () => {
                this.discardUserFormChanges();
            },
        };
        this.unsavedChangesService.register(unsavedChangesHandler);
        this.destroyRef.onDestroy(() => {
            this.unsavedChangesService.clear(unsavedChangesHandler);
        });

        this.facade.initialize();
        this.loadBillingOverview();
    }

    protected onBeforeUnload(event: BeforeUnloadEvent): void {
        if (this.hasUnsavedProfileChanges()) {
            event.preventDefault();
        }
    }

    private watchLanguageChanges(): void {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildSelectOptions();
            this.updateDietologistInviteEmailError();
        });
    }

    private watchUserFormElement(): void {
        effect(() => {
            const formElement = this.userFormElement()?.nativeElement;
            if (formElement === undefined || !this.isBrowser || this.userFormDomListenersRegistered) {
                return;
            }

            this.userFormDomListenersRegistered = true;
            this.listenToUserFormDomEvents(formElement);
        });
    }

    private listenToUserFormDomEvents(formElement: HTMLFormElement): void {
        if (!this.isBrowser) {
            return;
        }

        const stopInputListener = this.renderer.listen(formElement, 'input', (event: Event) => {
            this.onUserFormInput(event);
        });
        const stopFocusoutListener = this.renderer.listen(formElement, 'focusout', (event: Event) => {
            this.onUserFormInput(event);
        });

        this.destroyRef.onDestroy(() => {
            stopInputListener();
            stopFocusoutListener();
        });
    }

    private watchPasswordSetupIntent(): void {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
            this.pendingPasswordSetupIntent.set(params.get('intent') === 'set-password');
        });
    }

    private watchGoogleLinkResult(): void {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
            const result = params.get('googleLink');
            if (result === 'success') {
                this.toastService.success(this.translateService.instant('USER_MANAGE.GOOGLE_LINK_SUCCESS'));
                void this.clearGoogleLinkQueryParamAsync();
            } else if (result === 'failed') {
                this.toastService.error(this.translateService.instant('USER_MANAGE.GOOGLE_LINK_ERROR'));
                void this.clearGoogleLinkQueryParamAsync();
            }
        });
    }

    private watchUserProfile(): void {
        effect(() => {
            const user = this.facade.user();
            const profileSavedVersion = this.facade.profileSavedVersion();
            if (user === null) {
                return;
            }

            const userData = {
                ...createUserManageFormModel(),
                ...mapUserToForm(user),
            };
            const wasProfileSaved = profileSavedVersion !== this.lastProfileSavedVersion;
            this.lastProfileSavedVersion = profileSavedVersion;
            const shouldApplyUserData =
                wasProfileSaved ||
                untracked(() => {
                    const currentFormData = this.readUserFormValues();
                    return !this.hasUserFormChanges(currentFormData) || this.areUserFormValuesEqual(userData, currentFormData);
                });
            if (shouldApplyUserData) {
                this.applyUserData(userData);
            }
            this.notifications.syncFromUser(user);
        });
    }

    private watchPasswordSetupDialog(): void {
        effect(() => {
            const user = this.facade.user();
            if (user === null || !this.pendingPasswordSetupIntent()) {
                return;
            }

            void this.clearProfileIntentQueryParamAsync();
            this.pendingPasswordSetupIntent.set(false);

            if (!user.hasPassword) {
                this.facade.openChangePasswordDialog();
            }
        });
    }

    private watchNotificationRelationshipRefresh(): void {
        effect(() => {
            const version = this.notifications.notificationsChangedVersion();
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

    private watchUserFormChanges(): void {
        effect(() => {
            this.userFormInputVersion();
            this.facade.clearGlobalError();
        });
    }

    protected onSubmit(): void {
        if (this.userForm().invalid()) {
            return;
        }
        this.facade.saveProfileNow(buildUserUpdateDto(this.readUserFormValues()));
    }

    protected onUserFormSubmit(event: SubmitEvent): void {
        event.preventDefault();
        this.onSubmit();
    }

    protected startUserManageTour(force = true): void {
        this.tourService.start(this.localizedTour.build(USER_MANAGE_TOUR), { force });
    }

    protected onUserFormInput(event?: Event): void {
        if (!this.syncUserFormInputEvent(event)) {
            return;
        }

        this.markUserFormChanged();
    }

    protected onUserFormPatch(patch: UserManageFormPatch): void {
        const formData = {
            ...this.readUserFormValues(),
            ...patch,
        };
        this.userFormModel.set(formData);
        this.markUserFormChanged();
    }

    protected openChangePasswordDialog(): void {
        this.facade.openChangePasswordDialog();
    }

    protected linkGoogle(credential: string): void {
        this.facade.linkGoogle(credential);
    }

    protected onRevokeAiConsent(): void {
        this.facade.revokeAiConsent();
    }

    protected onDeleteAccount(): void {
        this.facade.deleteAccount();
    }

    protected inviteDietologist(): void {
        this.dietologist.inviteDietologist();
    }

    protected updateDietologistPermission(controlName: DietologistPermissionControlName, nextValue: boolean): void {
        this.dietologist.updateDietologistPermission(controlName, nextValue);
    }

    protected onDietologistPermissionChangeRequest(change: DietologistPermissionChange): void {
        this.dietologist.onDietologistPermissionChangeRequest(change);
    }

    protected persistDietologistPermissions(previousPermissions?: DietologistPermissions): void {
        this.dietologist.persistDietologistPermissions(previousPermissions);
    }

    protected revokeDietologistRelationship(): void {
        this.dietologist.revokeDietologistRelationship();
    }

    protected onDietologistProfileToggle(nextValue: boolean): void {
        this.dietologist.onDietologistProfileToggle(nextValue);
    }

    private updateDietologistInviteEmailError(): void {
        this.dietologist.updateDietologistInviteEmailError();
    }

    protected reloadBillingOverview(): void {
        this.billing.reloadBillingOverview();
    }

    protected openPremiumPage(): void {
        void this.router.navigate(['/premium']);
    }

    protected openBillingPortal(): void {
        this.billing.openBillingPortal();
    }

    private applyUserData(userData: Partial<UserFormValues>): void {
        const formData = {
            ...createUserManageFormModel(),
            ...userData,
        };
        this.userForm().reset(formData);
        this.lastSyncedUserFormData.set(formData);
    }

    private readUserFormValues(): UserFormValues {
        const userFormFields = this.userForm;
        return {
            username: userFormFields.username().value(),
            firstName: userFormFields.firstName().value(),
            lastName: userFormFields.lastName().value(),
            email: userFormFields.email().value(),
            birthDate: userFormFields.birthDate().value(),
            gender: userFormFields.gender().value(),
            language: userFormFields.language().value(),
            timeZoneId: userFormFields.timeZoneId().value(),
            theme: userFormFields.theme().value(),
            uiStyle: userFormFields.uiStyle().value(),
            heightCm: userFormFields.heightCm().value(),
            activityLevel: userFormFields.activityLevel().value(),
            stepGoal: userFormFields.stepGoal().value(),
            profileImage: userFormFields.profileImage().value(),
        };
    }

    private syncUserFormInputEvent(event: Event | undefined): boolean {
        const view = this.document.defaultView;
        if (event === undefined || view === null || !this.isBrowser || !(event.target instanceof view.HTMLInputElement)) {
            return false;
        }

        const field = event.target.closest('[data-user-field]')?.getAttribute('data-user-field');
        if (field === null || field === undefined) {
            return false;
        }

        this.syncUserFormFieldValue(field, event.target.value);
        return true;
    }

    private syncUserFormFieldValue(field: string, value: string): void {
        switch (field) {
            case 'username': {
                this.userForm.username().value.set(normalizeOptionalTextInput(value));
                break;
            }
            case 'firstName': {
                this.userForm.firstName().value.set(normalizeOptionalTextInput(value));
                break;
            }
            case 'lastName': {
                this.userForm.lastName().value.set(normalizeOptionalTextInput(value));
                break;
            }
            case 'heightCm': {
                this.userForm.heightCm().value.set(parseOptionalNumberInput(value));
                break;
            }
            default: {
                break;
            }
        }
    }

    private markUserFormChanged(): void {
        this.facade.clearGlobalError();
        this.userFormInputVersion.update(version => version + 1);
    }

    protected discardUserFormChanges(): void {
        const currentImage = this.readUserFormValues().profileImage;
        const syncedFormData = this.lastSyncedUserFormData();
        const syncedImage = syncedFormData.profileImage;
        if (currentImage?.assetId !== null && currentImage?.assetId !== undefined && currentImage.assetId !== syncedImage?.assetId) {
            this.imageUploadFacade
                .deleteAsset(currentImage.assetId)
                .pipe(
                    catchError(() => EMPTY),
                    takeUntilDestroyed(this.destroyRef),
                )
                .subscribe();
        }

        this.userForm().reset(syncedFormData);
        this.facade.clearGlobalError();
        this.userFormInputVersion.update(version => version + 1);
    }

    private hasUserFormChanges(formData: UserFormValues = this.readUserFormValues()): boolean {
        return !this.areUserFormValuesEqual(formData, this.lastSyncedUserFormData());
    }

    private areUserFormValuesEqual(formData: UserFormValues, synced: UserFormValues): boolean {
        const comparableFields = [
            'username',
            'firstName',
            'lastName',
            'email',
            'birthDate',
            'gender',
            'language',
            'timeZoneId',
            'theme',
            'uiStyle',
            'heightCm',
            'activityLevel',
            'stepGoal',
        ] as const;

        return (
            comparableFields.every(field => formData[field] === synced[field]) &&
            formData.profileImage?.url === synced.profileImage?.url &&
            formData.profileImage?.assetId === synced.profileImage?.assetId
        );
    }

    private loadDietologistRelationship(): void {
        this.dietologist.loadDietologistRelationship();
    }

    private loadBillingOverview(): void {
        this.billing.loadBillingOverview();
    }

    private buildSelectOptions(): void {
        const options = buildUserManageSelectOptions(key => this.translateService.instant(key));
        this.genderOptions = options.genderOptions;
        this.activityLevelOptions = options.activityLevelOptions;
        this.languageOptions = options.languageOptions;
        this.themeOptions = options.themeOptions;
        this.uiStyleOptions = options.uiStyleOptions;
    }

    private async clearProfileIntentQueryParamAsync(): Promise<void> {
        await this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { intent: null },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    }

    private async clearGoogleLinkQueryParamAsync(): Promise<void> {
        await this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { googleLink: null },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    }

    private surfaceBusySignals(): boolean[] {
        return [
            this.isSavingProfile(),
            this.isDeleting(),
            this.isRevokingAiConsent(),
            this.notifications.isUpdatingNotifications(),
            this.notifications.isSchedulingTestNotification(),
            this.isSavingDietologist(),
            this.isLoadingDietologist(),
            this.isLoadingBilling(),
            this.isOpeningBillingPortal(),
            this.notifications.isLoadingConnectedDevices(),
            this.notifications.removingConnectedDeviceEndpoint() !== null,
        ];
    }
}

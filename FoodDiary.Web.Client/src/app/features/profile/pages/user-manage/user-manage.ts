import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type ElementRef,
    inject,
    input,
    linkedSignal,
    PLATFORM_ID,
    Renderer2,
    signal,
    untracked,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, validate } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective, FdUiPreventUnloadDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormDirective, focusFirstInvalidField } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, EMPTY, merge } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { PwaInstallCardComponent } from '../../../../components/shared/pwa-install-card/pwa-install-card';
import { UnsavedChangesBarComponent } from '../../../../components/shared/unsaved-changes-bar/unsaved-changes-bar';
import { type UnsavedChangesHandler, UnsavedChangesService } from '../../../../services/unsaved-changes.service';
import { ImageUploadFacade } from '../../../../shared/lib/image-upload.facade';
import type { ActivityLevelOption, Gender, User } from '../../../../shared/models/user.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import type { AppThemeName, AppUiStyleName } from '../../../../theme/app-theme.config';
import { ProfileDietologistFacade } from '../../lib/profile-dietologist.facade';
import { hasProfilePassword, ProfileManageFacade } from '../../lib/profile-manage.facade';
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
} from '../../lib/user-manage-form.mapper';
import { UserManageComparisonWidgetsComponent } from '../user-manage-sections/comparison-widgets/user-manage-comparison-widgets';
import { UserManageDietologistCardComponent } from '../user-manage-sections/dietologist-card/user-manage-dietologist-card';
import { UserManageNotificationsCardComponent } from '../user-manage-sections/notifications-card/user-manage-notifications-card';
import { UserManageBackupEmailComponent } from '../user-manage-sections/security-card/user-manage-backup-email';
import { UserManageSecurityCardComponent } from '../user-manage-sections/security-card/user-manage-security-card';
import { UserManageNotificationsFacade } from './user-manage-lib/user-manage-notifications.facade';
import { USER_MANAGE_TOUR } from './user-manage-tour';

type UserManageFormPatch = UserManageAccountFormPatch | UserManageBodyFormPatch;
type ProfileSaveSnapshot = {
    readonly values: UserFormValues;
    hasNewerEdits: boolean | null;
};
const MAX_PROFILE_HEIGHT_CM = 300;

function firstQueryValue(value: string | string[] | undefined): string | undefined {
    return Array.isArray(value) ? value[0] : value;
}

@Component({
    selector: 'fd-user-manage',
    imports: [
        FdUiPreventUnloadDirective,
        TranslatePipe,
        FdUiFormDirective,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        PageHeaderComponent,
        PwaInstallCardComponent,
        PageBodyComponent,
        UnsavedChangesBarComponent,
        FdPageContainerDirective,
        UserManageDietologistCardComponent,
        UserManageNotificationsCardComponent,
        UserManageSecurityCardComponent,
        UserManageBackupEmailComponent,
        UserManageComparisonWidgetsComponent,
    ],
    templateUrl: './user-manage.html',
    styleUrl: './user-manage.scss',
    providers: [ProfileManageFacade, UserManageNotificationsFacade, ProfileDietologistFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageComponent {
    public readonly intent = input(undefined, { transform: firstQueryValue });
    public readonly googleLink = input(undefined, { transform: firstQueryValue });

    private readonly dietologist = inject(ProfileDietologistFacade);
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
    private readonly comparisonWidgets = viewChild(UserManageComparisonWidgetsComponent);
    private lastProfileSavedVersion = 0;
    private pendingProfileSave: ProfileSaveSnapshot | null = null;
    private activeProfileSave: Promise<boolean> | null = null;
    private readonly pendingPasswordSetupIntent = linkedSignal(() => this.intent() === 'set-password');

    protected genderOptions: Array<FdUiSelectOption<Gender | null>> = [];
    protected activityLevelOptions: Array<FdUiSelectOption<ActivityLevelOption | null>> = [];
    protected languageOptions: Array<FdUiSelectOption<string | null>> = [];
    protected themeOptions: Array<FdUiSelectOption<AppThemeName | null>> = [];
    protected uiStyleOptions: Array<FdUiSelectOption<AppUiStyleName | null>> = [];
    protected readonly userFormModel = signal<UserFormValues>(createUserManageFormModel());
    protected readonly birthDateInputInvalid = signal(false);
    protected readonly heightInputInvalid = signal(false);
    private readonly lastSyncedUserFormData = signal<UserFormValues>(createUserManageFormModel());
    private readonly userFormInputVersion = signal(0);
    protected readonly userForm = form(this.userFormModel, path => {
        validate(path.birthDate, () => (this.birthDateInputInvalid() ? { kind: 'birthDateInput' } : undefined));
        validate(path.heightCm, ({ value }) => {
            if (this.heightInputInvalid()) {
                return { kind: 'heightDraft' };
            }
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
    protected readonly hasLoadedProfile = computed(() => this.facade.user() !== null);
    protected readonly isSavingProfile = this.facade.isSavingProfile;
    protected readonly isSurfaceBusy = computed(() => this.surfaceBusySignals().includes(true));
    protected readonly hasPassword = computed(() => hasProfilePassword(this.facade.user()));
    protected readonly hasGoogleIdentity = computed(() => this.facade.user()?.hasGoogleIdentity ?? false);
    protected readonly hasTelegramIdentity = computed(() => this.facade.user()?.hasTelegramIdentity ?? false);
    protected readonly canUnlinkTelegram = computed(() => {
        const user = this.facade.user();
        return user !== null && (user.hasGoogleIdentity === true || (user.hasPassword && user.isEmailConfirmed && user.email !== null));
    });
    protected readonly isUnlinkingTelegram = this.facade.isUnlinkingTelegram;
    protected readonly telegramUnlinkError = this.facade.telegramUnlinkError;
    protected readonly securityExpanded = signal(false);

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
    protected readonly hasWeightLoadError = this.facade.hasWeightLoadError;
    protected readonly hasWaistLoadError = this.facade.hasWaistLoadError;
    protected readonly dietologistInviteEmailError = this.dietologist.dietologistInviteEmailError;
    protected readonly hasUnsavedProfileChanges = computed(() => {
        this.userFormInputVersion();
        return this.birthDateInputInvalid() || this.heightInputInvalid() || this.hasUserFormChanges();
    });

    protected retryWeight(): void {
        this.facade.retryWeight();
    }

    protected retryWaist(): void {
        this.facade.retryWaist();
    }

    public constructor() {
        effect(() => {
            if (this.telegramUnlinkError() !== null) {
                this.securityExpanded.set(true);
            }
        });
        this.buildSelectOptions();
        this.watchTranslations();
        this.watchGoogleLinkResult();
        this.watchUserProfile();
        this.watchPasswordSetupDialog();

        this.watchNotificationRelationshipRefresh();
        this.watchUserFormChanges();

        this.updateDietologistInviteEmailError();

        const unsavedChangesHandler: UnsavedChangesHandler = {
            hasChanges: () => this.hasUnsavedProfileChanges(),
            save: (): boolean | Promise<boolean> => this.activeProfileSave ?? this.onSubmit(),
            discard: () => {
                this.discardUserFormChanges();
            },
        };
        this.unsavedChangesService.register(unsavedChangesHandler);
        this.destroyRef.onDestroy(() => {
            this.unsavedChangesService.clear(unsavedChangesHandler);
        });

        this.facade.initialize();
    }

    private watchTranslations(): void {
        merge(this.translateService.onLangChange, this.translateService.onTranslationChange)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.buildSelectOptions();
                this.updateDietologistInviteEmailError();
            });
    }

    private watchGoogleLinkResult(): void {
        effect(() => {
            const result = this.googleLink();
            if (result !== 'success' && result !== 'failed') {
                return;
            }
            untracked(() => {
                if (result === 'success') {
                    this.toastService.success(this.translateService.instant('USER_MANAGE.GOOGLE_LINK_SUCCESS'));
                } else {
                    this.toastService.error(this.translateService.instant('USER_MANAGE.GOOGLE_LINK_ERROR'));
                }
                void this.clearGoogleLinkQueryParamAsync();
            });
        });
    }

    private watchUserProfile(): void {
        effect(() => {
            const user = this.facade.user();
            const profileSavedVersion = this.facade.profileSavedVersion();
            if (user === null) {
                return;
            }

            untracked(() => {
                this.syncUserProfileForm(user, profileSavedVersion);
            });
            this.notifications.syncFromUser(user);
        });
    }

    private syncUserProfileForm(user: User, profileSavedVersion: number): void {
        const userData = {
            ...createUserManageFormModel(),
            ...mapUserToForm(user),
        };
        const wasProfileSaved = profileSavedVersion !== this.lastProfileSavedVersion;
        this.lastProfileSavedVersion = profileSavedVersion;
        const currentFormData = this.readUserFormValues();
        const submission = this.pendingProfileSave;
        if (wasProfileSaved && submission !== null) {
            this.pendingProfileSave = null;
            submission.hasNewerEdits = this.hasProfileDraftChanged(submission.values, currentFormData);
            if (submission.hasNewerEdits) {
                this.lastSyncedUserFormData.set(userData);
                return;
            }
        }

        if (wasProfileSaved || !this.hasUserFormChanges(currentFormData) || this.areUserFormValuesEqual(userData, currentFormData)) {
            this.applyUserData(userData);
        }
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
            if (this.notifications.notificationsChangedVersion() !== 0) {
                untracked(() => {
                    this.loadDietologistRelationship();
                });
            }
        });
    }

    private watchUserFormChanges(): void {
        effect(() => {
            this.userFormInputVersion();
            this.facade.clearGlobalError();
        });
    }

    protected onSubmit(): boolean | Promise<boolean> {
        this.userForm().markAsTouched();
        if (this.userForm().invalid() || this.facade.isSavingProfile() || this.activeProfileSave !== null) {
            if (this.userForm().invalid()) {
                this.focusInvalidProfileField();
            }
            return false;
        }
        const user = this.facade.user();
        const profileSavedVersion = this.facade.profileSavedVersion();
        if (user !== null && profileSavedVersion !== this.lastProfileSavedVersion) {
            // The previous save can finish before the profile effect reconciles its form snapshot.
            this.syncUserProfileForm(user, profileSavedVersion);
        }
        const submission: ProfileSaveSnapshot = { values: this.readUserFormValues(), hasNewerEdits: null };
        this.pendingProfileSave = submission;
        this.activeProfileSave = this.facade.saveProfileAsync(buildUserUpdateDto(submission.values)).then(saved => {
            try {
                if (!saved) {
                    this.pendingProfileSave = null;
                    return false;
                }
                return !(submission.hasNewerEdits ?? this.hasProfileDraftChanged(submission.values, this.readUserFormValues()));
            } finally {
                this.activeProfileSave = null;
            }
        });
        return this.activeProfileSave;
    }

    protected onUserFormSubmit(event: SubmitEvent): void {
        event.preventDefault();
        void this.onSubmit();
    }

    private focusInvalidProfileField(): void {
        focusFirstInvalidField(this.userForm);
        if (this.userForm.heightCm().invalid() && !this.userForm.birthDate().invalid()) {
            this.userFormElement()
                ?.nativeElement.querySelector<HTMLInputElement>('[data-profile-height-field] input[aria-invalid="true"]')
                ?.focus();
        }
    }

    protected startUserManageTour(formElement: HTMLFormElement): void {
        const tour = this.localizedTour.build(USER_MANAGE_TOUR);
        for (const step of tour.steps) {
            const section = formElement.querySelector(step.target)?.closest('details');
            if (section !== null && section !== undefined) {
                this.renderer.setProperty(section, 'open', true);
            }
        }
        this.tourService.start(tour, { force: true });
    }

    protected onUserFormInput(event: Event): void {
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

    protected inviteDietologist(): void {
        this.dietologist.inviteDietologist();
    }

    protected updateDietologistPermission(controlName: DietologistPermissionControlName, nextValue: boolean): void {
        this.dietologist.updateDietologistPermission(controlName, nextValue);
    }

    protected onDietologistPermissionChangeRequest(change: DietologistPermissionChange): void {
        this.dietologist.onDietologistPermissionChangeRequest(change);
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

    private syncUserFormInputEvent(event: Event): boolean {
        const view = this.document.defaultView;
        if (view === null || !this.isBrowser || !(event.target instanceof view.HTMLInputElement)) {
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
            default: {
                break;
            }
        }
    }

    protected markUserFormChanged(): void {
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
        this.comparisonWidgets()?.resetHeightInputDrafts();
        this.facade.clearGlobalError();
        this.userFormInputVersion.update(version => version + 1);
    }

    private hasUserFormChanges(formData: UserFormValues = this.readUserFormValues()): boolean {
        return !this.areUserFormValuesEqual(formData, this.lastSyncedUserFormData());
    }

    private hasProfileDraftChanged(submitted: UserFormValues, current: UserFormValues): boolean {
        return this.userForm().invalid() || !this.areUserFormValuesEqual(submitted, current);
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
            this.notifications.isUpdatingNotifications(),
            this.notifications.isSchedulingTestNotification(),
            this.isSavingDietologist(),
            this.isLoadingDietologist(),
            this.notifications.isLoadingConnectedDevices(),
            this.notifications.removingConnectedDeviceEndpoint() !== null,
        ];
    }
}

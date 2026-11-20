import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, type EnvironmentProviders, type Provider, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { defer, firstValueFrom, of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import { type UserProfileOverview, UserService } from '../../shared/api/user.service';
import { TelegramBackupEmailFlowService } from '../../shared/auth/telegram-backup-email-flow.service';
import { TelegramUnlinkFlowService } from '../../shared/auth/telegram-unlink-flow.service';
import { TelegramWebAppService } from '../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../shared/i18n/localization.service';
import { ImageUploadFacade } from '../../shared/lib/image-upload.facade';
import { UserFacade } from '../../shared/lib/user.facade';
import { imageSelection } from '../../shared/models/image-upload.data';
import { entityId } from '../../shared/models/semantics/entity-id';
import { Gender, type User } from '../../shared/models/user.data';
import { BrowserNotificationCapabilityService } from '../../shared/notifications/browser-notification-capability.service';
import { type NotificationPreferences, NotificationService } from '../../shared/notifications/notification.service';
import { PushNotificationService } from '../../shared/notifications/push-notification.service';
import { BrowserStorageService } from '../../shared/platform/browser-storage.service';
import { ViewportService } from '../../shared/platform/viewport.service';
import { PrivacyConsentFacade } from '../../shared/privacy/privacy-consent.facade';
import { ThemeService } from '../../shared/theme/theme.service';
import { DIETOLOGIST_RELATIONSHIP_ACTIONS } from '../dietologist/contracts/relationship-actions';
import { ProfileMeasurementsService, type ProfileMeasurementSummary } from './api/profile-measurements.service';
import profileRoutes from './profile.routes';

const PROFILE_URL = '/profile';
const OTHER_URL = '/other';
const SAVED_AVATAR_URL = 'https://images.example.test/saved-avatar.png';
const NEW_AVATAR_URL = 'https://images.example.test/new-avatar.png';
const SAVED_AVATAR_ASSET = entityId<'image-asset'>('saved-avatar-asset');
const NEW_AVATAR_ASSET = entityId<'image-asset'>('new-avatar-asset');
const SYSTEM_PREFERENCE = 'fd_measurement_system';
const WEIGHT_KG = 70;
const WAIST_CM = 80;
const EXPECTED_WEIGHT_LB = '154.3';
const EXPECTED_WAIST_IN = '31.5';
const CROP_SURFACE_WIDTH = 400;
const CROP_SURFACE_HEIGHT = 300;
const PROFILE_ZONE = 'UTC';
const REPLACEMENT_ZONE = 'America/New_York';
const INITIAL_PREFERENCES: NotificationPreferences = {
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: true,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
};

@Component({ template: '<h1>Another page</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class OtherPageComponent {}

type ProfileFixture = {
    router: RouterTestingHarness;
    readAccount: Mock<UserService['getOverview']>;
    update: Mock<UserService['update']>;
    upload: Mock<ImageUploadFacade['upload']>;
    deleteAsset: Mock<ImageUploadFacade['deleteAsset']>;
    savedPreferences: Map<string, string>;
    applyLanguage: Mock<LocalizationService['applyLanguagePreferenceAsync']>;
    applyAppearance: Mock<ThemeService['syncWithUserPreferences']>;
};

type ProfileReads = {
    account?: ReturnType<UserService['getOverview']>;
    measurements?: ReturnType<ProfileMeasurementsService['getLatest']>;
    locale?: 'en' | 'ru';
    providers?: Array<Provider | EnvironmentProviders>;
};

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
});

async function renderProfileAsync(reads: ProfileReads = {}): Promise<ProfileFixture> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const readAccount = vi.fn<UserService['getOverview']>().mockReturnValue(reads.account ?? of(profileOverview()));
    const update = vi.fn<UserService['update']>();
    const upload = vi.fn<ImageUploadFacade['upload']>();
    const deleteAsset = vi.fn<ImageUploadFacade['deleteAsset']>().mockReturnValue(of(void 0));
    const savedPreferences = new Map<string, string>();
    const applyLanguage = vi.fn<LocalizationService['applyLanguagePreferenceAsync']>().mockResolvedValue(undefined);
    const applyAppearance = vi.fn<ThemeService['syncWithUserPreferences']>();
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'profile', children: profileRoutes },
                { path: 'other', component: OtherPageComponent },
            ]),
            provideTranslateTesting(),
            ...profileBoundaryProviders(),
            { provide: ThemeService, useValue: { syncWithUserPreferences: applyAppearance } },
            {
                provide: UserService,
                useValue: { getOverview: readAccount, update },
            },
            {
                provide: ProfileMeasurementsService,
                useValue: { getLatest: vi.fn().mockReturnValue(reads.measurements ?? of({ weightKg: WEIGHT_KG, waistCm: WAIST_CM })) },
            },
            { provide: ImageUploadFacade, useValue: { upload, deleteAsset } },
            {
                provide: LocalizationService,
                useValue: { getCurrentLanguage: (): string => 'en', applyLanguagePreferenceAsync: applyLanguage },
            },
            {
                provide: BrowserStorageService,
                useValue: {
                    getItem: vi.fn<BrowserStorageService['getItem']>((_scope, key) => savedPreferences.get(key) ?? null),
                    setItem: vi.fn<BrowserStorageService['setItem']>((_scope, key, value) => {
                        savedPreferences.set(key, value);
                    }),
                },
            },
            ...(reads.providers ?? []),
        ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setFallbackLang('en');
    translate.use(reads.locale ?? 'en');
    const router = await RouterTestingHarness.create(PROFILE_URL);
    return { router, readAccount, update, upload, deleteAsset, savedPreferences, applyLanguage, applyAppearance };
}

function profileBoundaryProviders(): Provider[] {
    return [
        { provide: DIETOLOGIST_RELATIONSHIP_ACTIONS, useValue: { getRelationship: () => of(null) } },
        { provide: AuthService, useValue: { isAdmin: () => false } },
        { provide: TelegramWebAppService, useValue: {} },
        { provide: TelegramBackupEmailFlowService, useValue: { read: () => null } },
        { provide: TelegramUnlinkFlowService, useValue: { consumeError: () => null } },
        { provide: NotificationService, useValue: { notificationsChangedVersion: signal(0) } },
        { provide: BrowserNotificationCapabilityService, useValue: { getPermission: (): string => 'unsupported' } },
        {
            provide: PushNotificationService,
            useValue: {
                isSupported: signal(false),
                isSubscribed: signal(false),
                isBusy: signal(false),
                currentSubscriptionEndpoint: signal(null),
            },
        },
        { provide: FrontendObservabilityService, useValue: { recordNotificationSettingsViewed: vi.fn() } },
        { provide: ViewportService, useValue: { isMobile: signal(false) } },
        { provide: PrivacyConsentFacade, useValue: { ensureHealthConsentAsync: vi.fn().mockResolvedValue(true) } },
    ];
}

function profileOverview(overrides: Partial<User> = {}): UserProfileOverview {
    return {
        user: {
            ...INITIAL_PREFERENCES,
            id: entityId<'user'>('avatar-user'),
            email: 'photo@example.test',
            firstName: 'Alex',
            lastName: 'Example',
            hasPassword: true,
            isEmailConfirmed: true,
            isActive: true,
            language: 'en',
            heightCm: 170,
            profileImage: SAVED_AVATAR_URL,
            profileImageAssetId: SAVED_AVATAR_ASSET,
            ...overrides,
        },
        notificationPreferences: INITIAL_PREFERENCES,
        webPushSubscriptions: [],
        dietologistRelationship: null,
    };
}

function profilePage(router: RouterTestingHarness): HTMLElement {
    const host = router.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the profile route');
    }
    return host;
}

function getButton(root: ParentNode, label: string): HTMLButtonElement {
    const found = Array.from(root.querySelectorAll('button')).find(
        button => button.getAttribute('aria-label') === label || button.textContent.trim() === label,
    );
    if (found === undefined) {
        throw new Error(`Expected profile button ${label}`);
    }
    return found;
}

function avatarUrl(router: RouterTestingHarness): string | null | undefined {
    return profilePage(router).querySelector('.profile-comparison__avatar img')?.getAttribute('src');
}

function chooseAvatarAction(router: RouterTestingHarness, action: string): void {
    getButton(profilePage(router), 'USER_MANAGE.COMPARISON.AVATAR_ACTIONS').click();
    router.detectChanges();
    getButton(document, action).click();
    router.detectChanges();
}

function installCropper(): void {
    const prototype = HTMLDialogElement.prototype;
    const descriptors = {
        showModal: Object.getOwnPropertyDescriptor(prototype, 'showModal'),
        close: Object.getOwnPropertyDescriptor(prototype, 'close'),
    };
    Object.defineProperties(prototype, {
        showModal: {
            configurable: true,
            value(this: HTMLDialogElement): void {
                this.open = true;
            },
        },
        close: {
            configurable: true,
            value(this: HTMLDialogElement): void {
                this.open = false;
            },
        },
    });
    onTestFinished(() => {
        for (const [key, descriptor] of Object.entries(descriptors)) {
            if (descriptor === undefined) {
                Reflect.deleteProperty(prototype, key);
            } else {
                Object.defineProperty(prototype, key, descriptor);
            }
        }
    });
    vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage: vi.fn() } as unknown as CanvasRenderingContext2D);
    vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
        callback(new Blob(['avatar crop'], { type: 'image/png' }));
    });
}

async function uploadReplacementAsync(router: RouterTestingHarness): Promise<void> {
    installCropper();
    const host = profilePage(router);
    const input = host.querySelector<HTMLInputElement>('fd-image-upload-field input[type="file"]');
    if (input === null) {
        throw new Error('Expected native avatar file input');
    }
    const chooser = vi.spyOn(input, 'click');
    chooseAvatarAction(router, 'IMAGE_UPLOAD_FIELD.REPLACE');
    expect(chooser).toHaveBeenCalledOnce();
    Object.defineProperty(input, 'files', { configurable: true, value: [new File(['image data'], 'avatar.png', { type: 'image/png' })] });
    input.dispatchEvent(new Event('change', { bubbles: true }));
    await vi.waitFor(() => {
        router.detectChanges();
        expect(host.querySelector('fd-image-upload-field dialog[open]')).not.toBeNull();
    });
    const surface = host.querySelector('.image-upload-field__cropper-body');
    const image = surface?.querySelector('img');
    const submit = host.querySelector<HTMLButtonElement>('.image-upload-field__cropper-actions fd-ui-button:last-child button');
    if (surface === null || image === undefined || image === null || submit === null) {
        throw new Error('Expected rendered avatar crop controls');
    }
    vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, CROP_SURFACE_WIDTH, CROP_SURFACE_HEIGHT));
    Object.defineProperties(image, { naturalWidth: { value: 800 }, naturalHeight: { value: 600 } });
    image.dispatchEvent(new Event('load'));
    router.detectChanges();
    expect(submit.disabled).toBe(false);
    submit.click();
}

function installOptionScrolling(): void {
    const originalScroll = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollIntoView');
    Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
    onTestFinished(() => {
        if (originalScroll === undefined) {
            Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
        } else {
            Object.defineProperty(Element.prototype, 'scrollIntoView', originalScroll);
        }
    });
}

function measurementValue(profile: HTMLElement, kind: 'weight' | 'waist'): HTMLElement {
    const value = profile.querySelector<HTMLElement>(`a[href="/${kind}-history"] strong`);
    if (value === null) {
        throw new Error(`Expected profile ${kind} comparison`);
    }
    return value;
}

describe('Profile avatar ownership', () => {
    it('can discard removal of a saved avatar without deleting it or writing the profile', async () => {
        const { router, update, deleteAsset } = await renderProfileAsync();
        const profile = profilePage(router);
        expect(avatarUrl(router)).toBe(SAVED_AVATAR_URL);
        chooseAvatarAction(router, 'IMAGE_UPLOAD_FIELD.REMOVE');
        expect(avatarUrl(router)).toBeUndefined();
        expect(deleteAsset).not.toHaveBeenCalled();
        expect(update).not.toHaveBeenCalled();

        getButton(profile, 'UNSAVED_CHANGES.DISCARD').click();
        router.detectChanges();

        expect(avatarUrl(router)).toBe(SAVED_AVATAR_URL);
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        expect(deleteAsset).not.toHaveBeenCalled();
    });

    it('persists explicit avatar removal only when the user saves', async () => {
        const { router, update, deleteAsset } = await renderProfileAsync();
        const pending = new Subject<User | null>();
        update.mockReturnValueOnce(pending);
        chooseAvatarAction(router, 'IMAGE_UPLOAD_FIELD.REMOVE');
        getButton(profilePage(router), 'UNSAVED_CHANGES.SAVE').click();
        router.detectChanges();

        expect(update).toHaveBeenCalledOnce();
        expect(update.mock.calls[0][0].profileImage).toBeNull();
        expect(update.mock.calls[0][0].profileImageAssetId).toBeNull();
        expect(pending.observed).toBe(true);
        const saved = profileOverview().user;
        delete saved.profileImage;
        delete saved.profileImageAssetId;
        pending.next(saved);
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profilePage(router).querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
        expect(avatarUrl(router)).toBeUndefined();
        expect(deleteAsset).not.toHaveBeenCalled();
    });
});

describe('Profile replacement draft cleanup', () => {
    it.each([false, true])('cleans up only the discarded upload and restores the saved avatar (delete fails=%s)', async fail => {
        const { router, upload, update, deleteAsset } = await renderProfileAsync();
        upload.mockReturnValueOnce(of(imageSelection(NEW_AVATAR_URL, NEW_AVATAR_ASSET)));
        if (fail) {
            deleteAsset.mockReturnValueOnce(throwError(() => new Error('Cleanup unavailable')));
        }
        await uploadReplacementAsync(router);
        await vi.waitFor(() => {
            router.detectChanges();
            expect(avatarUrl(router)).toBe(NEW_AVATAR_URL);
        });
        expect(upload).toHaveBeenCalledOnce();
        expect(update).not.toHaveBeenCalled();
        expect(deleteAsset).not.toHaveBeenCalled();

        getButton(profilePage(router), 'UNSAVED_CHANGES.DISCARD').click();
        router.detectChanges();

        expect(deleteAsset).toHaveBeenCalledExactlyOnceWith(NEW_AVATAR_ASSET);
        expect(avatarUrl(router)).toBe(SAVED_AVATAR_URL);
        expect(profilePage(router).querySelector('fd-unsaved-changes-bar')).toBeNull();
        expect(profilePage(router).querySelector('.user-manage__form > .user-manage__error')).toBeNull();
    });
});

describe('Profile measurements and route ownership', () => {
    it('updates both comparison cards and the height input when units change without dirtying the profile', async () => {
        installOptionScrolling();
        const measurement = new Subject<ProfileMeasurementSummary>();
        const { router, update, savedPreferences } = await renderProfileAsync({ measurements: measurement });
        const profile = profilePage(router);
        const weight = measurementValue(profile, 'weight');
        const waist = measurementValue(profile, 'waist');
        expect(weight.textContent.trim()).toBe('—');
        expect(waist.textContent.trim()).toBe('—');
        measurement.next({ weightKg: WEIGHT_KG, waistCm: WAIST_CM });
        router.detectChanges();
        expect(weight.textContent).toContain('GENERAL.UNITS.KG');
        expect(waist.textContent).toContain('GENERAL.UNITS.CM');

        getButton(profile, 'USER_MANAGE.COMPARISON.UNIT_SYSTEM_METRIC').click();
        router.detectChanges();
        getButton(document, 'USER_MANAGE.COMPARISON.UNIT_SYSTEM_IMPERIAL').click();
        router.detectChanges();

        expect(weight.textContent).toContain(EXPECTED_WEIGHT_LB);
        expect(weight.textContent).toContain('GENERAL.UNITS.LB');
        expect(waist.textContent).toContain(EXPECTED_WAIST_IN);
        expect(waist.textContent).toContain('GENERAL.UNITS.IN');
        expect(profile.querySelector('.profile-comparison__imperial-height')).not.toBeNull();
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        expect(update).not.toHaveBeenCalled();
        expect(savedPreferences.get(SYSTEM_PREFERENCE)).toBe('imperial');
    });

    it('releases pending account and measurement reads when leaving the profile', async () => {
        const account = new Subject<UserProfileOverview | null>();
        const measurements = new Subject<ProfileMeasurementSummary>();
        const { router, applyLanguage } = await renderProfileAsync({ account, measurements });
        expect(account.observed).toBe(true);
        expect(measurements.observed).toBe(true);

        await router.navigateByUrl(OTHER_URL);
        account.next(profileOverview());
        measurements.next({ weightKg: WEIGHT_KG, waistCm: WAIST_CM });

        expect(TestBed.inject(Router).url).toBe(OTHER_URL);
        expect(account.observed).toBe(false);
        expect(measurements.observed).toBe(false);
        expect(applyLanguage).not.toHaveBeenCalled();
    });
});

describe('Profile measurement recovery', () => {
    it('keeps the account visible after a measurement read fails and shows measurements on return', async () => {
        const response = new Subject<ProfileMeasurementSummary>();
        const getLatest = vi
            .fn<ProfileMeasurementsService['getLatest']>()
            .mockReturnValueOnce(response)
            .mockReturnValueOnce(of({ weightKg: WEIGHT_KG, waistCm: WAIST_CM }));
        const { router, update } = await renderProfileAsync({ measurements: defer(getLatest) });
        const profile = profilePage(router);
        response.error(new Error('Measurements temporarily unavailable'));
        router.detectChanges();

        expect(accountField(profile, '[data-user-field="firstName"] input').value).toBe('Alex');
        expect(measurementValue(profile, 'weight').textContent.trim()).toBe('—');
        expect(measurementValue(profile, 'waist').textContent.trim()).toBe('—');
        expect(profile.querySelector('.user-manage__form > .user-manage__error')).toBeNull();

        await router.navigateByUrl(OTHER_URL, OtherPageComponent);
        await router.navigateByUrl(PROFILE_URL);
        const reopened = profilePage(router);
        expect(measurementValue(reopened, 'weight').textContent.trim().replaceAll(/\s+/gu, ' ')).toBe(`${WEIGHT_KG} GENERAL.UNITS.KG`);
        expect(measurementValue(reopened, 'waist').textContent.trim().replaceAll(/\s+/gu, ' ')).toBe(`${WAIST_CM} GENERAL.UNITS.CM`);
        expect(getLatest).toHaveBeenCalledTimes(2);
        expect(update).not.toHaveBeenCalled();
    });
});

describe('Profile before the application translation bundle loads', () => {
    it('translates the selected controls when entering the profile with Russian already active', async () => {
        const previousDocumentLanguage = document.documentElement.getAttribute('lang');
        onTestFinished(() => {
            if (previousDocumentLanguage === null) {
                document.documentElement.removeAttribute('lang');
            } else {
                document.documentElement.setAttribute('lang', previousDocumentLanguage);
            }
        });
        const { router, update } = await renderProfileAsync({
            locale: 'ru',
            account: of(profileOverview({ language: 'ru-RU', gender: Gender.Female })),
            providers: [provideHttpClient(), provideHttpClientTesting(), { provide: LocalizationService, useClass: LocalizationService }],
        });
        const http = TestBed.inject(HttpTestingController);
        const core = http.expectOne(request => request.url.startsWith('./assets/i18n/ru/core.json?'));
        const app = http.expectOne(request => request.url.startsWith('./assets/i18n/ru/app.json?'));
        http.expectOne(request => request.method === 'GET' && request.url === `${environment.apiUrls.auth}/sessions`).flush([]);
        const profile = profilePage(router);
        expect(TestBed.inject(TranslateService).getCurrentLang()).toBe('ru');
        expect(profile.textContent).toContain('USER_MANAGE.GENDER_OPTIONS.F');
        expect(profile.textContent).toContain('USER_MANAGE.LANGUAGE_OPTIONS.RU');

        core.flush({});
        app.flush({
            USER_MANAGE: {
                EDIT_TITLE: 'Профиль',
                GENDER_OPTIONS: { F: 'Женский' },
                LANGUAGE_OPTIONS: { RU: 'Русский' },
            },
        });
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-page-header')?.textContent).toContain('Профиль');
        });

        expect(profile.textContent).toContain('Женский');
        expect(profile.textContent).toContain('Русский');
        expect(profile.textContent).not.toContain('USER_MANAGE.GENDER_OPTIONS.F');
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        expect(update).not.toHaveBeenCalled();
        http.verify();
    });
});

describe('Profile time zone editor', () => {
    it('keeps uncommitted search local and saves only a chosen time zone', async () => {
        installOptionScrolling();
        const { router, update } = await renderProfileAsync({ account: of(profileOverview({ timeZoneId: PROFILE_ZONE })) });
        const profile = profilePage(router);
        const field = accountField(profile, 'fd-ui-autocomplete input');
        expect(field.value).toBe(PROFILE_ZONE);

        typeIntoProfile(field, 'Unknown Test Zone', router);
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain('USER_MANAGE.TIME_ZONE_NO_RESULTS');
        field.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
        router.detectChanges();
        expect(field.value).toBe(PROFILE_ZONE);
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();

        typeIntoProfile(field, 'UTC', router);
        const sameZone = document.querySelector<HTMLElement>('[role="listbox"] [role="option"]');
        expect(sameZone?.textContent.trim()).toBe(PROFILE_ZONE);
        sameZone?.click();
        router.detectChanges();
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();

        typeIntoProfile(field, 'america/new york', router);
        expect(document.querySelector('[role="listbox"] [role="option"]')?.textContent.trim()).toBe(REPLACEMENT_ZONE);
        const selectZone = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
        field.dispatchEvent(selectZone);
        router.detectChanges();
        expect(selectZone.defaultPrevented).toBe(true);
        expect(field.value).toBe(REPLACEMENT_ZONE);
        expect(update).not.toHaveBeenCalled();

        update.mockReturnValueOnce(of(profileOverview({ timeZoneId: REPLACEMENT_ZONE }).user));
        getButton(profile, 'UNSAVED_CHANGES.SAVE').click();
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
        expect(update).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ timeZoneId: REPLACEMENT_ZONE }));
        expect(field.value).toBe(REPLACEMENT_ZONE);
    });
});

describe('Profile account save and leave', () => {
    it('keeps a cleared surname in the form after a failed write and leaves only after a successful retry', async () => {
        const { router, readAccount, update } = await renderProfileAsync();
        const angularRouter = TestBed.inject(Router);
        const profile = profilePage(router);
        const lastName = accountField(profile, '[data-user-field="lastName"] input');
        expect(lastName.value).toBe('Example');
        typeIntoProfile(lastName, '', router);
        expect(profile.querySelector('.profile-comparison__identity-copy strong')?.textContent.trim()).toBe('Alex');

        const pending = new Subject<User | null>();
        update.mockReturnValueOnce(pending);
        const failedNavigation = angularRouter.navigateByUrl(OTHER_URL);
        await chooseProfileSaveAsync(router);
        expect(update).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ lastName: '' }));
        expect(angularRouter.url).toBe(PROFILE_URL);
        pending.error(new Error('Profile update unavailable'));
        expect(await failedNavigation).toBe(false);
        router.detectChanges();
        expect(angularRouter.url).toBe(PROFILE_URL);
        expect(lastName.value).toBe('');
        expect(profile.querySelector('.user-manage__error [role="alert"]')?.textContent).toContain('USER_MANAGE.UPDATE_ERROR');
        expect(profile.querySelector('fd-unsaved-changes-bar')).not.toBeNull();

        const saved = profileOverview();
        delete saved.user.lastName;
        update.mockReturnValueOnce(of(saved.user));
        const successfulNavigation = angularRouter.navigateByUrl(OTHER_URL);
        await chooseProfileSaveAsync(router);
        expect(await successfulNavigation).toBe(true);
        expect(update).toHaveBeenCalledTimes(2);
        expect(angularRouter.url).toBe(OTHER_URL);

        readAccount.mockReturnValue(of(saved));
        await router.navigateByUrl(PROFILE_URL);
        expect(accountField(profilePage(router), '[data-user-field="lastName"] input').value).toBe('');
        expect(profilePage(router).querySelector('fd-unsaved-changes-bar')).toBeNull();
    });
});

describe('Profile overview ownership', () => {
    it('waits for the authenticated account before allowing a save of its full profile', async () => {
        const account = new Subject<UserProfileOverview | null>();
        const pendingWrite = new Subject<User | null>();
        const { router, update } = await renderProfileAsync({ account });
        const profile = profilePage(router);
        const firstNameSelector = '[data-user-field="firstName"] input';
        update.mockReturnValue(pendingWrite);

        const earlyName = profile.querySelector<HTMLInputElement>(firstNameSelector);
        if (earlyName !== null && !earlyName.disabled && earlyName.closest('[inert]') === null) {
            typeIntoProfile(earlyName, 'Typed before loading', router);
            const actions = profile.querySelector('fd-unsaved-changes-bar');
            if (actions !== null) {
                getButton(actions, 'UNSAVED_CHANGES.SAVE').click();
                router.detectChanges();
            }
        }
        expect(update).not.toHaveBeenCalled();
        expect(account.observed).toBe(true);

        const loaded = profileOverview({ firstName: 'Loaded name' });
        account.next(loaded);
        account.complete();
        router.detectChanges();
        const name = accountField(profile, firstNameSelector);
        expect(name.value).toBe('Loaded name');
        expect(accountField(profile, '[data-user-field="height"] input').value).toBe('170');
        expect(avatarUrl(router)).toBe(SAVED_AVATAR_URL);
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();

        typeIntoProfile(name, 'Saved name', router);
        getButton(profile, 'UNSAVED_CHANGES.SAVE').click();
        expect(update).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                firstName: 'Saved name',
                lastName: 'Example',
                heightCm: 170,
                profileImageAssetId: SAVED_AVATAR_ASSET,
            }),
        );
        pendingWrite.next({ ...loaded.user, firstName: 'Saved name' });
        pendingWrite.complete();
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
    });
});

describe('Profile appearance settings', () => {
    it('discards local selections and applies the server-confirmed theme and style only after saving', async () => {
        installOptionScrolling();
        const { router, update, applyAppearance } = await renderProfileAsync({
            account: of(profileOverview({ theme: 'ocean', uiStyle: 'classic' })),
        });
        applyAppearance.mockClear();
        const profile = profilePage(router);
        const appearance = profile.querySelector('.profile-comparison__appearance');
        const summary = appearance?.querySelector('summary');
        if (!(appearance instanceof HTMLDetailsElement) || !(summary instanceof HTMLElement)) {
            throw new Error('Expected the appearance settings');
        }
        summary.click();
        router.detectChanges();
        expect(appearance.open).toBe(true);

        const ocean = 'USER_MANAGE.THEME_OPTIONS.OCEAN';
        const leaf = 'USER_MANAGE.THEME_OPTIONS.LEAF';
        const dark = 'USER_MANAGE.THEME_OPTIONS.DARK';
        const classic = 'USER_MANAGE.UI_STYLE_OPTIONS.CLASSIC';
        const modern = 'USER_MANAGE.UI_STYLE_OPTIONS.MODERN';
        chooseProfileSelect(router, appearance, ocean, leaf);
        chooseProfileSelect(router, appearance, classic, modern);
        expect(getButton(appearance, leaf).textContent).toContain(leaf);
        expect(getButton(appearance, modern).textContent).toContain(modern);
        expect(applyAppearance).not.toHaveBeenCalled();
        expect(update).not.toHaveBeenCalled();

        getButton(profile, 'UNSAVED_CHANGES.DISCARD').click();
        router.detectChanges();
        expect(getButton(appearance, ocean).textContent).toContain(ocean);
        expect(getButton(appearance, classic).textContent).toContain(classic);
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();

        chooseProfileSelect(router, appearance, ocean, dark);
        chooseProfileSelect(router, appearance, classic, modern);
        update.mockReturnValueOnce(of(profileOverview({ theme: 'dark', uiStyle: 'modern' }).user));
        getButton(profile, 'UNSAVED_CHANGES.SAVE').click();
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
        expect(update).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ theme: 'dark', uiStyle: 'modern' }));
        expect(applyAppearance).toHaveBeenCalledExactlyOnceWith('dark', 'modern', undefined);
    });
});

describe('Profile height editing', () => {
    it('reflects a cleared metric height in both formats and saves the optional value', async () => {
        installOptionScrolling();
        const { router, update } = await renderProfileAsync();
        const profile = profilePage(router);
        const height = accountField(profile, '[data-user-field="height"] input');
        expect(height.value).toBe('170');
        typeIntoProfile(height, '', router);

        const metric = 'USER_MANAGE.COMPARISON.UNIT_SYSTEM_METRIC';
        const imperial = 'USER_MANAGE.COMPARISON.UNIT_SYSTEM_IMPERIAL';
        chooseProfileSelect(router, profile, metric, imperial);
        const parts = Array.from(profile.querySelectorAll<HTMLInputElement>('.profile-comparison__imperial-height input'));
        expect(parts.map(part => part.value)).toEqual(['', '']);
        chooseProfileSelect(router, profile, imperial, metric);
        expect(accountField(profile, '[data-user-field="height"] input').value).toBe('');

        const saved = profileOverview();
        delete saved.user.heightCm;
        update.mockReturnValueOnce(of(saved.user));
        getButton(profile, 'UNSAVED_CHANGES.SAVE').click();
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
        expect(update).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ heightCm: null }));
    });
});

describe('Profile password setup and replacement', () => {
    const oldPassword = 'fixture-old-password-17';
    const newPassword = 'fixture-new-password-28';

    it.each([
        {
            hasPassword: true,
            action: 'USER_MANAGE.CHANGE_PASSWORD',
            submit: 'USER_MANAGE.CHANGE_PASSWORD_SAVE',
            error: 'USER_MANAGE.CHANGE_PASSWORD_ERROR',
            fields: [oldPassword, newPassword, newPassword],
        },
        {
            hasPassword: false,
            action: 'USER_MANAGE.SET_PASSWORD',
            submit: 'USER_MANAGE.SET_PASSWORD_SAVE',
            error: 'USER_MANAGE.SET_PASSWORD_ERROR',
            fields: [newPassword, newPassword],
        },
    ])(
        'can retry $action after a rejected request and reaches sign-in after success',
        async ({ hasPassword, action, submit, error, fields }) => {
            const change = vi.fn<UserFacade['changePassword']>().mockReturnValueOnce(of(false)).mockReturnValue(of(true));
            const set = vi.fn<UserFacade['setPassword']>().mockReturnValueOnce(of(false)).mockReturnValue(of(true));
            const logout = vi.fn<AuthService['onLogoutAsync']>().mockResolvedValue(undefined);
            const { router } = await renderProfileAsync({
                account: of(profileOverview({ hasPassword })),
                providers: [
                    { provide: UserFacade, useValue: { changePassword: change, setPassword: set } },
                    { provide: AuthService, useValue: { isAdmin: (): boolean => false, onLogoutAsync: logout } },
                ],
            });
            const dialog = openProfilePasswordDialog(router, action);
            const inputs = Array.from(dialog.querySelectorAll<HTMLInputElement>('input[type="password"]'));
            expect(inputs).toHaveLength(fields.length);
            for (const [index, input] of inputs.entries()) {
                typeIntoProfile(input, fields[index], router);
            }
            const submitButton = dialog.querySelector('button[type="submit"]');
            if (!(submitButton instanceof HTMLButtonElement)) {
                throw new Error('Expected the password submit action');
            }
            expect(submitButton.textContent).toContain(submit);
            submitButton.click();
            await vi.waitFor(() => {
                TestBed.tick();
                expect(dialog.querySelector('[role="alert"]')?.textContent).toContain(error);
            });
            const target = hasPassword ? change : set;
            const unused = hasPassword ? set : change;
            const request = hasPassword ? { currentPassword: oldPassword, newPassword } : { newPassword };
            expect(target).toHaveBeenCalledExactlyOnceWith(request);
            expect(unused).not.toHaveBeenCalled();
            expect(inputs.map(input => input.value)).toEqual(fields);
            expect(logout).not.toHaveBeenCalled();

            submitButton.click();
            const success = await vi.waitFor(() => {
                TestBed.tick();
                return profilePasswordSuccessButton();
            });
            expect(target.mock.calls).toEqual([[request], [request]]);
            success.click();
            await vi.waitFor(() => {
                expect(logout).toHaveBeenCalledExactlyOnceWith(true);
            });
        },
    );
});

function openProfilePasswordDialog(router: RouterTestingHarness, action: string): HTMLElement {
    const method = profilePage(router).querySelector('fd-user-manage-password-method');
    const summary = method?.closest('details')?.querySelector('summary');
    if (method === null || summary === null || summary === undefined) {
        throw new Error('Expected the rendered profile password controls');
    }
    summary.click();
    router.detectChanges();
    getButton(method, action).click();
    TestBed.tick();
    const dialog = document.querySelector('fd-change-password-dialog');
    if (!(dialog instanceof HTMLElement)) {
        throw new Error('Expected the profile password form');
    }
    return dialog;
}

function profilePasswordSuccessButton(): HTMLButtonElement {
    const modal = document.querySelector('fd-user-manage-password-success-dialog');
    const button = Array.from(modal?.querySelectorAll('button') ?? []).find(element =>
        element.textContent.includes('USER_MANAGE.PASSWORD_SUCCESS_OK'),
    );
    if (button === undefined) {
        throw new Error('Expected the password success action');
    }
    return button;
}

describe('Profile backup email handoff', () => {
    const backupEmail = 'backup@example.test';
    const miniAppProof = 'fixture-mini-app-proof';

    it.each(['browser', 'mini-app'] as const)('keeps the backup email request on the active profile in %s', async mode => {
        const proof = mode === 'mini-app' ? miniAppProof : null;
        const pendingInitialization = new Subject<void>();
        const initialization = firstValueFrom(pendingInitialization);
        const initialize = vi
            .fn<TelegramWebAppService['initializeAsync']>()
            .mockReturnValueOnce(initialization)
            .mockResolvedValue(undefined);
        const send = vi.fn<AuthService['requestTelegramBackupEmail']>().mockReturnValue(of(void 0));
        const startReauthentication = vi.fn<TelegramBackupEmailFlowService['startAsync']>().mockResolvedValue(undefined);
        const markSent = vi.fn<TelegramBackupEmailFlowService['markSent']>();
        const { router, update } = await renderProfileAsync({
            account: of(profileOverview({ email: null, isEmailConfirmed: false, hasPassword: false, hasTelegramIdentity: true })),
            providers: [
                { provide: AuthService, useValue: { isAdmin: (): boolean => false, requestTelegramBackupEmail: send } },
                { provide: TelegramWebAppService, useValue: { initializeAsync: initialize, getInitData: (): string | null => proof } },
                {
                    provide: TelegramBackupEmailFlowService,
                    useValue: { read: (): null => null, startAsync: startReauthentication, markSent },
                },
            ],
        });

        requestBackupEmail(router, backupEmail);
        expect(initialize).toHaveBeenCalledOnce();
        expect(send).not.toHaveBeenCalled();
        expect(startReauthentication).not.toHaveBeenCalled();
        await router.navigateByUrl(OTHER_URL, OtherPageComponent);

        pendingInitialization.next();
        pendingInitialization.complete();
        await initialization;
        await router.fixture.whenStable();
        expect(profilePage(router).textContent).toContain('Another page');
        expect(send).not.toHaveBeenCalled();
        expect(startReauthentication).not.toHaveBeenCalled();
        expect(markSent).not.toHaveBeenCalled();
        expect(update).not.toHaveBeenCalled();

        await router.navigateByUrl(PROFILE_URL);
        requestBackupEmail(router, backupEmail);
        await vi.waitFor(() => {
            if (proof === null) {
                expect(startReauthentication).toHaveBeenCalledExactlyOnceWith(backupEmail);
                expect(send).not.toHaveBeenCalled();
            } else {
                expect(send).toHaveBeenCalledExactlyOnceWith(backupEmail, proof);
                expect(markSent).toHaveBeenCalledExactlyOnceWith(backupEmail);
                expect(startReauthentication).not.toHaveBeenCalled();
            }
        });
    });
});

describe('Profile backup email when browser session storage is unavailable', () => {
    it('shows the failed handoff without opening Telegram and allows retry after storage returns', async () => {
        const backupEmail = 'backup@example.test';
        const stateLength = 43;
        const state = 's'.repeat(stateLength);
        const authorizationUrl = `https://oauth.telegram.org/auth?state=${state}`;
        const account = profileOverview({ email: null, isEmailConfirmed: false, hasPassword: false, hasTelegramIdentity: true });
        const start = vi.fn<AuthService['startTelegramBackupEmail']>().mockReturnValue(of({ authorizationUrl }));
        const initialize = vi.fn<TelegramWebAppService['initializeAsync']>().mockResolvedValue(undefined);
        const open = vi.spyOn(window, 'open').mockReturnValue(null);
        const browserStorage = window.sessionStorage;
        let storageBlocked = true;
        vi.spyOn(window, 'sessionStorage', 'get').mockImplementation(() => {
            if (storageBlocked) {
                throw new DOMException('Session storage denied', 'SecurityError');
            }
            return browserStorage;
        });
        const { router, update } = await renderProfileAsync({
            account: of(account),
            providers: [
                {
                    provide: AuthService,
                    useValue: {
                        isAdmin: (): boolean => false,
                        getUserId: (): User['id'] => account.user.id,
                        startTelegramBackupEmail: start,
                    },
                },
                { provide: TelegramWebAppService, useValue: { initializeAsync: initialize, getInitData: (): null => null } },
                { provide: TelegramBackupEmailFlowService, useClass: TelegramBackupEmailFlowService },
                { provide: BrowserStorageService, useClass: BrowserStorageService },
            ],
        });
        const flow = TestBed.inject(TelegramBackupEmailFlowService);
        onTestFinished(() => {
            storageBlocked = false;
            flow.clear();
        });

        requestBackupEmail(router, backupEmail);
        const profile = profilePage(router);
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('.user-manage__form > .user-manage__error [role="alert"]')?.textContent).toContain(
                'USER_MANAGE.BACKUP_EMAIL_ERROR',
            );
        });
        const widget = profile.querySelector<HTMLElement>('fd-user-manage-backup-email');
        if (widget === null) {
            throw new Error('Expected the rendered recovery widget');
        }
        expect(accountField(widget, 'input[type="email"]').value).toBe(backupEmail);
        expect(open).not.toHaveBeenCalled();
        const send = getButton(widget, 'USER_MANAGE.BACKUP_EMAIL_SEND');
        expect(send.disabled).toBe(false);

        storageBlocked = false;
        send.click();
        await vi.waitFor(() => {
            expect(open).toHaveBeenCalledExactlyOnceWith(authorizationUrl, '_self');
        });
        router.detectChanges();
        expect(start.mock.calls).toEqual([[backupEmail], [backupEmail]]);
        expect(flow.read()).toMatchObject({ userId: account.user.id, email: backupEmail, state, sentAt: null, failed: false });
        expect(profile.querySelector('.user-manage__form > .user-manage__error')).toBeNull();
        expect(update).not.toHaveBeenCalled();
    });
});

function requestBackupEmail(router: RouterTestingHarness, email: string): void {
    const widget = profilePage(router).querySelector<HTMLElement>('fd-user-manage-backup-email');
    const accordion = widget?.closest('details');
    const summary = accordion?.querySelector('summary');
    if (widget === null || summary === null || summary === undefined) {
        throw new Error('Expected backup email settings on the profile');
    }
    summary.click();
    router.detectChanges();
    expect(accordion?.open).toBe(true);
    typeIntoProfile(accountField(widget, 'input[type="email"]'), email, router);
    getButton(widget, 'USER_MANAGE.BACKUP_EMAIL_SEND').click();
}

function chooseProfileSelect(router: RouterTestingHarness, root: ParentNode, activeLabel: string, nextLabel: string): void {
    const trigger = getButton(root, activeLabel);
    trigger.click();
    router.detectChanges();
    const listboxId = trigger.getAttribute('aria-controls');
    const listbox = listboxId === null ? null : document.getElementById(listboxId);
    if (listbox === null) {
        throw new Error(`Expected options for ${activeLabel}`);
    }
    getButton(listbox, nextLabel).click();
    router.detectChanges();
}

function accountField(root: HTMLElement, selector: string): HTMLInputElement {
    const field = root.querySelector(selector);
    if (!(field instanceof HTMLInputElement)) {
        throw new Error(`Expected the profile input ${selector}`);
    }
    return field;
}

function typeIntoProfile(field: HTMLInputElement, value: string, router: RouterTestingHarness): void {
    field.value = value;
    field.dispatchEvent(new Event('input', { bubbles: true }));
    router.detectChanges();
}

async function chooseProfileSaveAsync(router: RouterTestingHarness): Promise<void> {
    const dialog = await vi.waitFor(() => {
        router.detectChanges();
        const current = document.querySelector('fd-unsaved-changes-dialog');
        if (current === null) {
            throw new Error('Expected profile navigation to offer saving');
        }
        return current;
    });
    getButton(dialog, 'UNSAVED_CHANGES.SAVE').click();
}

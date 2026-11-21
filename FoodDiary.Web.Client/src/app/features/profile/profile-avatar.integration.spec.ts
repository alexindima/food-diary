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
import { FEATURE_ACTION_PROVIDERS } from '../../composition/feature-action.providers';
import { AuthService } from '../../services/auth.service';
import { FrontendObservabilityService } from '../../services/frontend-observability.service';
import type { DietologistRelationshipHttpResponse } from '../../shared/api/sdk/generated/model/dietologist-relationship-http-response';
import { type UserProfileOverview, UserService } from '../../shared/api/user.service';
import { GoogleIdentityService } from '../../shared/auth/google-identity.service';
import { TelegramBackupEmailFlowService } from '../../shared/auth/telegram-backup-email-flow.service';
import { TelegramUnlinkFlowService } from '../../shared/auth/telegram-unlink-flow.service';
import { TelegramWebAppService } from '../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../shared/i18n/localization.service';
import { ImageUploadFacade } from '../../shared/lib/image-upload.facade';
import { UserFacade } from '../../shared/lib/user.facade';
import { imageSelection } from '../../shared/models/image-upload.data';
import { calendarDate } from '../../shared/models/semantics/date-value';
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
import { DEFAULT_DIETOLOGIST_PERMISSIONS } from './lib/user-manage.config';
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
    const menuButtons = Array.from(document.querySelectorAll<HTMLButtonElement>('[role="menu"] button[role="menuitem"]'));
    const button = menuButtons.find(item => item.textContent.trim().endsWith(action));
    if (button === undefined) {
        throw new Error(`Expected avatar menu item ${action}`);
    }
    button.click();
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
    it('preserves and saves the username draft after a measurement failure, then reloads the measurements on return', async () => {
        const response = new Subject<ProfileMeasurementSummary>();
        const firstAccount = profileOverview({ username: 'alex-example' });
        const updatedUsername = 'alex-hikes';
        const savedAccount = profileOverview({ username: updatedUsername });
        const getAccount = vi.fn<UserService['getOverview']>().mockReturnValueOnce(of(firstAccount)).mockReturnValueOnce(of(savedAccount));
        const getLatest = vi
            .fn<ProfileMeasurementsService['getLatest']>()
            .mockReturnValueOnce(response)
            .mockReturnValueOnce(of({ weightKg: WEIGHT_KG, waistCm: WAIST_CM }));
        const { router, update } = await renderProfileAsync({ account: defer(getAccount), measurements: defer(getLatest) });
        const profile = profilePage(router);
        const usernameSelector = '[data-user-field="username"] input';
        const username = accountField(profile, usernameSelector);
        expect(username.value).toBe(firstAccount.user.username);
        typeIntoProfile(username, updatedUsername, router);

        response.error(new Error('Measurements temporarily unavailable'));
        router.detectChanges();

        expect(accountField(profile, '[data-user-field="firstName"] input').value).toBe('Alex');
        expect(username.value).toBe(savedAccount.user.username);
        expect(measurementValue(profile, 'weight').textContent.trim()).toBe('—');
        expect(measurementValue(profile, 'waist').textContent.trim()).toBe('—');
        expect(profile.querySelector('.user-manage__form > .user-manage__error')).toBeNull();
        expect(update).not.toHaveBeenCalled();

        update.mockReturnValueOnce(of(savedAccount.user));
        getButton(profile, 'UNSAVED_CHANGES.SAVE').click();
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
        expect(update).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                username: savedAccount.user.username,
                firstName: firstAccount.user.firstName,
                heightCm: firstAccount.user.heightCm,
            }),
        );
        expect(getLatest).toHaveBeenCalledOnce();

        await router.navigateByUrl(OTHER_URL, OtherPageComponent);
        await router.navigateByUrl(PROFILE_URL);
        const reopened = profilePage(router);
        expect(accountField(reopened, usernameSelector).value).toBe(savedAccount.user.username);
        expect(measurementValue(reopened, 'weight').textContent.trim().replaceAll(/\s+/gu, ' ')).toBe(`${WEIGHT_KG} GENERAL.UNITS.KG`);
        expect(measurementValue(reopened, 'waist').textContent.trim().replaceAll(/\s+/gu, ' ')).toBe(`${WAIST_CM} GENERAL.UNITS.CM`);
        expect(getAccount).toHaveBeenCalledTimes(2);
        expect(getLatest).toHaveBeenCalledTimes(2);
        expect(update).toHaveBeenCalledOnce();
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

describe('Profile overview HTTP recovery', () => {
    it('cancels the departed request and recovers from a profile HTTP error on a later visit', async () => {
        const { router } = await renderProfileAsync({ providers: [provideHttpClient(), provideHttpClientTesting(), UserService] });
        const http = TestBed.inject(HttpTestingController);
        const usersUrl = environment.apiUrls.users;
        const overviewRequest = { method: 'GET', url: `${usersUrl}/overview` };
        const updateRequest = { method: 'PATCH', url: `${usersUrl}/info` };
        const sessionsRequest = { method: 'GET', url: `${environment.apiUrls.auth}/sessions?page=1&limit=100` };
        http.expectOne(sessionsRequest).flush([]);
        const departedRead = http.expectOne(overviewRequest);
        expect(departedRead.cancelled).toBe(false);

        await router.navigateByUrl(OTHER_URL, OtherPageComponent);
        expect(departedRead.cancelled).toBe(true);
        await router.navigateByUrl(PROFILE_URL);
        http.expectOne(sessionsRequest).flush([]);
        http.expectOne(overviewRequest).flush('Unavailable', { status: 503, statusText: 'Service Unavailable' });
        router.detectChanges();

        const unavailable = profilePage(router);
        expect(unavailable.querySelector('.user-manage__form > .user-manage__error [role="alert"]')?.textContent).toContain(
            'USER_MANAGE.LOAD_ERROR',
        );
        expect(unavailable.querySelector('fd-user-manage-comparison-widgets[inert]')).not.toBeNull();
        expect(unavailable.querySelector('fd-unsaved-changes-bar')).toBeNull();
        http.expectNone(updateRequest);

        await router.navigateByUrl(OTHER_URL, OtherPageComponent);
        await router.navigateByUrl(PROFILE_URL);
        http.expectOne(sessionsRequest).flush([]);
        const loaded = profileOverview({ firstName: 'Recovered name' });
        http.expectOne(overviewRequest).flush(loaded);
        router.detectChanges();

        const profile = profilePage(router);
        const firstName = accountField(profile, '[data-user-field="firstName"] input');
        expect(profile.querySelector('.user-manage__form > .user-manage__error')).toBeNull();
        expect(profile.querySelector('fd-user-manage-comparison-widgets[inert]')).toBeNull();
        expect(firstName.value).toBe('Recovered name');
        typeIntoProfile(firstName, 'Saved name', router);
        getButton(profile, 'UNSAVED_CHANGES.SAVE').click();
        const patch = http.expectOne(updateRequest);
        expect(patch.request.body).toEqual(expect.objectContaining({ firstName: 'Saved name' }));
        patch.flush({ ...loaded.user, firstName: 'Saved name' });
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
        http.verify();
    });
});

it('keeps a newer dietologist acceptance when the initial profile overview finishes last', async () => {
    const overview = profileOverview();
    const pendingInvitation = {
        invitationId: 'invitation-1',
        status: 'Pending',
        email: 'dietologist@example.test',
        createdAtUtc: '2026-11-18T08:00:00Z',
        expiresAtUtc: '2026-11-25T08:00:00Z',
        permissions: DEFAULT_DIETOLOGIST_PERMISSIONS,
    } satisfies DietologistRelationshipHttpResponse;
    const acceptedInvitation = {
        ...pendingInvitation,
        status: 'Accepted',
        dietologistUserId: 'dietologist-1',
        acceptedAtUtc: '2026-11-19T08:00:00Z',
    } satisfies DietologistRelationshipHttpResponse;
    const { router } = await renderProfileAsync({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            UserService,
            NotificationService,
            ...FEATURE_ACTION_PROVIDERS,
            {
                provide: AuthService,
                useValue: {
                    isAdmin: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isImpersonating: (): boolean => false,
                    getUserId: (): User['id'] => overview.user.id,
                },
            },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        http.verify();
    });
    http.expectOne({ method: 'GET', url: `${environment.apiUrls.auth}/sessions?page=1&limit=100` }).flush([]);
    const older = http.expectOne({ method: 'GET', url: `${environment.apiUrls.users}/overview` });
    const relationshipRequest = { method: 'GET', url: `${environment.apiUrls.dietologist}/relationship` };
    const connected = 'USER_MANAGE.DIETOLOGIST_CONNECTED_INFO';

    // The current notification connection emits this service event when a dietologist accepts.
    TestBed.inject(NotificationService).notifyNotificationsChanged();
    http.expectOne({ method: 'GET', url: environment.apiUrls.auth.replace('/auth', '/notifications') }).flush([]);
    router.detectChanges();
    const latest = await vi.waitFor(() => http.expectOne(relationshipRequest));
    latest.flush(acceptedInvitation);
    router.detectChanges();
    const card = profilePage(router).querySelector('fd-user-manage-dietologist-card');
    const details = card?.closest('details');
    const summary = details?.querySelector('summary');
    if (!(card instanceof HTMLElement) || !(details instanceof HTMLDetailsElement) || !(summary instanceof HTMLElement)) {
        throw new Error('Expected the rendered dietologist accordion');
    }
    expect(card.textContent).toContain(connected);

    older.flush({ ...overview, dietologistRelationship: pendingInvitation });
    router.detectChanges();
    summary.click();
    expect(details.open).toBe(true);
    expect(card.textContent).toContain(connected);
    expect(card.textContent).not.toContain('USER_MANAGE.DIETOLOGIST_PENDING_TITLE');
    http.expectNone(relationshipRequest);
});

it('persists a cleared birthday when reopening the profile', async () => {
    const { router } = await renderProfileAsync({ providers: [provideHttpClient(), provideHttpClientTesting(), UserService] });
    const http = TestBed.inject(HttpTestingController);
    const overviewRequest = { method: 'GET', url: `${environment.apiUrls.users}/overview` };
    const updateRequest = { method: 'PATCH', url: `${environment.apiUrls.users}/info` };
    const sessionsRequest = { method: 'GET', url: `${environment.apiUrls.auth}/sessions?page=1&limit=100` };
    const loaded = profileOverview({ birthDate: calendarDate('1990-01-02') });
    http.expectOne(sessionsRequest).flush([]);
    http.expectOne(overviewRequest).flush(loaded);
    router.detectChanges();

    const profile = profilePage(router);
    const birthdaySelector = 'fd-ui-date-input input';
    const birthday = accountField(profile, birthdaySelector);
    expect(birthday.value).not.toBe('');
    expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
    http.expectNone(updateRequest);

    typeIntoProfile(birthday, '', router);
    expect(birthday.getAttribute('aria-invalid')).toBeNull();
    getButton(profile, 'UNSAVED_CHANGES.SAVE').click();

    const patch = http.expectOne(updateRequest);
    expect(patch.request.body).toHaveProperty('birthDate', null);
    expect(patch.request.serializeBody()).toContain('"birthDate":null');
    const saved = { ...loaded.user, birthDate: null };
    patch.flush(saved);
    await vi.waitFor(() => {
        router.detectChanges();
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
    });

    await router.navigateByUrl(OTHER_URL, OtherPageComponent);
    await router.navigateByUrl(PROFILE_URL);
    http.expectOne(sessionsRequest).flush([]);
    http.expectOne(overviewRequest).flush({ ...loaded, user: saved });
    router.detectChanges();

    const reopened = profilePage(router);
    expect(accountField(reopened, birthdaySelector).value).toBe('');
    expect(reopened.querySelector('fd-unsaved-changes-bar')).toBeNull();
    http.verify();
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

describe('Profile unfinished metric height drafts', () => {
    it('restores the saved height on discard and treats a later intentional clear as empty', async () => {
        const { router, update } = await renderProfileAsync();
        const profile = profilePage(router);
        const height = accountField(profile, '[data-user-field="height"] input');
        expect(height.value).toBe('170');
        typeIntoProfile(height, '180', router);

        typeUnfinishedProfileHeight(height, router);
        expect(height.getAttribute('aria-invalid')).toBe('true');
        getButton(profile, 'UNSAVED_CHANGES.SAVE').click();
        router.detectChanges();
        expect(update).not.toHaveBeenCalled();

        getButton(profile, 'UNSAVED_CHANGES.DISCARD').click();
        router.detectChanges();
        expect(height.value).toBe('170');
        expect(height.getAttribute('aria-invalid')).toBeNull();
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();

        typeUnfinishedProfileHeight(height, router);
        typeIntoProfile(height, '', router);
        expect(height.getAttribute('aria-invalid')).toBeNull();
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

    it('preserves the canonical height and clears the hidden draft when changing units', async () => {
        installOptionScrolling();
        const { router, update } = await renderProfileAsync();
        const profile = profilePage(router);
        const metric = 'USER_MANAGE.COMPARISON.UNIT_SYSTEM_METRIC';
        const imperial = 'USER_MANAGE.COMPARISON.UNIT_SYSTEM_IMPERIAL';
        const height = accountField(profile, '[data-user-field="height"] input');
        typeUnfinishedProfileHeight(height, router);

        chooseProfileSelect(router, profile, metric, metric);
        expect(height.getAttribute('aria-invalid')).toBe('true');
        expect(profile.querySelector('fd-unsaved-changes-bar')).not.toBeNull();

        chooseProfileSelect(router, profile, metric, imperial);
        const feet = accountField(profile, '.profile-comparison__imperial-height fd-ui-input:first-child input');
        const inches = accountField(profile, '.profile-comparison__imperial-height fd-ui-input:last-child input');
        expect([feet.value, inches.value]).toEqual(['5', '6.9']);
        expect(inches.getAttribute('aria-invalid')).toBeNull();
        expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        chooseProfileSelect(router, profile, imperial, metric);
        expect(accountField(profile, '[data-user-field="height"] input').value).toBe('170');
        expect(update).not.toHaveBeenCalled();
    });
});

describe('Profile unfinished imperial height drafts', () => {
    it('requires both invalid parts to be corrected and submits the replacement canonical height', async () => {
        installOptionScrolling();
        const { router, update } = await renderProfileAsync();
        const profile = profilePage(router);
        chooseProfileSelect(router, profile, 'USER_MANAGE.COMPARISON.UNIT_SYSTEM_METRIC', 'USER_MANAGE.COMPARISON.UNIT_SYSTEM_IMPERIAL');
        const feet = accountField(profile, '.profile-comparison__imperial-height fd-ui-input:first-child input');
        const inches = accountField(profile, '.profile-comparison__imperial-height fd-ui-input:last-child input');
        typeUnfinishedProfileHeight(feet, router);
        typeUnfinishedProfileHeight(inches, router);
        expect(feet.getAttribute('aria-invalid')).toBe('true');
        expect(inches.getAttribute('aria-invalid')).toBe('true');

        typeIntoProfile(feet, '5', router);
        expect(feet.getAttribute('aria-invalid')).toBeNull();
        expect(inches.getAttribute('aria-invalid')).toBe('true');
        const save = getButton(profile, 'UNSAVED_CHANGES.SAVE');
        save.click();
        router.detectChanges();
        expect(update).not.toHaveBeenCalled();
        expect(document.activeElement).toBe(inches);

        typeIntoProfile(inches, '8', router);
        expect(inches.getAttribute('aria-invalid')).toBeNull();
        const canonicalHeight = 172.7;
        update.mockReturnValueOnce(of(profileOverview({ heightCm: canonicalHeight }).user));
        save.click();
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
        expect(update).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ heightCm: canonicalHeight }));
    });
});

function typeUnfinishedProfileHeight(input: HTMLInputElement, router: RouterTestingHarness): void {
    const draft = vi.spyOn(input.validity, 'badInput', 'get').mockReturnValue(true);
    typeIntoProfile(input, '', router);
    draft.mockRestore();
}

describe('Profile height error focus', () => {
    const imperialHeight = '.profile-comparison__imperial-height';

    it.each([
        { system: 'metric', selector: '[data-user-field="height"] input', invalid: '301', corrected: '180', canonical: 180 },
        {
            system: 'imperial',
            selector: `${imperialHeight} fd-ui-input:first-child input`,
            invalid: '10',
            corrected: '6',
            canonical: 182.9,
        },
    ])('focuses the invalid $system height when Save is pressed and submits the corrected height', async options => {
        installOptionScrolling();
        const { router, update } = await renderProfileAsync();
        const profile = profilePage(router);
        if (options.system === 'imperial') {
            chooseProfileSelect(
                router,
                profile,
                'USER_MANAGE.COMPARISON.UNIT_SYSTEM_METRIC',
                'USER_MANAGE.COMPARISON.UNIT_SYSTEM_IMPERIAL',
            );
        }
        const height = accountField(profile, options.selector);
        typeIntoProfile(height, options.invalid, router);
        expect(height.getAttribute('aria-invalid')).toBe('true');
        const description = height.getAttribute('aria-describedby');
        expect(document.getElementById(description ?? '')?.textContent).toContain('USER_MANAGE.HEIGHT_INVALID');

        const save = getButton(profile, 'UNSAVED_CHANGES.SAVE');
        save.focus();
        expect(document.activeElement).toBe(save);
        save.click();
        router.detectChanges();
        expect(update).not.toHaveBeenCalled();
        expect(document.activeElement).toBe(height);

        typeIntoProfile(height, options.corrected, router);
        if (options.system === 'imperial') {
            const inches = accountField(profile, `${imperialHeight} fd-ui-input:last-child input`);
            typeIntoProfile(inches, '0', router);
            expect(inches.getAttribute('aria-invalid')).toBeNull();
        }
        expect(height.getAttribute('aria-invalid')).toBeNull();
        update.mockReturnValueOnce(of(profileOverview({ heightCm: options.canonical }).user));
        save.click();
        await vi.waitFor(() => {
            router.detectChanges();
            expect(profile.querySelector('fd-unsaved-changes-bar')).toBeNull();
        });
        expect(update).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ heightCm: options.canonical }));
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

describe('Profile password draft ownership', () => {
    it('discards an unsubmitted password draft on leaving Profile and starts blank on return', async () => {
        vi.spyOn(window.history, 'pushState').mockImplementation(() => {});
        const change = vi.fn<UserFacade['changePassword']>().mockReturnValue(of(false));
        const logout = vi.fn<AuthService['onLogoutAsync']>().mockResolvedValue(undefined);
        const { router } = await renderProfileAsync({
            providers: [
                { provide: UserFacade, useValue: { changePassword: change } },
                { provide: AuthService, useValue: { isAdmin: (): boolean => false, onLogoutAsync: logout } },
            ],
        });
        const action = 'USER_MANAGE.CHANGE_PASSWORD';
        const dialog = openProfilePasswordDialog(router, action);
        const input = accountField(dialog, 'input[type="password"]');
        const draft = 'unsaved-fixture-password';
        typeIntoProfile(input, draft, router);
        expect(input.value).toBe(draft);

        await router.navigateByUrl(OTHER_URL, OtherPageComponent);
        expect(TestBed.inject(Router).url).toBe(OTHER_URL);
        expect(router.routeNativeElement?.textContent).toContain('Another page');
        expect(dialog.isConnected).toBe(false);
        expect(document.querySelector('fd-change-password-dialog')).toBeNull();

        await router.navigateByUrl(PROFILE_URL);
        const reopened = openProfilePasswordDialog(router, action);
        expect(reopened).not.toBe(dialog);
        expect(accountField(reopened, 'input[type="password"]').value).toBe('');
        expect(change).not.toHaveBeenCalled();
        expect(logout).not.toHaveBeenCalled();
    });
});

describe('Profile password submission after leaving', () => {
    it.each([
        { scenario: 'completes an already submitted change and requests sign-in', saved: true },
        { scenario: 'discards a rejected password change after the owner has left', saved: false },
    ])('$scenario', async ({ saved }) => {
        vi.spyOn(window.history, 'pushState').mockImplementation(() => {});
        const pending = new Subject<boolean>();
        const change = vi.fn<UserFacade['changePassword']>().mockReturnValue(pending);
        const logout = vi.fn<AuthService['onLogoutAsync']>().mockResolvedValue(undefined);
        const { router } = await renderProfileAsync({
            providers: [
                { provide: UserFacade, useValue: { changePassword: change } },
                { provide: AuthService, useValue: { isAdmin: (): boolean => false, onLogoutAsync: logout } },
            ],
        });
        const dialog = openProfilePasswordDialog(router, 'USER_MANAGE.CHANGE_PASSWORD');
        const inputs = Array.from(dialog.querySelectorAll<HTMLInputElement>('input[type="password"]'));
        const fields = ['current-fixture-password', 'new-fixture-password', 'new-fixture-password'];
        expect(inputs).toHaveLength(fields.length);
        for (const [index, field] of inputs.entries()) {
            typeIntoProfile(field, fields[index], router);
        }
        const submit = dialog.querySelector<HTMLButtonElement>('button[type="submit"]');
        if (submit === null) {
            throw new Error('Expected the password submit action');
        }
        submit.click();
        await vi.waitFor(() => {
            expect(change).toHaveBeenCalledExactlyOnceWith({ currentPassword: fields[0], newPassword: fields[1] });
        });

        await router.navigateByUrl(OTHER_URL, OtherPageComponent);
        expect(pending.observed).toBe(true);
        expect(logout).not.toHaveBeenCalled();

        pending.next(saved);
        pending.complete();
        if (!saved) {
            await vi.waitFor(() => {
                TestBed.tick();
                expect(dialog.isConnected).toBe(false);
            });
            expect(TestBed.inject(Router).url).toBe(OTHER_URL);
            expect(document.querySelector('fd-user-manage-password-success-dialog')).toBeNull();
            expect(logout).not.toHaveBeenCalled();
            return;
        }
        const success = await vi.waitFor(() => {
            TestBed.tick();
            return profilePasswordSuccessButton();
        });
        success.click();
        await vi.waitFor(() => {
            expect(logout).toHaveBeenCalledExactlyOnceWith(true);
        });
        expect(dialog.isConnected).toBe(false);
    });
});

describe('Profile Google connection', () => {
    it('deduplicates the open connection request and keeps the rendered Google control available for retry', async () => {
        const originalClientId = environment.googleClientId;
        environment.googleClientId = 'fixture-profile-google-client';
        onTestFinished(() => {
            environment.googleClientId = originalClientId;
        });
        const credential = 'synthetic-profile-google-response';
        const googleLabel = 'Connect test Google account';
        const initializeAsync = vi.fn<GoogleIdentityService['initializeAsync']>().mockResolvedValue(undefined);
        const renderButton: GoogleIdentityService['renderButton'] = host => {
            const link = document.createElement('button');
            link.type = 'button';
            link.textContent = googleLabel;
            link.addEventListener('click', () => {
                initializeAsync.mock.lastCall?.[0].callback(credential);
            });
            host.replaceChildren(link);
        };
        const pending = new Subject<User>();
        const account = profileOverview({ hasGoogleIdentity: false });
        const linked = { ...account.user, hasGoogleIdentity: true };
        const connect = vi.fn<AuthService['linkGoogle']>().mockReturnValueOnce(pending).mockReturnValueOnce(of(linked));
        const { router, update } = await renderProfileAsync({
            account: of(account),
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: GoogleIdentityService, useValue: { initializeAsync, renderButton } },
                { provide: AuthService, useValue: { isAdmin: (): boolean => false, linkGoogle: connect } },
            ],
        });
        const http = TestBed.inject(HttpTestingController);
        http.expectOne(request => request.method === 'GET' && request.url === `${environment.apiUrls.auth}/sessions`).flush([]);
        const profile = profilePage(router);
        const security = profile.querySelector('fd-user-manage-security-card');
        const summary = security?.closest('details')?.querySelector('summary');
        if (security === null || !(summary instanceof HTMLElement)) {
            throw new Error('Expected the rendered profile security section');
        }
        summary.click();
        const google = await vi.waitFor(() => {
            router.detectChanges();
            return getButton(security, googleLabel);
        });

        google.click();
        google.click();
        router.detectChanges();
        expect(connect).toHaveBeenCalledExactlyOnceWith(credential);
        expect(google.closest('[aria-busy]')?.getAttribute('aria-busy')).toBe('true');

        pending.error(new Error('Connection unavailable'));
        router.detectChanges();
        expect(profile.querySelector('.user-manage__form > .user-manage__error')?.textContent).toContain('USER_MANAGE.GOOGLE_LINK_ERROR');
        expect(google.closest('[aria-busy]')?.getAttribute('aria-busy')).toBe('false');
        expect(security.textContent).toContain('USER_MANAGE.GOOGLE_NOT_CONNECTED');

        google.click();
        router.detectChanges();
        expect(connect.mock.calls).toEqual([[credential], [credential]]);
        expect(google.isConnected).toBe(false);
        expect(security.textContent).toContain('USER_MANAGE.GOOGLE_CONNECTED_AS');
        expect(profile.querySelector('.user-manage__form > .user-manage__error')).toBeNull();
        expect(update).not.toHaveBeenCalled();
        http.verify();
    });
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

describe('Profile while a legacy Telegram disconnect is pending', () => {
    it('keeps the connected account and the pending attempt when viewing the same profile', async () => {
        const account = profileOverview({ hasTelegramIdentity: true });
        const key = 'fooddiary.telegram.unlink';
        const stateLength = 43;
        const attemptTtlMs = 300_000;
        const attempt = JSON.stringify({
            userId: account.user.id,
            state: 's'.repeat(stateLength),
            expiresAt: Date.now() + attemptTtlMs,
            errorKey: null,
        });
        window.sessionStorage.setItem(key, attempt);
        onTestFinished(() => {
            window.sessionStorage.removeItem(key);
        });
        const complete = vi.fn<AuthService['completeTelegramUnlink']>();
        const { router } = await renderProfileAsync({
            account: of(account),
            providers: [
                {
                    provide: AuthService,
                    useValue: {
                        isAdmin: (): boolean => false,
                        getUserId: (): User['id'] => account.user.id,
                        completeTelegramUnlink: complete,
                    },
                },
                { provide: TelegramUnlinkFlowService, useClass: TelegramUnlinkFlowService },
                { provide: BrowserStorageService, useClass: BrowserStorageService },
            ],
        });

        const connection = profilePage(router).querySelector('fd-user-manage-telegram-connection');
        expect(connection?.textContent).toContain('USER_MANAGE.METHOD_CONNECTED');
        expect(connection?.querySelector('.user-manage__method-feedback')).toBeNull();
        expect(window.sessionStorage.getItem(key)).toBe(attempt);
        expect(complete).not.toHaveBeenCalled();
    });
});

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

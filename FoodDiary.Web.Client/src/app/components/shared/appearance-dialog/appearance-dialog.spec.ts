import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { UserFacade } from '../../../shared/lib/user.facade';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { User } from '../../../shared/models/user.data';
import { ThemeService } from '../../../shared/theme/theme.service';
import { type AppearanceDialogData, AppearanceFacade } from './appearance.facade';
import { AppearanceDialogComponent } from './appearance-dialog';

let fixture: ComponentFixture<AppearanceDialogComponent>;
let appearance: AppearanceFacade;
let themeService: ReturnType<typeof createThemeServiceMock>;
let userService: { updateAppearance: ReturnType<typeof vi.fn> };

beforeEach(async () => {
    themeService = createThemeServiceMock();
    userService = {
        updateAppearance: vi.fn().mockReturnValue(
            of({
                theme: 'leaf',
                uiStyle: 'modern',
            }),
        ),
    };

    await TestBed.configureTestingModule({
        imports: [AppearanceDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: ThemeService, useValue: themeService },
            { provide: UserFacade, useValue: userService },
            {
                provide: FD_UI_DIALOG_DATA,
                useValue: {
                    theme: 'ocean',
                    uiStyle: 'classic',
                },
            },
        ],
    }).compileComponents();
});

function renderAppearanceDialog(persistence?: AppearanceDialogData['persistence']): void {
    if (persistence !== undefined) {
        TestBed.inject<AppearanceDialogData>(FD_UI_DIALOG_DATA).persistence = persistence;
    }
    fixture = TestBed.createComponent(AppearanceDialogComponent);
    appearance = fixture.debugElement.injector.get(AppearanceFacade);
    fixture.detectChanges();
}

it('shows device-only persistence and saves guest preferences without calling the account API', () => {
    renderAppearanceDialog('local');
    appearanceButton('.appearance-dialog__swatch--dark').click();
    appearanceButton('.appearance-dialog__style-preview--modern').click();
    const glass = Array.from(
        appearanceRoot().querySelectorAll<HTMLButtonElement>('.appearance-dialog__surface-toggle [role="radio"]'),
    ).find(option => option.textContent.trim() === 'DASHBOARD.APPEARANCE.SURFACE_GLASS');
    glass?.click();
    fixture.detectChanges();

    expect(appearanceRoot().textContent).toContain('DASHBOARD.APPEARANCE.LOCAL_DEVICE_HINT');
    expect(themeService.setTheme).toHaveBeenCalledWith('dark');
    expect(themeService.setUiStyle).toHaveBeenCalledWith('modern');
    expect(themeService.setSurfaceStyle).toHaveBeenCalledWith('glass');
    expect(glass?.getAttribute('aria-checked')).toBe('true');
    expect(userService.updateAppearance).not.toHaveBeenCalled();
    expect(appearance.hasChanges()).toBe(false);
    expect(appearance.isSaving()).toBe(false);
    expect(appearance.submitError()).toBeNull();
});

describe('AppearanceDialogComponent', () => {
    beforeEach(() => {
        renderAppearanceDialog();
    });

    it('applies preview when selecting theme and style', () => {
        appearance.selectTheme('leaf');
        appearance.selectUiStyle('modern');

        expect(themeService.setTheme).toHaveBeenCalledWith('leaf');
        expect(themeService.setUiStyle).toHaveBeenCalledWith('modern');
    });

    it('persists selected appearance immediately after choosing it', () => {
        appearance.selectTheme('leaf');
        appearance.selectUiStyle('modern');

        expect(userService.updateAppearance).toHaveBeenCalledTimes(1);
        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('leaf', 'modern', 'normal');
    });

    it('reverts preview and shows error when autosave fails', () => {
        userService.updateAppearance.mockReturnValueOnce(throwError(() => new Error('save failed')));
        appearance.selectTheme('dark');

        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('ocean', 'classic', 'normal');
        expect(appearance.submitError()).toBeTruthy();
    });

    it('persists surface selection and rolls back failed previews', () => {
        userService.updateAppearance.mockReturnValueOnce(of({ theme: 'ocean', uiStyle: 'classic', surfaceStyle: 'matte' }));
        appearance.selectSurfaceStyle('matte');
        expect(userService.updateAppearance).toHaveBeenLastCalledWith(expect.objectContaining({ surfaceStyle: 'matte' }));
        expect(themeService.setSurfaceStyle).toHaveBeenCalledWith('matte');
        userService.updateAppearance.mockReturnValueOnce(throwError(() => new Error('save failed')));
        appearance.selectSurfaceStyle('glass');
        expect(appearance.selectedSurfaceStyle()).toBe('matte');
        expect(themeService.syncWithUserPreferences).toHaveBeenLastCalledWith('ocean', 'classic', 'matte');
        expect(appearance.submitError()).toBeTruthy();
    });

    it('queues the latest selection while a save is in flight', () => {
        const saveResponse$ = new Subject<{ theme: string; uiStyle: string }>();
        userService.updateAppearance.mockImplementationOnce(() => saveResponse$.asObservable());

        appearance.selectTheme('leaf');
        appearance.selectUiStyle('modern');

        appearance.selectSurfaceStyle('glass');

        expect(userService.updateAppearance).toHaveBeenCalledTimes(1);

        saveResponse$.next({ theme: 'leaf', uiStyle: 'classic' });
        saveResponse$.complete();

        expect(userService.updateAppearance).toHaveBeenCalledTimes(2);
        const queuedAppearance = userService.updateAppearance.mock.calls[1]?.[0] as { theme: string; uiStyle: string } | undefined;
        if (queuedAppearance === undefined) {
            throw new Error('Expected queued appearance update.');
        }

        expect(queuedAppearance.theme).toBe('leaf');
        expect(queuedAppearance.uiStyle).toBe('modern');
        expect(queuedAppearance).toEqual(expect.objectContaining({ surfaceStyle: 'glass' }));
    });
});

describe('Appearance dialog account responses', () => {
    beforeEach(() => {
        renderAppearanceDialog();
    });

    it('leaves the selected controls pressed without writing the same preferences again', () => {
        const ocean = appearanceButton('.appearance-dialog__swatch--ocean');
        const classic = appearanceButton('.appearance-dialog__style-preview--classic');

        ocean.click();
        classic.click();
        appearance.selectSurfaceStyle('unsupported-surface');
        fixture.detectChanges();

        expect(ocean.getAttribute('aria-pressed')).toBe('true');
        expect(classic.getAttribute('aria-pressed')).toBe('true');
        expect(appearance.selectedSurfaceStyle()).toBe('normal');
        expect(themeService.setSurfaceStyle).not.toHaveBeenCalled();
        expect(userService.updateAppearance).not.toHaveBeenCalled();
    });

    it('rolls back a rejected account update and lets the same theme be retried', () => {
        userService.updateAppearance
            .mockReturnValueOnce(of(null))
            .mockReturnValueOnce(of({ ...ACCOUNT_WITHOUT_APPEARANCE, theme: 'dark', uiStyle: 'classic', surfaceStyle: 'normal' }));
        const ocean = appearanceButton('.appearance-dialog__swatch--ocean');
        const dark = appearanceButton('.appearance-dialog__swatch--dark');

        dark.click();
        fixture.detectChanges();

        expect(ocean.getAttribute('aria-pressed')).toBe('true');
        expect(dark.getAttribute('aria-pressed')).toBe('false');
        expect(appearanceRoot().querySelector('[role="alert"]')?.textContent).toContain('DASHBOARD.APPEARANCE.ERROR');
        expect(themeService.syncWithUserPreferences).toHaveBeenLastCalledWith('ocean', 'classic', 'normal');

        dark.click();
        fixture.detectChanges();

        expect(userService.updateAppearance).toHaveBeenCalledTimes(2);
        expect(dark.getAttribute('aria-pressed')).toBe('true');
        expect(appearanceRoot().querySelector('[role="alert"]')).toBeNull();
        expect(themeService.syncWithUserPreferences).toHaveBeenLastCalledWith('dark', 'classic', 'normal');
    });

    it('keeps the selected preview when a legacy account response omits appearance fields', () => {
        userService.updateAppearance.mockReturnValueOnce(of(ACCOUNT_WITHOUT_APPEARANCE));
        const dark = appearanceButton('.appearance-dialog__swatch--dark');

        dark.click();
        fixture.detectChanges();

        expect(userService.updateAppearance).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ theme: 'dark', uiStyle: 'classic', surfaceStyle: 'normal' }),
        );
        expect(themeService.syncWithUserPreferences).toHaveBeenLastCalledWith('dark', 'classic', 'normal');
        expect(dark.getAttribute('aria-pressed')).toBe('true');
        expect(appearance.hasChanges()).toBe(false);
    });
});

const ACCOUNT_WITHOUT_APPEARANCE: User = {
    id: entityId<'user'>('local-appearance-user'),
    email: null,
    hasPassword: true,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 0,
    fastingCheckInFollowUpReminderHours: 0,
    isActive: true,
    isEmailConfirmed: true,
};

function appearanceRoot(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function appearanceButton(previewSelector: string): HTMLButtonElement {
    const button = appearanceRoot().querySelector(previewSelector)?.closest('button');
    if (button === null || button === undefined) {
        throw new Error(`No appearance button for ${previewSelector}`);
    }
    return button;
}

function createThemeServiceMock(): {
    setTheme: ReturnType<typeof vi.fn>;
    setUiStyle: ReturnType<typeof vi.fn>;
    setSurfaceStyle: ReturnType<typeof vi.fn>;
    surfaceStyle: ReturnType<typeof signal<string>>;
    syncWithUserPreferences: ReturnType<typeof vi.fn>;
} {
    return {
        setTheme: vi.fn(),
        setUiStyle: vi.fn(),
        setSurfaceStyle: vi.fn(),
        surfaceStyle: signal('normal'),
        syncWithUserPreferences: vi.fn(),
    };
}

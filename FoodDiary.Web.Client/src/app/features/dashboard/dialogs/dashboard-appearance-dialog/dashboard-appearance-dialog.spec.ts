import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { ThemeService } from '../../../../shared/theme/theme.service';
import { DashboardAppearanceFacade } from './dashboard-appearance.facade';
import { DashboardAppearanceDialogComponent } from './dashboard-appearance-dialog';

let fixture: ComponentFixture<DashboardAppearanceDialogComponent>;
let appearance: DashboardAppearanceFacade;
let themeService: ReturnType<typeof createThemeServiceMock>;
let userService: { updateAppearance: ReturnType<typeof vi.fn> };

describe('DashboardAppearanceDialogComponent', () => {
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
            imports: [DashboardAppearanceDialogComponent],
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

        fixture = TestBed.createComponent(DashboardAppearanceDialogComponent);
        appearance = fixture.debugElement.injector.get(DashboardAppearanceFacade);
        fixture.detectChanges();
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

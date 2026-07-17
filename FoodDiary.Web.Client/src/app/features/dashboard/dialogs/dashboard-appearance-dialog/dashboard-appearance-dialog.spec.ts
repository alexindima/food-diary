import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { ThemeService } from '../../../../shared/theme/theme.service';
import { DashboardAppearanceFacade } from './dashboard-appearance.facade';
import { DashboardAppearanceDialogComponent } from './dashboard-appearance-dialog';

describe('DashboardAppearanceDialogComponent', () => {
    let fixture: ComponentFixture<DashboardAppearanceDialogComponent>;
    let appearance: DashboardAppearanceFacade;
    let themeService: {
        setTheme: ReturnType<typeof vi.fn>;
        setUiStyle: ReturnType<typeof vi.fn>;
        syncWithUserPreferences: ReturnType<typeof vi.fn>;
    };
    let userService: {
        updateAppearance: ReturnType<typeof vi.fn>;
    };

    beforeEach(async () => {
        themeService = {
            setTheme: vi.fn(),
            setUiStyle: vi.fn(),
            syncWithUserPreferences: vi.fn(),
        };
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
        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('leaf', 'modern');
    });

    it('reverts preview and shows error when autosave fails', () => {
        userService.updateAppearance.mockReturnValueOnce(throwError(() => new Error('save failed')));
        appearance.selectTheme('dark');

        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('ocean', 'classic');
        expect(appearance.submitError()).toBeTruthy();
    });

    it('queues the latest selection while a save is in flight', () => {
        const saveResponse$ = new Subject<{ theme: string; uiStyle: string }>();
        userService.updateAppearance.mockImplementationOnce(() => saveResponse$.asObservable());

        appearance.selectTheme('leaf');
        appearance.selectUiStyle('modern');

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
    });
});

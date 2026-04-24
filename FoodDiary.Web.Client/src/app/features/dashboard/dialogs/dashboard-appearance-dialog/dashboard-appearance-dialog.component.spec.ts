import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { ThemeService } from '../../../../services/theme.service';
import { UserService } from '../../../../shared/api/user.service';
import { DashboardAppearanceDialogComponent } from './dashboard-appearance-dialog.component';

describe('DashboardAppearanceDialogComponent', () => {
    let fixture: ComponentFixture<DashboardAppearanceDialogComponent>;
    let component: DashboardAppearanceDialogComponent;
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
            imports: [DashboardAppearanceDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: ThemeService, useValue: themeService },
                { provide: UserService, useValue: userService },
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
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('applies preview when selecting theme and style', () => {
        component.selectTheme('leaf');
        component.selectUiStyle('modern');

        expect(themeService.setTheme).toHaveBeenCalledWith('leaf');
        expect(themeService.setUiStyle).toHaveBeenCalledWith('modern');
    });

    it('persists selected appearance immediately after choosing it', () => {
        component.selectTheme('leaf');
        component.selectUiStyle('modern');

        expect(userService.updateAppearance).toHaveBeenCalledTimes(1);
        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('leaf', 'modern');
    });

    it('reverts preview and shows error when autosave fails', () => {
        userService.updateAppearance.mockReturnValueOnce(throwError(() => new Error('save failed')));
        component.selectTheme('dark');

        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('ocean', 'classic');
        expect(component.submitError()).toBeTruthy();
    });

    it('queues the latest selection while a save is in flight', () => {
        const saveResponse$ = new Subject<{ theme: string; uiStyle: string }>();
        userService.updateAppearance.mockImplementationOnce(
            () => saveResponse$.asObservable(),
        );

        component.selectTheme('leaf');
        component.selectUiStyle('modern');

        expect(userService.updateAppearance).toHaveBeenCalledTimes(1);

        saveResponse$.next({ theme: 'leaf', uiStyle: 'classic' });
        saveResponse$.complete();

        expect(userService.updateAppearance).toHaveBeenCalledTimes(2);
        expect(userService.updateAppearance.mock.calls[1][0].theme).toBe('leaf');
        expect(userService.updateAppearance.mock.calls[1][0].uiStyle).toBe('modern');
    });
});

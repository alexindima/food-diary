import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { ThemeService } from '../../../../services/theme.service';
import { UserService } from '../../../../shared/api/user.service';
import { DashboardAppearanceDialogComponent } from './dashboard-appearance-dialog.component';

describe('DashboardAppearanceDialogComponent', () => {
    let fixture: ComponentFixture<DashboardAppearanceDialogComponent>;
    let component: DashboardAppearanceDialogComponent;
    let dialogRef: { close: ReturnType<typeof vi.fn> };
    let themeService: {
        setTheme: ReturnType<typeof vi.fn>;
        setUiStyle: ReturnType<typeof vi.fn>;
        syncWithUserPreferences: ReturnType<typeof vi.fn>;
    };
    let userService: {
        update: ReturnType<typeof vi.fn>;
    };

    beforeEach(async () => {
        dialogRef = { close: vi.fn() };
        themeService = {
            setTheme: vi.fn(),
            setUiStyle: vi.fn(),
            syncWithUserPreferences: vi.fn(),
        };
        userService = {
            update: vi.fn().mockReturnValue(
                of({
                    theme: 'leaf',
                    uiStyle: 'modern',
                }),
            ),
        };

        await TestBed.configureTestingModule({
            imports: [DashboardAppearanceDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: FdUiDialogRef, useValue: dialogRef },
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

    it('reverts preview on cancel', () => {
        component.selectTheme('leaf');
        component.selectUiStyle('modern');

        component.close();

        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('ocean', 'classic');
        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('persists selected appearance on save', () => {
        component.selectTheme('leaf');
        component.selectUiStyle('modern');

        component.save();

        expect(userService.update).toHaveBeenCalled();
        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('leaf', 'modern');
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });

    it('reverts preview and shows error when save fails', () => {
        userService.update.mockReturnValueOnce(throwError(() => new Error('save failed')));
        component.selectTheme('dark');
        component.selectUiStyle('modern');

        component.save();

        expect(themeService.syncWithUserPreferences).toHaveBeenCalledWith('ocean', 'classic');
        expect(component.submitError()).toBeTruthy();
        expect(dialogRef.close).not.toHaveBeenCalledWith(true);
    });
});

import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { finalize } from 'rxjs';

import { ThemeService } from '../../../../services/theme.service';
import { UserService } from '../../../../shared/api/user.service';
import { UpdateUserAppearanceDto } from '../../../../shared/models/user.data';
import {
    APP_THEMES,
    APP_UI_STYLES,
    AppThemeName,
    AppUiStyleName,
    isAppThemeName,
    isAppUiStyleName,
} from '../../../../theme/app-theme.config';

export interface DashboardAppearanceDialogData {
    theme: AppThemeName;
    uiStyle: AppUiStyleName;
}

@Component({
    selector: 'fd-dashboard-appearance-dialog',
    standalone: true,
    templateUrl: './dashboard-appearance-dialog.component.html',
    styleUrl: './dashboard-appearance-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiDialogComponent],
})
export class DashboardAppearanceDialogComponent {
    private readonly themeService = inject(ThemeService);
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly data = inject<DashboardAppearanceDialogData>(FD_UI_DIALOG_DATA);
    private readonly initialTheme = this.data.theme;
    private readonly initialUiStyle = this.data.uiStyle;
    private pendingPersist = false;

    public readonly themes = APP_THEMES;
    public readonly uiStyles = APP_UI_STYLES;
    public readonly selectedTheme = signal<AppThemeName>(this.initialTheme);
    public readonly selectedUiStyle = signal<AppUiStyleName>(this.initialUiStyle);
    public readonly persistedTheme = signal<AppThemeName>(this.initialTheme);
    public readonly persistedUiStyle = signal<AppUiStyleName>(this.initialUiStyle);
    public readonly isSaving = signal(false);
    public readonly submitError = signal<string | null>(null);
    public readonly hasChanges = computed(
        () => this.selectedTheme() !== this.persistedTheme() || this.selectedUiStyle() !== this.persistedUiStyle(),
    );

    public selectTheme(theme: AppThemeName): void {
        if (this.selectedTheme() === theme) {
            return;
        }

        this.selectedTheme.set(theme);
        this.themeService.setTheme(theme);
        this.submitError.set(null);
        this.persistSelection();
    }

    public selectUiStyle(uiStyle: AppUiStyleName): void {
        if (this.selectedUiStyle() === uiStyle) {
            return;
        }

        this.selectedUiStyle.set(uiStyle);
        this.themeService.setUiStyle(uiStyle);
        this.submitError.set(null);
        this.persistSelection();
    }

    private persistSelection(): void {
        if (!this.hasChanges()) {
            return;
        }

        if (this.isSaving()) {
            this.pendingPersist = true;
            return;
        }

        const requestedTheme = this.selectedTheme();
        const requestedUiStyle = this.selectedUiStyle();
        this.isSaving.set(true);

        this.userService
            .updateAppearance(
                new UpdateUserAppearanceDto({
                    theme: requestedTheme,
                    uiStyle: requestedUiStyle,
                }),
            )
            .pipe(
                finalize(() => {
                    this.isSaving.set(false);

                    if (this.pendingPersist) {
                        this.pendingPersist = false;
                        this.persistSelection();
                    }
                }),
            )
            .subscribe({
                next: user => {
                    if (!user) {
                        this.handlePersistError();
                        return;
                    }

                    const persistedTheme = isAppThemeName(user.theme) ? user.theme : requestedTheme;
                    const persistedUiStyle = isAppUiStyleName(user.uiStyle) ? user.uiStyle : requestedUiStyle;

                    this.persistedTheme.set(persistedTheme);
                    this.persistedUiStyle.set(persistedUiStyle);

                    if (this.selectedTheme() === requestedTheme && this.selectedUiStyle() === requestedUiStyle) {
                        this.themeService.syncWithUserPreferences(persistedTheme, persistedUiStyle);
                    }
                },
                error: () => this.handlePersistError(),
            });
    }

    private revertPreview(): void {
        this.selectedTheme.set(this.persistedTheme());
        this.selectedUiStyle.set(this.persistedUiStyle());
        this.themeService.syncWithUserPreferences(this.persistedTheme(), this.persistedUiStyle());
    }

    private handlePersistError(): void {
        this.pendingPersist = false;
        this.revertPreview();
        this.submitError.set(this.translateService.instant('DASHBOARD.APPEARANCE.ERROR'));
    }
}

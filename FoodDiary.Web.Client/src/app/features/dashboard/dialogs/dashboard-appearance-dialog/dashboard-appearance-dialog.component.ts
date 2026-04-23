import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { ThemeService } from '../../../../services/theme.service';
import { UserService } from '../../../../shared/api/user.service';
import { UpdateUserDto } from '../../../../shared/models/user.data';
import { APP_THEMES, APP_UI_STYLES, AppThemeName, AppUiStyleName } from '../../../../theme/app-theme.config';

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
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
})
export class DashboardAppearanceDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<DashboardAppearanceDialogComponent, boolean>);
    private readonly themeService = inject(ThemeService);
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly data = inject<DashboardAppearanceDialogData>(FD_UI_DIALOG_DATA);
    private readonly initialTheme = this.data.theme;
    private readonly initialUiStyle = this.data.uiStyle;

    public readonly themes = APP_THEMES;
    public readonly uiStyles = APP_UI_STYLES;
    public readonly selectedTheme = signal<AppThemeName>(this.initialTheme);
    public readonly selectedUiStyle = signal<AppUiStyleName>(this.initialUiStyle);
    public readonly isSaving = signal(false);
    public readonly submitError = signal<string | null>(null);
    public readonly hasChanges = computed(
        () => this.selectedTheme() !== this.initialTheme || this.selectedUiStyle() !== this.initialUiStyle,
    );

    public selectTheme(theme: AppThemeName): void {
        if (this.selectedTheme() === theme) {
            return;
        }

        this.selectedTheme.set(theme);
        this.themeService.setTheme(theme);
    }

    public selectUiStyle(uiStyle: AppUiStyleName): void {
        if (this.selectedUiStyle() === uiStyle) {
            return;
        }

        this.selectedUiStyle.set(uiStyle);
        this.themeService.setUiStyle(uiStyle);
    }

    public close(): void {
        if (this.isSaving()) {
            return;
        }

        this.revertPreview();
        this.dialogRef.close(false);
    }

    public save(): void {
        if (this.isSaving()) {
            return;
        }

        if (!this.hasChanges()) {
            this.dialogRef.close(true);
            return;
        }

        this.isSaving.set(true);
        this.submitError.set(null);

        this.userService
            .update(
                new UpdateUserDto({
                    theme: this.selectedTheme(),
                    uiStyle: this.selectedUiStyle(),
                }),
            )
            .pipe(finalize(() => this.isSaving.set(false)))
            .subscribe({
                next: user => {
                    if (!user) {
                        this.revertPreview();
                        this.submitError.set(this.translateService.instant('DASHBOARD.APPEARANCE.ERROR'));
                        return;
                    }

                    this.themeService.syncWithUserPreferences(user.theme, user.uiStyle);
                    this.dialogRef.close(true);
                },
                error: () => {
                    this.revertPreview();
                    this.submitError.set(this.translateService.instant('DASHBOARD.APPEARANCE.ERROR'));
                },
            });
    }

    private revertPreview(): void {
        this.selectedTheme.set(this.initialTheme);
        this.selectedUiStyle.set(this.initialUiStyle);
        this.themeService.syncWithUserPreferences(this.initialTheme, this.initialUiStyle);
    }
}

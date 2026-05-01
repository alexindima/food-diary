import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiSegmentedToggleComponent, FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';

import { LocalizationService } from '../../../../services/localization.service';

@Component({
    selector: 'fd-hero',
    imports: [FdUiButtonComponent, FdUiSegmentedToggleComponent, TranslateModule],
    templateUrl: './hero.component.html',
    styleUrl: './hero.component.scss',
})
export class HeroComponent {
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly localizationService = inject(LocalizationService);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly languageOptions: FdUiSegmentedToggleOption[] = [
        { label: 'EN', value: 'en' },
        { label: 'RU', value: 'ru' },
    ];

    protected language = this.getCurrentLanguage();
    protected currentLanguage = this.language;

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.currentLanguage = event.lang;
            this.language = event.lang;
        });
    }

    protected updateLanguage(): void {
        const target = this.language;
        if (!target || target === this.currentLanguage) {
            return;
        }

        this.currentLanguage = target;
        void this.localizationService.applyLanguagePreference(target);
    }

    public goToLogin(): void {
        void this.openAuthDialog('login');
    }

    public goToRegister(): void {
        void this.openAuthDialog('register');
    }

    private async openAuthDialog(mode: 'login' | 'register'): Promise<void> {
        const { AuthDialogComponent } = await import('../../../auth/dialogs/auth-dialog/auth-dialog.component');

        this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            data: { mode },
        });
    }

    private getCurrentLanguage(): string {
        return this.translateService.currentLang || this.translateService.getDefaultLang() || 'en';
    }
}

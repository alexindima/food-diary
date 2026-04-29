import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiSegmentedToggleComponent, FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
    selector: 'fd-hero',
    imports: [FdUiHintDirective, FdUiButtonComponent, FdUiSegmentedToggleComponent, TranslateModule],
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
    protected readonly searchIntentKeys = ['FOOD_LOG', 'CALORIES', 'PLANNING', 'PROGRESS'] as const;

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

    public async goToLogin(): Promise<void> {
        this.openAuthDialog('login');
    }

    public async goToRegister(): Promise<void> {
        await this.openAuthDialog('register');
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

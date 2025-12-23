
import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiSegmentedToggleComponent, FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import { DashboardSummaryCardComponent, NutrientBar } from '../shared/dashboard-summary-card/dashboard-summary-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AuthDialogComponent } from '../auth/auth-dialog.component';

@Component({
    selector: 'fd-hero',
    imports: [FdUiButtonComponent, FdUiSegmentedToggleComponent, TranslateModule, DashboardSummaryCardComponent],
    templateUrl: './hero.component.html',
    styleUrl: './hero.component.scss'
})
export class HeroComponent {
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly languageOptions: FdUiSegmentedToggleOption[] = [
        { label: 'EN', value: 'en' },
        { label: 'RU', value: 'ru' },
    ];

    protected language = this.getCurrentLanguage();
    protected currentLanguage = this.language;

    protected readonly ringData = {
        dailyGoal: 2000,
        dailyConsumed: 1450,
        weeklyConsumed: 8200,
        weeklyGoal: 14000,
        nutrientBars: [
            { id: 'protein', label: 'Protein', labelKey: 'GENERAL.NUTRIENTS.PROTEIN', current: 110, target: 140, unit: 'g', unitKey: 'GENERAL.UNITS.G', colorStart: '#4dabff', colorEnd: '#2563eb' },
            { id: 'carbs', label: 'Carbs', labelKey: 'GENERAL.NUTRIENTS.CARB', current: 180, target: 250, unit: 'g', unitKey: 'GENERAL.UNITS.G', colorStart: '#2dd4bf', colorEnd: '#0ea5e9' },
            { id: 'fats', label: 'Fats', labelKey: 'GENERAL.NUTRIENTS.FAT', current: 45, target: 70, unit: 'g', unitKey: 'GENERAL.UNITS.G', colorStart: '#fbbf24', colorEnd: '#f97316' },
            { id: 'fiber', label: 'Fiber', labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER', current: 18, target: 30, unit: 'g', unitKey: 'GENERAL.UNITS.G', colorStart: '#fb7185', colorEnd: '#ec4899' },
        ] satisfies NutrientBar[],
    };

    public constructor() {
        this.translateService.onLangChange
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(event => {
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
        this.translateService.use(target).subscribe();
    }

    public async goToLogin(): Promise<void> {
        this.openAuthDialog('login');
    }

    public async goToRegister(): Promise<void> {
        this.openAuthDialog('register');
    }

    private openAuthDialog(mode: 'login' | 'register'): void {
        this.fdDialogService.open(AuthDialogComponent, {
            size: 'md',
            data: { mode },
        });
    }

    private getCurrentLanguage(): string {
        return this.translateService.currentLang || this.translateService.getDefaultLang() || 'en';
    }
}

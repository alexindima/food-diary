
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { DashboardSummaryCardComponent, NutrientBar } from '../shared/dashboard-summary-card/dashboard-summary-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AuthDialogComponent } from '../auth/auth-dialog.component';

@Component({
    selector: 'fd-hero',
    imports: [FdUiButtonComponent, TranslateModule, DashboardSummaryCardComponent],
    templateUrl: './hero.component.html',
    styleUrl: './hero.component.scss'
})
export class HeroComponent {
    private readonly fdDialogService = inject(FdUiDialogService);

    protected readonly ringData = {
        dailyGoal: 2000,
        dailyConsumed: 1450,
        weeklyConsumed: 8200,
        weeklyGoal: 14000,
        nutrientBars: [
            { id: 'protein', label: 'Protein', current: 110, target: 140, unit: 'g', colorStart: '#4dabff', colorEnd: '#2563eb' },
            { id: 'carbs', label: 'Carbs', current: 180, target: 250, unit: 'g', colorStart: '#2dd4bf', colorEnd: '#0ea5e9' },
            { id: 'fats', label: 'Fats', current: 45, target: 70, unit: 'g', colorStart: '#fbbf24', colorEnd: '#f97316' },
            { id: 'fiber', label: 'Fiber', current: 18, target: 30, unit: 'g', colorStart: '#fb7185', colorEnd: '#ec4899' },
        ] satisfies NutrientBar[],
    };

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
}

import { Component, OnInit, inject } from '@angular/core';
import { HeroComponent } from '../hero/hero.component';
import { FeaturesComponent } from '../features/features.component';
import { DashboardComponent } from '../dashboard/dashboard.component';
import { AuthService } from '../../services/auth.service';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AuthDialogComponent } from '../auth/auth-dialog.component';
import { ActivatedRoute } from '@angular/router';
import { MealsPreviewComponent, MealPreviewEntry } from '../shared/meals-preview/meals-preview.component';
import { Consumption } from '../../types/consumption.data';
import { StatisticsSummaryComponent, SummaryMetrics } from '../shared/statistics-summary/statistics-summary.component';
import { StatisticsNutritionComponent } from '../shared/statistics-nutrition/statistics-nutrition.component';
import { StatisticsBodyComponent } from '../shared/statistics-body/statistics-body.component';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

@Component({
    selector: 'fd-main',
    imports: [
        HeroComponent,
        FeaturesComponent,
        DashboardComponent,
        FdUiButtonComponent,
        TranslateModule,
        MealsPreviewComponent,
        StatisticsSummaryComponent,
        StatisticsNutritionComponent,
        StatisticsBodyComponent,
    ],
    templateUrl: './main.component.html',
    styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
    private readonly authService = inject(AuthService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly route = inject(ActivatedRoute);

    public isAuthenticated = this.authService.isAuthenticated;
    public guestMealEntries: MealPreviewEntry[] = this.buildGuestMeals();
    public guestSummary = this.buildGuestSummary();
    public guestSummarySparkline = this.buildSparkline();
    public guestMacroSparkline = this.buildMacroSparkline();
    public guestSummarySparklineOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: { enabled: false } },
        elements: { line: { borderJoinStyle: 'round' } },
        scales: { x: { display: false }, y: { display: false } },
    };
    public nutritionTabs: FdUiTab[] = [
        { value: 'calories', labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES' },
        { value: 'macros', labelKey: 'STATISTICS.NUTRITION_TABS.MACROS' },
        { value: 'distribution', labelKey: 'STATISTICS.NUTRITION_TABS.DISTRIBUTION' },
    ];
    public bodyTabs: FdUiTab[] = [
        { value: 'weight', labelKey: 'STATISTICS.BODY_TABS.WEIGHT' },
        { value: 'bmi', labelKey: 'STATISTICS.BODY_TABS.BMI' },
    ];
    public selectedNutritionTab: string = 'calories';
    public selectedBodyTab: string = 'weight';
    public guestCaloriesLineData = this.buildLineData([1200, 1450, 1600, 1800, 1750, 1900, 2000]);
    public guestNutrientsLineData = this.buildMultiLineData();
    public guestPieData = this.buildPieData();
    public guestRadarData = this.buildRadarData();
    public guestBarData = this.buildBarData();
    public guestBodyData = this.buildBodyData();
    public lineOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
            y: { beginAtZero: true, ticks: { color: '#475569' } },
            x: { ticks: { color: '#475569', maxRotation: 0 } },
        },
    };
    public lineOptionsWithLegend: ChartConfiguration['options'] = {
        ...this.lineOptions,
        plugins: { legend: { position: 'bottom' } },
    };
    public pieOptions: ChartOptions<'pie'> = {};
    public radarOptions: ChartOptions<'radar'> = { scales: { r: { beginAtZero: true } } };
    public barOptions: ChartOptions<'bar'> = { plugins: { legend: { display: false } }, responsive: true, scales: { y: { beginAtZero: true } } };

    public openAuthDialog(mode: 'login' | 'register'): void {
        this.fdDialogService.open(AuthDialogComponent, {
            size: 'md',
            data: { mode },
        });
    }

    public ngOnInit(): void {
        const path = this.route.snapshot.routeConfig?.path ?? '';
        if (path.startsWith('auth')) {
            const modeParam = this.route.snapshot.params['mode'];
            const mode: 'login' | 'register' = modeParam === 'register' ? 'register' : 'login';
            this.openAuthDialog(mode);
        }
    }

    private buildGuestMeals(): MealPreviewEntry[] {
        const lunch: Consumption = {
            id: 'guest-lunch',
            date: new Date().toISOString(),
            mealType: 'LUNCH',
            totalCalories: 430,
            totalProteins: 24,
            totalFats: 12,
            totalCarbs: 52,
            totalFiber: 7,
            isNutritionAutoCalculated: true,
            manualCalories: null,
            manualProteins: null,
            manualFats: null,
            manualCarbs: null,
            manualFiber: null,
            preMealSatietyLevel: null,
            postMealSatietyLevel: null,
            comment: 'Салат с киноа, курицей и авокадо',
            imageUrl: 'assets/images/stubs/meals/salad.svg',
            imageAssetId: null,
            items: [],
        };

        return [
            { slot: 'BREAKFAST', icon: 'wb_sunny', labelKey: 'MEAL_CARD.MEAL_TYPES.BREAKFAST', meal: null },
            { slot: 'LUNCH', icon: 'lunch_dining', labelKey: 'MEAL_CARD.MEAL_TYPES.LUNCH', meal: lunch },
            { slot: 'DINNER', icon: 'nights_stay', labelKey: 'MEAL_CARD.MEAL_TYPES.DINNER', meal: null },
        ];
    }

    private buildGuestSummary(): SummaryMetrics {
        return {
            totalCalories: 1820,
            averageCard: { consumption: 1740, steps: 6400, burned: 215 },
            macros: [
                { key: 'proteins', labelKey: 'GENERAL.NUTRIENTS.PROTEIN', value: 110, color: '#36A2EB' },
                { key: 'fats', labelKey: 'GENERAL.NUTRIENTS.FAT', value: 45, color: '#FFCE56' },
                { key: 'carbs', labelKey: 'GENERAL.NUTRIENTS.CARB', value: 180, color: '#4BC0C0' },
                { key: 'fiber', labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER', value: 18, color: '#8E44AD' },
            ],
        };
    }

    private buildSparkline(): ChartConfiguration<'line'>['data'] {
        return {
            labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            datasets: [
                {
                    data: [1600, 1700, 1800, 1500, 1900, 1750, 1820],
                    borderColor: '#2563eb',
                    backgroundColor: 'rgba(37, 99, 235, 0.18)',
                    tension: 0.35,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 0,
                },
            ],
        };
    }

    private buildMacroSparkline(): Record<string, ChartConfiguration<'line'>['data']> {
        const template = (color: string) => ({
            labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            datasets: [
                {
                    data: [12, 14, 16, 11, 18, 15, 17],
                    borderColor: color,
                    backgroundColor: `${color}29`,
                    tension: 0.35,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 0,
                },
            ],
        });
        return {
            proteins: template('#36A2EB'),
            fats: template('#FFCE56'),
            carbs: template('#4BC0C0'),
            fiber: template('#8E44AD'),
        };
    }

    private buildLineData(values: number[]): ChartConfiguration<'line'>['data'] {
        return {
            labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            datasets: [
                {
                    data: values,
                    borderColor: '#2563eb',
                    backgroundColor: 'rgba(37, 99, 235, 0.16)',
                    tension: 0.35,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 0,
                },
            ],
        };
    }

    private buildMultiLineData(): ChartConfiguration<'line'>['data'] {
        return {
            labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            datasets: [
                { data: [110, 120, 130, 125, 140, 135, 138], label: 'Proteins', borderColor: '#36A2EB', tension: 0.35, borderWidth: 2 },
                { data: [40, 42, 45, 44, 46, 48, 47], label: 'Fats', borderColor: '#FFCE56', tension: 0.35, borderWidth: 2 },
                { data: [160, 170, 180, 175, 185, 190, 195], label: 'Carbs', borderColor: '#4BC0C0', tension: 0.35, borderWidth: 2 },
            ],
        };
    }

    private buildPieData(): ChartConfiguration<'pie'>['data'] {
        return {
            labels: ['Proteins', 'Fats', 'Carbs'],
            datasets: [
                {
                    data: [24, 18, 58],
                    backgroundColor: ['#36A2EB', '#FFCE56', '#4BC0C0'],
                },
            ],
        };
    }

    private buildRadarData(): ChartConfiguration<'radar'>['data'] {
        return {
            labels: ['Proteins', 'Fats', 'Carbs', 'Fiber'],
            datasets: [
                {
                    data: [70, 55, 80, 60],
                    label: 'Macros',
                    backgroundColor: 'rgba(37, 99, 235, 0.12)',
                    borderColor: '#2563eb',
                    pointBackgroundColor: '#2563eb',
                },
            ],
        };
    }

    private buildBarData(): ChartConfiguration<'bar'>['data'] {
        return {
            labels: ['P', 'F', 'C'],
            datasets: [
                {
                    data: [110, 45, 180],
                    backgroundColor: ['#36A2EB', '#FFCE56', '#4BC0C0'],
                },
            ],
        };
    }

    private buildBodyData(): ChartConfiguration<'line'>['data'] {
        return {
            labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            datasets: [
                {
                    data: [72.4, 72.1, 71.9, 72.0, 71.8, 71.6, 71.5],
                    borderColor: '#2563eb',
                    backgroundColor: 'rgba(37, 99, 235, 0.16)',
                    tension: 0.3,
                    borderWidth: 2,
                    pointRadius: 3,
                    fill: true,
                },
            ],
        };
    }
}

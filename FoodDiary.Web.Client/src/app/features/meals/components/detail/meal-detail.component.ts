import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Meal } from '../../models/meal.data';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { CHART_COLORS } from '../../../../constants/chart-colors';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, TooltipItem } from 'chart.js';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'fd-meal-detail',
    standalone: true,
    templateUrl: './meal-detail.component.html',
    styleUrls: ['./meal-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [DatePipe],
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        FdUiAccentSurfaceComponent,
        BaseChartDirective,
        MatIconModule,
    ],
})
export class MealDetailComponent {
    private readonly dialogRef = inject(FdUiDialogRef<MealDetailComponent>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly datePipe = inject(DatePipe);
    private readonly translate = inject(TranslateService);
    private readonly favoriteMealService = inject(FavoriteMealService);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    private favoriteMealId: string | null = null;

    public readonly consumption: Meal;
    public readonly calories: number;
    public readonly proteins: number;
    public readonly fats: number;
    public readonly carbs: number;
    public readonly fiber: number;
    public readonly alcohol: number;
    public readonly qualityScore: number;
    public readonly qualityGrade: string;
    public readonly itemsCount: number;
    public readonly formattedDate: string | null;
    public readonly mealTypeLabel: string | null;
    public readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'CONSUMPTION_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'CONSUMPTION_DETAIL.TABS.NUTRIENTS' },
    ];
    public activeTab: 'summary' | 'nutrients' = 'summary';
    public readonly pieChartData: ChartData<'pie', number[], string>;
    public readonly barChartData: ChartData<'bar', number[], string>;
    public readonly pieChartOptions: ChartOptions<'pie'>;
    public readonly barChartOptions: ChartOptions<'bar'>;
    public readonly chartSize = 200;
    public readonly macroBlocks: {
        labelKey: string;
        value: number;
        unitKey: string;
        color: string;
    }[];

    public constructor() {
        const data = inject<Meal>(FD_UI_DIALOG_DATA);

        this.consumption = data;
        this.calories = data.totalCalories ?? 0;
        this.proteins = data.totalProteins ?? 0;
        this.fats = data.totalFats ?? 0;
        this.carbs = data.totalCarbs ?? 0;
        this.fiber = data.totalFiber ?? 0;
        this.alcohol = data.totalAlcohol ?? 0;
        this.qualityScore = Math.round(Math.min(100, Math.max(0, data.qualityScore ?? 50)));
        this.qualityGrade = data.qualityGrade ?? 'yellow';
        this.itemsCount = data.items.length;
        this.formattedDate = this.datePipe.transform(this.consumption.date, 'dd.MM.yyyy, HH:mm');
        this.mealTypeLabel = data.mealType ? this.translate.instant(`MEAL_TYPES.${data.mealType}`) : null;

        const labels = [
            this.translate.instant('NUTRIENTS.PROTEINS'),
            this.translate.instant('NUTRIENTS.FATS'),
            this.translate.instant('NUTRIENTS.CARBS'),
        ];
        const datasetValues = [this.proteins, this.fats, this.carbs];
        const colors = [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs];
        this.pieChartData = {
            labels,
            datasets: [
                {
                    data: datasetValues,
                    backgroundColor: colors,
                },
            ],
        };
        this.barChartData = {
            labels,
            datasets: [
                {
                    data: datasetValues,
                    backgroundColor: colors,
                },
            ],
        };
        const tooltipLabel = (label: string, value: number): string =>
            `${label}: ${value.toFixed(2)} ${this.translate.instant('GENERAL.UNITS.G')}`;
        this.pieChartOptions = {
            responsive: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: (ctx: TooltipItem<'pie'>): string => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
                    },
                },
            },
        };
        this.barChartOptions = {
            responsive: true,
            scales: {
                x: { display: false },
                y: { beginAtZero: true },
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: (ctx: TooltipItem<'bar'>): string => tooltipLabel(ctx.label ?? '', Number(ctx.raw) || 0),
                    },
                },
            },
        };
        this.macroBlocks = [
            {
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                value: this.proteins,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.proteins,
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                value: this.fats,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fats,
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                value: this.carbs,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.carbs,
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FIBER',
                value: this.fiber,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fiber,
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.ALCOHOL',
                value: this.alcohol,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.alcohol,
            },
        ];
        this.favoriteMealService.isFavorite(this.consumption.id).subscribe(isFav => this.isFavorite.set(isFav));
    }

    public toggleFavorite(): void {
        if (this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            if (this.favoriteMealId) {
                this.favoriteMealService.remove(this.favoriteMealId).subscribe({
                    next: () => {
                        this.isFavorite.set(false);
                        this.favoriteMealId = null;
                        this.isFavoriteLoading.set(false);
                    },
                    error: () => this.isFavoriteLoading.set(false),
                });
            } else {
                this.favoriteMealService.getAll().subscribe({
                    next: favorites => {
                        const match = favorites.find(f => f.mealId === this.consumption.id);
                        if (match) {
                            this.favoriteMealService.remove(match.id).subscribe({
                                next: () => {
                                    this.isFavorite.set(false);
                                    this.favoriteMealId = null;
                                    this.isFavoriteLoading.set(false);
                                },
                                error: () => this.isFavoriteLoading.set(false),
                            });
                        } else {
                            this.isFavorite.set(false);
                            this.isFavoriteLoading.set(false);
                        }
                    },
                    error: () => this.isFavoriteLoading.set(false),
                });
            }
        } else {
            this.favoriteMealService.add(this.consumption.id).subscribe({
                next: favorite => {
                    this.isFavorite.set(true);
                    this.favoriteMealId = favorite.id;
                    this.isFavoriteLoading.set(false);
                },
                error: () => this.isFavoriteLoading.set(false),
            });
        }
    }

    public onTabChange(tab: string): void {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    }

    public onRepeat(): void {
        const repeatResult = new MealDetailActionResult(this.consumption.id, 'Repeat');
        this.dialogRef.close(repeatResult);
    }

    public onEdit(): void {
        const editResult = new MealDetailActionResult(this.consumption.id, 'Edit');
        this.dialogRef.close(editResult);
    }

    public onDelete(): void {
        const formattedDate = this.datePipe.transform(this.consumption.date, 'dd.MM.yyyy');
        const data: ConfirmDeleteDialogData = {
            title: this.translate.instant('CONFIRM_DELETE.TITLE', {
                type: this.translate.instant('CONSUMPTION_DETAIL.ENTITY_NAME'),
            }),
            message: this.translate.instant('CONFIRM_DELETE.MESSAGE', { name: formattedDate ?? '' }),
            name: formattedDate ?? '',
            entityType: this.translate.instant('CONSUMPTION_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translate.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translate.instant('CONFIRM_DELETE.CANCEL'),
        };

        this.fdDialogService
            .open(ConfirmDeleteDialogComponent, { data, size: 'sm' })
            .afterClosed()
            .subscribe(confirm => {
                if (confirm) {
                    const deleteResult = new MealDetailActionResult(this.consumption.id, 'Delete');
                    this.dialogRef.close(deleteResult);
                }
            });
    }
}

export class MealDetailActionResult {
    public constructor(
        public id: string,
        public action: MealDetailAction,
    ) {}
}

export type MealDetailAction = 'Edit' | 'Delete' | 'Repeat';

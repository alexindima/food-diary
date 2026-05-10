import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogHeaderDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-header.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { DEFAULT_HUNGER_LEVELS, DEFAULT_SATIETY_LEVELS } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import {
    type NutritionControlNames,
    NutritionEditorComponent,
    type NutritionMacroState,
} from '../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { CHART_COLORS } from '../../../../constants/chart-colors';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import type { ConsumptionAiItem, Meal } from '../../models/meal.data';

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
        FdUiDialogHeaderDirective,
        FdUiButtonComponent,
        FdUiTabsComponent,
        FdUiAccentSurfaceComponent,
        NutritionEditorComponent,
        DecimalPipe,
    ],
})
export class MealDetailComponent {
    private readonly dialogRef = inject(FdUiDialogRef<MealDetailComponent, MealDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly datePipe = inject(DatePipe);
    private readonly translate = inject(TranslateService);
    private readonly favoriteMealService = inject(FavoriteMealService);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    public readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'CONSUMPTION_DETAIL.REMOVE_FAVORITE' : 'CONSUMPTION_DETAIL.ADD_FAVORITE',
    );
    private initialFavoriteState = false;
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
    public readonly qualityHintKey: string;
    public readonly itemsCount: number;
    public readonly formattedDate: string | null;
    public readonly mealTypeLabel: string | null;
    public readonly preMealSatietyMeta: MealSatietyMeta;
    public readonly postMealSatietyMeta: MealSatietyMeta;
    public readonly tabs: FdUiTab[] = [
        { value: 'summary', labelKey: 'CONSUMPTION_DETAIL.TABS.SUMMARY' },
        { value: 'nutrients', labelKey: 'CONSUMPTION_DETAIL.TABS.NUTRIENTS' },
    ];
    public activeTab: 'summary' | 'nutrients' = 'summary';
    public readonly itemPreviewMaxItems = 2;
    public readonly isItemPreviewExpanded = signal(false);
    public readonly macroBlocks: {
        labelKey: string;
        value: number;
        unitKey: string;
        color: string;
        percent: number;
    }[];
    public readonly macroSummaryBlocks = computed(() => this.macroBlocks.slice(0, 3));
    public readonly itemPreview: MealDetailItemPreview[];
    public readonly visibleItemPreview = computed<MealDetailItemPreview[]>(() =>
        this.isItemPreviewExpanded() ? this.itemPreview : this.itemPreview.slice(0, this.itemPreviewMaxItems),
    );
    public readonly hiddenItemPreviewCount = computed(() => Math.max(0, this.itemPreview.length - this.itemPreviewMaxItems));
    public readonly nutritionControlNames: NutritionControlNames = {
        calories: 'calories',
        proteins: 'proteins',
        fats: 'fats',
        carbs: 'carbs',
        fiber: 'fiber',
        alcohol: 'alcohol',
    };
    public readonly nutritionForm: FormGroup;
    public readonly macroBarState: NutritionMacroState;

    public constructor() {
        const data = inject<Meal>(FD_UI_DIALOG_DATA);

        this.consumption = data;
        this.initialFavoriteState = this.consumption.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteMealId = this.consumption.favoriteMealId ?? null;
        this.calories = data.totalCalories;
        this.proteins = data.totalProteins;
        this.fats = data.totalFats;
        this.carbs = data.totalCarbs;
        this.fiber = data.totalFiber;
        this.alcohol = data.totalAlcohol;
        this.qualityScore = Math.round(Math.min(100, Math.max(0, data.qualityScore ?? 50)));
        this.qualityGrade = data.qualityGrade ?? 'yellow';
        this.qualityHintKey = `QUALITY.${this.qualityGrade.toUpperCase()}`;
        this.itemsCount = this.getTotalItemsCount(data);
        this.formattedDate = this.datePipe.transform(this.consumption.date, 'dd.MM.yyyy, HH:mm');
        this.mealTypeLabel = data.mealType ? this.translate.instant(`MEAL_TYPES.${data.mealType}`) : null;
        this.preMealSatietyMeta = this.buildSatietyMeta('before', data.preMealSatietyLevel);
        this.postMealSatietyMeta = this.buildSatietyMeta('after', data.postMealSatietyLevel);
        this.itemPreview = this.buildItemPreview();

        const datasetValues = [this.proteins, this.fats, this.carbs];
        this.nutritionForm = this.buildNutritionForm({
            calories: this.calories,
            proteins: this.proteins,
            fats: this.fats,
            carbs: this.carbs,
            fiber: this.fiber,
            alcohol: this.alcohol,
        });
        this.macroBarState = this.buildMacroBarState(datasetValues);
        this.macroBlocks = [
            {
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                value: this.proteins,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.proteins,
                percent: this.resolveMacroPercent(this.proteins, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                value: this.fats,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fats,
                percent: this.resolveMacroPercent(this.fats, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                value: this.carbs,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.carbs,
                percent: this.resolveMacroPercent(this.carbs, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.FIBER',
                value: this.fiber,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.fiber,
                percent: this.resolveMacroPercent(this.fiber, datasetValues),
            },
            {
                labelKey: 'GENERAL.NUTRIENTS.ALCOHOL',
                value: this.alcohol,
                unitKey: 'GENERAL.UNITS.G',
                color: CHART_COLORS.alcohol,
                percent: this.resolveMacroPercent(this.alcohol, datasetValues),
            },
        ];
        this.favoriteMealService.isFavorite(this.consumption.id).subscribe(isFav => {
            this.initialFavoriteState = isFav;
            this.isFavorite.set(isFav);
        });
    }

    private buildNutritionForm(values: {
        calories: number;
        proteins: number;
        fats: number;
        carbs: number;
        fiber: number;
        alcohol: number;
    }): FormGroup {
        return new FormGroup({
            calories: new FormControl(values.calories),
            proteins: new FormControl(values.proteins),
            fats: new FormControl(values.fats),
            carbs: new FormControl(values.carbs),
            fiber: new FormControl(values.fiber),
            alcohol: new FormControl(values.alcohol),
        });
    }

    private buildMacroBarState(values: number[]): NutritionMacroState {
        const total = values.reduce((sum, value) => sum + value, 0);

        return {
            isEmpty: total <= 0,
            segments: [
                { key: 'proteins', percent: total > 0 ? (values[0] / total) * 100 : 0 },
                { key: 'fats', percent: total > 0 ? (values[1] / total) * 100 : 0 },
                { key: 'carbs', percent: total > 0 ? (values[2] / total) * 100 : 0 },
            ],
        };
    }

    private resolveMacroPercent(value: number, values: number[]): number {
        const max = Math.max(...values, value, 1);
        return Math.max(4, Math.round((value / max) * 100));
    }

    private buildItemPreview(): MealDetailItemPreview[] {
        const manualItems = this.consumption.items.map(item => ({
            name: item.product?.name ?? item.recipe?.name ?? this.translate.instant('CONSUMPTION_DETAIL.SUMMARY.UNKNOWN_ITEM'),
            amount: item.amount,
            unitKey: item.product?.baseUnit ? `GENERAL.UNITS.${item.product.baseUnit}` : 'CONSUMPTION_DETAIL.SERVINGS',
            unitText: null,
        }));
        const aiItems = (this.consumption.aiSessions ?? []).flatMap(session =>
            session.items.map(item => ({
                name: this.getAiItemName(item),
                amount: item.amount,
                unitKey: this.getAiItemUnitKey(item.unit),
                unitText: this.getAiItemUnitKey(item.unit) ? null : item.unit,
            })),
        );

        return [...manualItems, ...aiItems];
    }

    private getTotalItemsCount(consumption: Meal): number {
        const aiItemsCount = (consumption.aiSessions ?? []).reduce((total, session) => total + session.items.length, 0);
        return consumption.items.length + aiItemsCount;
    }

    private getAiItemName(item: ConsumptionAiItem): string {
        return item.nameLocal?.trim() || item.nameEn.trim() || this.translate.instant('CONSUMPTION_DETAIL.SUMMARY.UNKNOWN_ITEM');
    }

    private getAiItemUnitKey(unit: string): string | null {
        const normalized = unit.trim().toLowerCase();
        const unitMap: Record<string, string> = {
            g: 'GENERAL.UNITS.G',
            gram: 'GENERAL.UNITS.G',
            grams: 'GENERAL.UNITS.G',
            gr: 'GENERAL.UNITS.G',
            ml: 'GENERAL.UNITS.ML',
            l: 'GENERAL.UNITS.ML',
            pcs: 'GENERAL.UNITS.PCS',
            pc: 'GENERAL.UNITS.PCS',
            piece: 'GENERAL.UNITS.PCS',
            pieces: 'GENERAL.UNITS.PCS',
        };

        return unitMap[normalized] ?? null;
    }

    private buildSatietyMeta(kind: 'before' | 'after', value: number | null | undefined): MealSatietyMeta {
        if (typeof value !== 'number') {
            return {
                emoji: '😐',
                title: this.translate.instant('HUNGER_SCALE.LEVEL_0.TITLE'),
                description: this.translate.instant('HUNGER_SCALE.LEVEL_0.DESCRIPTION'),
            };
        }

        const normalizedValue = Math.min(5, Math.max(1, Math.round(value)));
        const levels = kind === 'before' ? DEFAULT_HUNGER_LEVELS : DEFAULT_SATIETY_LEVELS;
        const level = levels.find(item => item.value === normalizedValue);

        return {
            emoji: level?.emoji ?? '😐',
            title: this.translate.instant(level?.titleKey ?? 'HUNGER_SCALE.LEVEL_0.TITLE'),
            description: this.translate.instant(level?.descriptionKey ?? 'HUNGER_SCALE.LEVEL_0.DESCRIPTION'),
        };
    }

    public close(): void {
        if (this.hasFavoriteChanged()) {
            this.dialogRef.close(new MealDetailActionResult(this.consumption.id, 'FavoriteChanged', true));
            return;
        }

        this.dialogRef.close();
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
                    error: () => {
                        this.isFavoriteLoading.set(false);
                    },
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
                                error: () => {
                                    this.isFavoriteLoading.set(false);
                                },
                            });
                        } else {
                            this.isFavorite.set(false);
                            this.isFavoriteLoading.set(false);
                        }
                    },
                    error: () => {
                        this.isFavoriteLoading.set(false);
                    },
                });
            }
        } else {
            this.favoriteMealService.add(this.consumption.id).subscribe({
                next: favorite => {
                    this.isFavorite.set(true);
                    this.favoriteMealId = favorite.id;
                    this.isFavoriteLoading.set(false);
                },
                error: () => {
                    this.isFavoriteLoading.set(false);
                },
            });
        }
    }

    public onTabChange(tab: string): void {
        if (tab === 'summary' || tab === 'nutrients') {
            this.activeTab = tab;
        }
    }

    public toggleItemPreviewExpanded(): void {
        this.isItemPreviewExpanded.update(isExpanded => !isExpanded);
    }

    public onRepeat(): void {
        const repeatResult = new MealDetailActionResult(this.consumption.id, 'Repeat', this.hasFavoriteChanged());
        this.dialogRef.close(repeatResult);
    }

    public onEdit(): void {
        const editResult = new MealDetailActionResult(this.consumption.id, 'Edit', this.hasFavoriteChanged());
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
                    const deleteResult = new MealDetailActionResult(this.consumption.id, 'Delete', this.hasFavoriteChanged());
                    this.dialogRef.close(deleteResult);
                }
            });
    }

    private hasFavoriteChanged(): boolean {
        return this.initialFavoriteState !== this.isFavorite();
    }
}

export class MealDetailActionResult {
    public constructor(
        public id: string,
        public action: MealDetailAction,
        public favoriteChanged = false,
    ) {}
}

export type MealDetailAction = 'Edit' | 'Delete' | 'Repeat' | 'FavoriteChanged';

type MealSatietyMeta = {
    emoji: string;
    title: string;
    description: string;
};

type MealDetailItemPreview = {
    name: string;
    amount: number;
    unitKey: string | null;
    unitText: string | null;
};

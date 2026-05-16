import { moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective } from 'fd-ui-kit';

import { recalculateEditedAiNutrition } from '../../../../shared/lib/ai-nutrition-edit.utils';
import {
    buildAiEditableItems,
    createEmptyAiEditableItem,
    normalizeAiEditableItems,
    requiresAiNutritionRecalculation,
    resolveAiPhotoUnitKey,
    updateAiEditableItem,
} from '../../../../shared/lib/ai-photo-edit.utils';
import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../../shared/lib/satiety-level.utils';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import type { AiInputBarMealDetails } from '../ai-input-bar.types';
import { AiPhotoDetailsPanelComponent } from './ai-photo-details-panel/ai-photo-details-panel.component';
import { AiPhotoEditListComponent } from './ai-photo-edit-list/ai-photo-edit-list.component';
import { AiPhotoNutritionSummaryComponent } from './ai-photo-nutrition-summary/ai-photo-nutrition-summary.component';
import { AiPhotoPreviewComponent } from './ai-photo-preview/ai-photo-preview.component';
import { AiPhotoResultActionsComponent } from './ai-photo-result-actions/ai-photo-result-actions.component';
import type {
    AiDetailsToggleView,
    AiEditActionView,
    AiEditItemDrop,
    AiEditItemUpdate,
    AiEditUnitOption,
    AiNutritionSummaryItem,
    AiPhotoEditApplied,
    AiResultRow,
    EditableAiItem,
} from './ai-photo-result-lib/ai-photo-result.types';
import { AiPhotoResultRowsComponent } from './ai-photo-result-rows/ai-photo-result-rows.component';

const TIME_PAD_LENGTH = 2;
const NUTRITION_FRACTION_THRESHOLD = 0.01;

@Component({
    selector: 'fd-ai-photo-result',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        AiPhotoPreviewComponent,
        AiPhotoResultActionsComponent,
        AiPhotoEditListComponent,
        AiPhotoResultRowsComponent,
        AiPhotoNutritionSummaryComponent,
        AiPhotoDetailsPanelComponent,
    ],
    templateUrl: './ai-photo-result.component.html',
    styleUrls: ['./ai-photo-result.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiPhotoResultComponent {
    private readonly translateService = inject(TranslateService);
    private readonly unitOptions = ['g', 'ml', 'pcs'] as const;

    public readonly titleKey = input<string>('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.RESULTS_TITLE');
    public readonly imageUrl = input<string | null>(null);
    public readonly sourceText = input<string | null>(null);
    public readonly sourceTextLabelKey = input<string>('AI_INPUT_BAR.TEXT_PREVIEW_LABEL');
    public readonly submitLabelKey = input.required<string>();
    public readonly showDetails = input.required<boolean>();
    public readonly results = input.required<FoodVisionItem[]>();
    public readonly isAnalyzing = input.required<boolean>();
    public readonly isNutritionLoading = input.required<boolean>();
    public readonly nutrition = input.required<FoodNutritionResponse | null>();
    public readonly errorKey = input.required<string | null>();
    public readonly nutritionErrorKey = input.required<string | null>();
    public readonly isProcessing = input.required<boolean>();

    public readonly dismissed = output();
    public readonly addToMeal = output<AiInputBarMealDetails>();
    public readonly editApplied = output<AiPhotoEditApplied>();
    public readonly reanalyzeRequested = output();

    public readonly isEditing = signal(false);
    public readonly isDetailsExpanded = signal(false);
    public readonly detailsDate = signal(this.getDateInputValue(new Date()));
    public readonly detailsTime = signal(this.getTimeInputValue(new Date()));
    public readonly detailsComment = signal('');
    public readonly preMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly postMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    public readonly editItems = signal<EditableAiItem[]>([]);
    public readonly resultRows = computed<AiResultRow[]>(() =>
        this.results().map(item => ({
            key: item.nameEn,
            displayName: this.resolveDisplayName(item),
            amountLabel: this.resolveAmountLabel(item),
        })),
    );
    public readonly editActionView = computed<AiEditActionView>(() =>
        this.isEditing()
            ? {
                  variant: 'primary',
                  fill: 'solid',
                  labelKey: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.SAVE',
              }
            : {
                  variant: 'secondary',
                  fill: 'outline',
                  labelKey: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON',
              },
    );
    public readonly detailsToggleView = computed<AiDetailsToggleView>(() =>
        this.isDetailsExpanded()
            ? {
                  icon: 'expand_less',
                  labelKey: 'MEAL_DETAILS.HIDE',
              }
            : {
                  icon: 'expand_more',
                  labelKey: 'MEAL_DETAILS.ADD',
              },
    );
    public readonly nutritionSummary = computed<AiNutritionSummaryItem[]>(() => {
        const nutrition = this.nutrition();
        if (nutrition === null) {
            return [];
        }

        return [
            { labelKey: 'GENERAL.NUTRIENTS.CALORIES', value: this.resolveMacroLabel(nutrition.calories, 'GENERAL.UNITS.KCAL') },
            { labelKey: 'GENERAL.NUTRIENTS.PROTEIN', value: this.resolveMacroLabel(nutrition.protein, 'GENERAL.UNITS.G') },
            { labelKey: 'GENERAL.NUTRIENTS.FAT', value: this.resolveMacroLabel(nutrition.fat, 'GENERAL.UNITS.G') },
            { labelKey: 'GENERAL.NUTRIENTS.CARB', value: this.resolveMacroLabel(nutrition.carbs, 'GENERAL.UNITS.G') },
            { labelKey: 'GENERAL.NUTRIENTS.FIBER', value: this.resolveMacroLabel(nutrition.fiber, 'GENERAL.UNITS.G') },
            { labelKey: 'GENERAL.NUTRIENTS.ALCOHOL', value: this.resolveMacroLabel(nutrition.alcohol, 'GENERAL.UNITS.G') },
        ];
    });
    public readonly editUnitOptions = computed<AiEditUnitOption[]>(() =>
        this.unitOptions.map(unit => ({
            value: unit,
            label: this.resolveUnitLabel(unit),
        })),
    );
    private readonly sourceItems = signal<EditableAiItem[]>([]);

    private resolveDisplayName(item: FoodVisionItem): string {
        const rawName = item.nameLocal?.trim() ?? item.nameEn;
        return this.capitalizeLabel(rawName);
    }

    private resolveAmountLabel(item: FoodVisionItem): string {
        const amount = item.amount;
        const unitKey = resolveAiPhotoUnitKey(item.unit);
        const unitLabel = unitKey !== null ? this.translateService.instant(unitKey) : item.unit;
        return unitLabel.length > 0 ? `${amount} ${unitLabel}`.trim() : `${amount}`.trim();
    }

    private resolveMacroLabel(value: number, unitKey: string): string {
        const locale = this.translateService.getCurrentLang();
        const hasFraction = Math.abs(value % 1) > NUTRITION_FRACTION_THRESHOLD;
        const formatter = new Intl.NumberFormat(locale, {
            maximumFractionDigits: hasFraction ? 1 : 0,
            minimumFractionDigits: hasFraction ? 1 : 0,
        });
        const unitLabel = this.translateService.instant(unitKey);
        return `${formatter.format(value)} ${unitLabel}`.trim();
    }

    private resolveUnitLabel(unit: string): string {
        const unitKey = resolveAiPhotoUnitKey(unit);
        return unitKey !== null ? this.translateService.instant(unitKey) : unit;
    }

    public startEditing(): void {
        const editable = buildAiEditableItems(this.results(), this.nutrition(), () => this.createEditId());
        this.editItems.set(editable);
        this.sourceItems.set(editable.map(item => ({ ...item })));
        this.isEditing.set(true);
    }

    public applyEditing(): void {
        const edited = this.editItems().filter(item => item.name.trim().length > 0 && item.amount > 0);
        const normalized: FoodVisionItem[] = normalizeAiEditableItems(edited);
        const requiresAi = requiresAiNutritionRecalculation(this.sourceItems(), edited);
        this.isEditing.set(false);

        if (requiresAi) {
            this.editApplied.emit({ items: normalized, nutrition: null });
            return;
        }

        this.editApplied.emit({
            items: normalized,
            nutrition: recalculateEditedAiNutrition(this.nutrition(), this.sourceItems(), edited),
        });
    }

    public handleEditAction(): void {
        if (this.isEditing()) {
            this.applyEditing();
            return;
        }

        this.startEditing();
    }

    public cancelEditing(): void {
        this.isEditing.set(false);
    }

    public reorderEditItems(event: AiEditItemDrop): void {
        if (event.previousIndex === event.currentIndex) {
            return;
        }

        const items = [...this.editItems()];
        moveItemInArray(items, event.previousIndex, event.currentIndex);
        this.editItems.set(items);
    }

    public updateEditItemFromView(update: AiEditItemUpdate): void {
        this.updateEditItem(update.index, update.field, update.value);
    }

    public updateEditItem(index: number, field: 'name' | 'amount' | 'unit', value: string): void {
        this.editItems.update(items => updateAiEditableItem(items, index, field, value));
    }

    public removeEditItem(index: number): void {
        this.editItems.update(items => items.filter((_, idx) => idx !== index));
    }

    public addEditItem(): void {
        this.editItems.update(items => [...items, createEmptyAiEditableItem(() => this.createEditId(), 'g')]);
    }

    public toggleDetails(): void {
        this.isDetailsExpanded.update(value => !value);
    }

    public updateDetailsDate(value: string): void {
        this.detailsDate.set(value);
    }

    public updateDetailsTime(value: string): void {
        this.detailsTime.set(value);
    }

    public updateDetailsComment(value: string): void {
        this.detailsComment.set(value);
    }

    public emitAddToMeal(): void {
        this.addToMeal.emit({
            date: this.detailsDate(),
            time: this.detailsTime(),
            comment: this.detailsComment().trim().length > 0 ? this.detailsComment().trim() : null,
            preMealSatietyLevel: normalizeSatietyLevel(this.preMealSatietyLevel()),
            postMealSatietyLevel: normalizeSatietyLevel(this.postMealSatietyLevel()),
        });
    }

    private capitalizeLabel(value?: string | null): string {
        if (value === null || value === undefined) {
            return '';
        }

        const trimmed = value.trim();
        if (trimmed.length === 0) {
            return '';
        }

        const [first, ...rest] = trimmed;
        return `${first.toLocaleUpperCase()}${rest.join('')}`;
    }

    private createEditId(): string {
        const cryptoLike = (globalThis as { crypto?: { randomUUID?: () => string } }).crypto;
        return cryptoLike?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;
    }

    private getDateInputValue(date: Date): string {
        const year = date.getFullYear();
        const month = (date.getMonth() + 1).toString().padStart(TIME_PAD_LENGTH, '0');
        const day = date.getDate().toString().padStart(TIME_PAD_LENGTH, '0');
        return `${year}-${month}-${day}`;
    }

    private getTimeInputValue(date: Date): string {
        const hours = date.getHours().toString().padStart(TIME_PAD_LENGTH, '0');
        const minutes = date.getMinutes().toString().padStart(TIME_PAD_LENGTH, '0');
        return `${hours}:${minutes}`;
    }
}

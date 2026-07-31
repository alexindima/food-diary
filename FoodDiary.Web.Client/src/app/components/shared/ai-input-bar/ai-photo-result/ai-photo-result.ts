import { CdkTrapFocus } from '@angular/cdk/a11y';
import { moveItemInArray } from '@angular/cdk/drag-drop';
import { DecimalPipe } from '@angular/common';
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
import { createClientId } from '../../../../shared/lib/client-id.utils';
import { formatDateInputValue, formatTimeInputValue } from '../../../../shared/lib/local-date.utils';
import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../../shared/lib/satiety-level.utils';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import type { AiInputBarMealDetails } from '../ai-input-bar.types';
import { AiPhotoDetailsPanelComponent } from './ai-photo-details-panel/ai-photo-details-panel';
import { AiPhotoEditListComponent } from './ai-photo-edit-list/ai-photo-edit-list';
import { AiPhotoPreviewComponent } from './ai-photo-preview/ai-photo-preview';
import { AiPhotoResultActionsComponent } from './ai-photo-result-actions/ai-photo-result-actions';
import { optimizeAiPhotoAnnotationLayout } from './ai-photo-result-lib/ai-photo-annotation-layout';
import type {
    AiDetailsToggleView,
    AiEditActionView,
    AiEditItemDrop,
    AiEditItemUpdate,
    AiEditUnitOption,
    AiPhotoAnnotation,
    AiPhotoEditApplied,
    AiResultRow,
    EditableAiItem,
} from './ai-photo-result-lib/ai-photo-result.types';
import { AiPhotoResultRowsComponent } from './ai-photo-result-rows/ai-photo-result-rows';

const LOCATION_CONFIDENCE_THRESHOLD = 0.35;
const PERCENT_SCALE = 100;

@Component({
    selector: 'fd-ai-photo-result',
    imports: [
        TranslatePipe,
        DecimalPipe,
        CdkTrapFocus,
        FdUiHintDirective,
        FdUiButtonComponent,
        AiPhotoPreviewComponent,
        AiPhotoResultActionsComponent,
        AiPhotoEditListComponent,
        AiPhotoResultRowsComponent,
        AiPhotoDetailsPanelComponent,
    ],
    templateUrl: './ai-photo-result.html',
    styleUrls: ['./ai-photo-result.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '(document:keydown.escape)': 'dismissPhotoDialog()',
    },
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

    protected readonly isEditing = signal(false);
    protected readonly isDetailsExpanded = signal(false);
    protected readonly isPortraitImage = signal(false);
    protected readonly annotationsVisible = signal(true);
    protected readonly selectedAnnotationId = signal<string | null>(null);
    protected readonly detailsDate = signal(this.getDateInputValue(new Date()));
    protected readonly detailsTime = signal(this.getTimeInputValue(new Date()));
    protected readonly detailsComment = signal('');
    protected readonly preMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    protected readonly postMealSatietyLevel = signal<number | null>(DEFAULT_SATIETY_LEVEL);
    protected readonly editItems = signal<EditableAiItem[]>([]);
    protected readonly resultRows = computed<AiResultRow[]>(() => {
        const nutritionItems = this.nutrition()?.items ?? [];
        return this.results().map((item, index) => ({
            key: item.nameEn,
            annotationId: hasReliableLocation(item) ? `${item.nameEn}-${index}` : null,
            displayName: this.resolveDisplayName(item),
            amountLabel: this.resolveAmountLabel(item),
            calories: nutritionItems[index]?.calories ?? null,
        }));
    });
    protected readonly annotations = computed<AiPhotoAnnotation[]>(() => {
        const nutritionItems = this.nutrition()?.items ?? [];
        const annotations = this.results().flatMap((item, index) => {
            if (!hasReliableLocation(item) || index >= nutritionItems.length) {
                return [];
            }
            const centerX = item.centerX * PERCENT_SCALE;
            const centerY = item.centerY * PERCENT_SCALE;
            const nutrition = nutritionItems[index];
            return [
                {
                    id: `${item.nameEn}-${index}`,
                    name: this.resolveDisplayName(item),
                    amountLabel: this.resolveAmountLabel(item),
                    centerX,
                    centerY,
                    cardX: 0,
                    cardY: 0,
                    cardWidth: 0,
                    cardHeight: 0,
                    connectorPoints: [
                        { x: centerX, y: centerY },
                        { x: centerX, y: centerY },
                    ] as const,
                    connectorPath: `${centerX},${centerY} ${centerX},${centerY}`,
                    calories: Math.round(nutrition.calories),
                    protein: Math.round(nutrition.protein),
                    fat: Math.round(nutrition.fat),
                    carbs: Math.round(nutrition.carbs),
                },
            ];
        });
        return optimizeAiPhotoAnnotationLayout(annotations, this.isPortraitImage());
    });
    protected readonly activeAnnotationId = computed(
        () =>
            this.annotations().find(annotation => annotation.id === this.selectedAnnotationId())?.id ??
            this.annotations().at(0)?.id ??
            null,
    );
    protected readonly editActionView = computed<AiEditActionView>(() =>
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
    protected readonly detailsToggleView = computed<AiDetailsToggleView>(() =>
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
    protected readonly submitDisabled = computed(
        () =>
            this.results().length === 0 ||
            this.nutrition() === null ||
            this.isAnalyzing() ||
            this.isNutritionLoading() ||
            this.isProcessing(),
    );
    protected readonly editUnitOptions = computed<AiEditUnitOption[]>(() =>
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

    private resolveUnitLabel(unit: string): string {
        const unitKey = resolveAiPhotoUnitKey(unit);
        return unitKey !== null ? this.translateService.instant(unitKey) : unit;
    }

    protected startEditing(): void {
        const editable = buildAiEditableItems(this.results(), this.nutrition(), () => this.createEditId());
        this.editItems.set(editable);
        this.sourceItems.set(editable.map(item => ({ ...item })));
        this.isEditing.set(true);
    }

    protected applyEditing(): void {
        const edited = this.editItems().filter(item => item.name.trim().length > 0 && item.amount > 0);
        const normalized: FoodVisionItem[] = normalizeAiEditableItems(edited);
        const requiresAi = requiresAiNutritionRecalculation(this.sourceItems(), edited);
        this.isEditing.set(false);

        if (normalized.length === 0) {
            this.editApplied.emit({ items: [], nutrition: null });
            return;
        }

        if (requiresAi) {
            this.editApplied.emit({ items: normalized, nutrition: null });
            return;
        }

        this.editApplied.emit({
            items: normalized,
            nutrition: recalculateEditedAiNutrition(this.nutrition(), this.sourceItems(), edited),
        });
    }

    protected applyEditAction(): void {
        if (this.isEditing()) {
            this.applyEditing();
            return;
        }

        this.startEditing();
    }

    protected cancelEditing(): void {
        this.isEditing.set(false);
    }

    protected reorderEditItems(event: AiEditItemDrop): void {
        if (event.previousIndex === event.currentIndex) {
            return;
        }

        const items = [...this.editItems()];
        moveItemInArray(items, event.previousIndex, event.currentIndex);
        this.editItems.set(items);
    }

    protected updateEditItemFromView(update: AiEditItemUpdate): void {
        this.updateEditItem(update.index, update.field, update.value);
    }

    protected updateEditItem(index: number, field: AiEditItemUpdate['field'], value: string): void {
        this.editItems.update(items => updateAiEditableItem(items, index, field, value));
    }

    protected removeEditItem(index: number): void {
        this.editItems.update(items => items.filter((_, idx) => idx !== index));
    }

    protected addEditItem(): void {
        this.editItems.update(items => [...items, createEmptyAiEditableItem(() => this.createEditId(), 'g')]);
    }

    protected toggleDetails(): void {
        this.isDetailsExpanded.update(value => !value);
    }

    protected toggleAnnotations(): void {
        this.annotationsVisible.update(visible => !visible);
    }

    protected dismissPhotoDialog(): void {
        if (this.imageUrl() !== null) {
            this.dismissed.emit();
        }
    }

    protected selectAnnotation(id: string): void {
        this.selectedAnnotationId.set(id);
    }

    protected updateDetailsDate(value: string): void {
        this.detailsDate.set(value);
    }

    protected updateDetailsTime(value: string): void {
        this.detailsTime.set(value);
    }

    protected updateDetailsComment(value: string): void {
        this.detailsComment.set(value);
    }

    protected emitAddToMeal(): void {
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
        return createClientId('ai-edit');
    }

    private getDateInputValue(date: Date): string {
        return formatDateInputValue(date);
    }

    private getTimeInputValue(date: Date): string {
        return formatTimeInputValue(date);
    }
}

function hasReliableLocation(
    item: FoodVisionItem,
): item is FoodVisionItem & { centerX: number; centerY: number; locationConfidence?: number | null } {
    return (
        item.centerX !== null &&
        item.centerX !== undefined &&
        item.centerY !== null &&
        item.centerY !== undefined &&
        item.centerX >= 0 &&
        item.centerX <= 1 &&
        item.centerY >= 0 &&
        item.centerY <= 1 &&
        (item.locationConfidence ?? 1) >= LOCATION_CONFIDENCE_THRESHOLD
    );
}

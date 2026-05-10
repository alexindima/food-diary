import { type CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import { type FoodNutritionResponse, type FoodVisionItem } from '../../../../shared/models/ai.data';
import { MealDetailsFieldsComponent } from '../../meal-details-fields/meal-details-fields.component';
import { type AiInputBarMealDetails } from '../ai-input-bar.types';

type EditableAiItem = {
    id: string;
    name: string;
    nameEn: string;
    nameLocal: string | null;
    amount: number;
    unit: string;
};

type AmountChange = {
    id: string;
    previousAmount: number;
    nextAmount: number;
};

type EditChangeSummary = {
    requiresAi: boolean;
    removedIds: string[];
    addedItems: EditableAiItem[];
    amountChanges: AmountChange[];
};

interface AiResultRow {
    key: string;
    displayName: string;
    amountLabel: string;
}

interface AiNutritionSummaryItem {
    labelKey: string;
    value: string;
}

interface AiEditUnitOption {
    value: string;
    label: string;
}

interface AiEditActionView {
    variant: 'primary' | 'secondary';
    fill: 'solid' | 'outline';
    labelKey: string;
}

interface AiDetailsToggleView {
    icon: string;
    labelKey: string;
}

@Component({
    selector: 'fd-ai-photo-result',
    standalone: true,
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent, DragDropModule, MealDetailsFieldsComponent],
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

    public readonly dismissed = output<void>();
    public readonly addToMeal = output<AiInputBarMealDetails>();
    public readonly editApplied = output<FoodVisionItem[]>();
    public readonly reanalyzeRequested = output<void>();

    public readonly isEditing = signal(false);
    public readonly isDetailsExpanded = signal(false);
    public readonly detailsDate = signal(this.getDateInputValue(new Date()));
    public readonly detailsTime = signal(this.getTimeInputValue(new Date()));
    public readonly detailsComment = signal('');
    public readonly preMealSatietyLevel = signal<number | null>(3);
    public readonly postMealSatietyLevel = signal<number | null>(3);
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
        if (!nutrition) {
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
        const rawName = item.nameLocal?.trim() || item.nameEn;
        return this.capitalizeLabel(rawName);
    }

    private resolveAmountLabel(item: FoodVisionItem): string {
        const amount = item.amount;
        const unitKey = this.resolveUnitKey(item.unit);
        const unitLabel = unitKey ? this.translateService.instant(unitKey) : item.unit;
        return unitLabel ? `${amount} ${unitLabel}`.trim() : `${amount}`.trim();
    }

    private resolveMacroLabel(value: number, unitKey: string): string {
        const locale = this.translateService.currentLang || this.translateService.defaultLang || 'en';
        const hasFraction = Math.abs(value % 1) > 0.01;
        const formatter = new Intl.NumberFormat(locale, {
            maximumFractionDigits: hasFraction ? 1 : 0,
            minimumFractionDigits: hasFraction ? 1 : 0,
        });
        const unitLabel = this.translateService.instant(unitKey);
        return `${formatter.format(value)} ${unitLabel}`.trim();
    }

    private resolveUnitLabel(unit: string): string {
        const unitKey = this.resolveUnitKey(unit);
        return unitKey ? this.translateService.instant(unitKey) : unit;
    }

    public removeEditItemAriaLabel(item: EditableAiItem): string {
        return this.translateService.instant('AI_INPUT_BAR.REMOVE_AI_ITEM', {
            name: item.name.trim() || item.nameEn.trim() || '?',
        });
    }

    public startEditing(): void {
        const items = this.results().length
            ? this.results()
            : (this.nutrition()?.items.map(item => ({
                  nameEn: item.name,
                  nameLocal: null,
                  amount: item.amount,
                  unit: item.unit,
                  confidence: 1,
              })) ?? []);

        const editable = items.map(item => ({
            id: this.createEditId(),
            name: item.nameLocal ?? item.nameEn,
            nameEn: item.nameEn,
            nameLocal: item.nameLocal ?? null,
            amount: item.amount,
            unit: item.unit,
        }));
        this.editItems.set(editable);
        this.sourceItems.set(editable.map(item => ({ ...item })));
        this.isEditing.set(true);
    }

    public applyEditing(): void {
        const edited = this.editItems().filter(item => item.name.trim().length > 0 && item.amount > 0);
        const normalized: FoodVisionItem[] = edited.map(item => ({
            nameEn: item.nameEn.trim() || item.name.trim(),
            nameLocal: item.nameLocal && item.nameLocal.trim().length ? item.nameLocal.trim() : null,
            amount: item.amount,
            unit: item.unit,
            confidence: 1,
        }));

        const changes = this.analyzeEditChanges(this.sourceItems(), edited);
        this.isEditing.set(false);

        if (changes.requiresAi) {
            this.editApplied.emit(normalized);
            return;
        }

        const recalculated = this.recalculateNutritionFromLocal(changes, edited);
        if (recalculated) {
            this.editApplied.emit(normalized);
        } else {
            this.editApplied.emit(normalized);
        }
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

    public onEditItemDrop(event: CdkDragDrop<EditableAiItem[]>): void {
        if (event.previousIndex === event.currentIndex) {
            return;
        }

        const items = [...this.editItems()];
        moveItemInArray(items, event.previousIndex, event.currentIndex);
        this.editItems.set(items);
    }

    public updateEditItem(index: number, field: 'name' | 'amount' | 'unit', value: string): void {
        this.editItems.update(items =>
            items.map((item, idx) => {
                if (idx !== index) {
                    return item;
                }

                if (field === 'amount') {
                    const parsed = Number.parseFloat(value);
                    return { ...item, amount: Number.isNaN(parsed) ? 0 : parsed };
                }

                if (field === 'unit') {
                    return { ...item, unit: value };
                }

                return { ...item, name: value, nameEn: value, nameLocal: value };
            }),
        );
    }

    public removeEditItem(index: number): void {
        this.editItems.update(items => items.filter((_, idx) => idx !== index));
    }

    public addEditItem(): void {
        this.editItems.update(items => [...items, { id: this.createEditId(), name: '', nameEn: '', nameLocal: '', amount: 0, unit: 'g' }]);
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
            comment: this.detailsComment().trim() || null,
            preMealSatietyLevel: this.normalizeSatietyLevel(this.preMealSatietyLevel()),
            postMealSatietyLevel: this.normalizeSatietyLevel(this.postMealSatietyLevel()),
        });
    }

    private resolveUnitKey(unit?: string | null): string | null {
        if (!unit) {
            return null;
        }

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
            kcal: 'GENERAL.UNITS.KCAL',
        };

        return unitMap[normalized] ?? null;
    }

    private capitalizeLabel(value?: string | null): string {
        if (!value) {
            return '';
        }

        const trimmed = value.trim();
        if (!trimmed) {
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
        const month = (date.getMonth() + 1).toString().padStart(2, '0');
        const day = date.getDate().toString().padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private getTimeInputValue(date: Date): string {
        const hours = date.getHours().toString().padStart(2, '0');
        const minutes = date.getMinutes().toString().padStart(2, '0');
        return `${hours}:${minutes}`;
    }

    private normalizeSatietyLevel(value: number | null): number {
        if (!value) {
            return 3;
        }

        if (value > 5) {
            return Math.min(5, Math.max(1, Math.round(value / 2)));
        }

        return Math.max(1, value);
    }

    private analyzeEditChanges(source: EditableAiItem[], edited: EditableAiItem[]): EditChangeSummary {
        const sourceById = new Map(source.map(item => [item.id, item]));
        const editedById = new Map(edited.map(item => [item.id, item]));
        const removedIds = source.filter(item => !editedById.has(item.id)).map(item => item.id);
        const addedItems = edited.filter(item => !sourceById.has(item.id));

        let requiresAi = addedItems.length > 0;
        const amountChanges: AmountChange[] = [];

        for (const item of edited) {
            const previous = sourceById.get(item.id);
            if (!previous) {
                continue;
            }

            const nameChanged = this.normalizeName(previous.name) !== this.normalizeName(item.name);
            const unitChanged = (previous.unit || '').trim().toLowerCase() !== (item.unit || '').trim().toLowerCase();
            if (nameChanged || unitChanged) {
                requiresAi = true;
            }

            if (previous.amount !== item.amount) {
                amountChanges.push({
                    id: item.id,
                    previousAmount: previous.amount,
                    nextAmount: item.amount,
                });
            }
        }

        return { requiresAi, removedIds, addedItems, amountChanges };
    }

    private recalculateNutritionFromLocal(changes: EditChangeSummary, edited: EditableAiItem[]): FoodNutritionResponse | null {
        const nutrition = this.nutrition();
        if (!nutrition) {
            return null;
        }

        const nutritionById = new Map(nutrition.items.map(item => [this.normalizeName(item.name), item]));
        const source = this.sourceItems();

        const updatedItems = edited
            .map(item => {
                const base = source.find(sourceItem => sourceItem.id === item.id);
                const originalName = base?.nameEn ?? base?.name ?? item.name;
                const originalNutrition = nutritionById.get(this.normalizeName(originalName));
                if (!originalNutrition) {
                    return null;
                }

                const ratio = base && base.amount > 0 ? item.amount / base.amount : 1;
                return {
                    name: item.name,
                    amount: item.amount,
                    unit: item.unit,
                    calories: originalNutrition.calories * ratio,
                    protein: originalNutrition.protein * ratio,
                    fat: originalNutrition.fat * ratio,
                    carbs: originalNutrition.carbs * ratio,
                    fiber: originalNutrition.fiber * ratio,
                    alcohol: originalNutrition.alcohol * ratio,
                };
            })
            .filter((item): item is FoodNutritionResponse['items'][number] => item !== null);

        if (updatedItems.length !== edited.length) {
            return null;
        }

        const totals = updatedItems.reduce(
            (acc, item) => ({
                calories: acc.calories + item.calories,
                protein: acc.protein + item.protein,
                fat: acc.fat + item.fat,
                carbs: acc.carbs + item.carbs,
                fiber: acc.fiber + item.fiber,
                alcohol: acc.alcohol + item.alcohol,
            }),
            { calories: 0, protein: 0, fat: 0, carbs: 0, fiber: 0, alcohol: 0 },
        );

        return { ...totals, items: updatedItems, notes: nutrition.notes ?? null };
    }

    private normalizeName(value?: string | null): string {
        return (value ?? '').trim().toLowerCase();
    }
}

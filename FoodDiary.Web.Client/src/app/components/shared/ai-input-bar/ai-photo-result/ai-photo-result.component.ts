import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MatIconModule } from '@angular/material/icon';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { DEFAULT_SATIETY_LEVELS } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';
import {
    MealSatietyLevelDialogComponent,
    SatietyLevelDialogData,
} from '../../../../features/meals/dialogs/satiety-level-dialog/meal-satiety-level-dialog.component';
import { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import { AiInputBarMealDetails } from '../ai-input-bar.types';

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

@Component({
    selector: 'fd-ai-photo-result',
    standalone: true,
    imports: [TranslatePipe, MatIconModule, FdUiButtonComponent, DragDropModule],
    templateUrl: './ai-photo-result.component.html',
    styleUrls: ['./ai-photo-result.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiPhotoResultComponent {
    private readonly translateService = inject(TranslateService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly unitOptions = ['g', 'ml', 'pcs'] as const;

    public readonly titleKey = input<string>('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.RESULTS_TITLE');
    public readonly imageUrl = input<string | null>(null);
    public readonly sourceText = input<string | null>(null);
    public readonly sourceTextLabelKey = input<string>('AI_INPUT_BAR.TEXT_PREVIEW_LABEL');
    public readonly submitLabelKey = input<string>('CONSUMPTION_LIST.VOICE_CREATE_MEAL');
    public readonly showDetails = input<boolean>(true);
    public readonly results = input<FoodVisionItem[]>([]);
    public readonly isAnalyzing = input<boolean>(false);
    public readonly isNutritionLoading = input<boolean>(false);
    public readonly nutrition = input<FoodNutritionResponse | null>(null);
    public readonly errorKey = input<string | null>(null);
    public readonly nutritionErrorKey = input<string | null>(null);
    public readonly isProcessing = input<boolean>(false);

    public readonly dismissed = output<void>();
    public readonly addToMeal = output<AiInputBarMealDetails>();
    public readonly editApplied = output<FoodVisionItem[]>();
    public readonly reanalyzeRequested = output<void>();

    public readonly isEditing = signal(false);
    public readonly isDetailsExpanded = signal(false);
    public readonly detailsDate = signal(this.getDateInputValue(new Date()));
    public readonly detailsTime = signal(this.getTimeInputValue(new Date()));
    public readonly detailsComment = signal('');
    public readonly preMealSatietyLevel = signal<number | null>(null);
    public readonly postMealSatietyLevel = signal<number | null>(null);
    public readonly editItems = signal<EditableAiItem[]>([]);
    private readonly sourceItems = signal<EditableAiItem[]>([]);

    public getDisplayName(item: FoodVisionItem): string {
        const rawName = item.nameLocal?.trim() || item.nameEn;
        return this.capitalizeLabel(rawName);
    }

    public formatAmount(item: FoodVisionItem): string {
        const amount = item.amount ?? '';
        const unitKey = this.resolveUnitKey(item.unit);
        const unitLabel = unitKey ? this.translateService.instant(unitKey) : item.unit;
        return unitLabel ? `${amount} ${unitLabel}`.trim() : `${amount}`.trim();
    }

    public formatMacro(value: number, unitKey: string): string {
        const locale = this.translateService.currentLang || this.translateService.defaultLang || 'en';
        const hasFraction = Math.abs(value % 1) > 0.01;
        const formatter = new Intl.NumberFormat(locale, {
            maximumFractionDigits: hasFraction ? 1 : 0,
            minimumFractionDigits: hasFraction ? 1 : 0,
        });
        const unitLabel = this.translateService.instant(unitKey);
        return `${formatter.format(value)} ${unitLabel}`.trim();
    }

    public getUnitOptions(): readonly string[] {
        return this.unitOptions;
    }

    public getUnitLabel(unit: string): string {
        const unitKey = this.resolveUnitKey(unit);
        return unitKey ? this.translateService.instant(unitKey) : unit;
    }

    public startEditing(): void {
        const items = this.results().length
            ? this.results()
            : (this.nutrition()?.items?.map(item => ({
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
            nameEn: item.nameEn?.trim() || item.name.trim(),
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
            preMealSatietyLevel: this.preMealSatietyLevel(),
            postMealSatietyLevel: this.postMealSatietyLevel(),
        });
    }

    public getSatietyLevelMeta(value: number | null): { label: string; description: string; gradient: string } {
        if (!value) {
            return {
                label: this.translateService.instant('AI_INPUT_BAR.SATIETY_PLACEHOLDER_TITLE'),
                description: this.translateService.instant('AI_INPUT_BAR.SATIETY_PLACEHOLDER_DESCRIPTION'),
                gradient: 'linear-gradient(135deg, #e2e8f0, #cbd5f5)',
            };
        }

        const config = DEFAULT_SATIETY_LEVELS.find(level => level.value === value);
        return {
            label: `${value} - ${this.translateService.instant(config?.titleKey ?? '')}`,
            description: this.translateService.instant(config?.descriptionKey ?? ''),
            gradient: config?.gradient ?? 'linear-gradient(135deg, #e2e8f0, #cbd5f5)',
        };
    }

    public openSatietyDialog(kind: 'before' | 'after'): void {
        const currentValue = kind === 'before' ? this.preMealSatietyLevel() : this.postMealSatietyLevel();
        const titleKey =
            kind === 'before' ? 'CONSUMPTION_MANAGE.HUNGER_BEFORE_DIALOG_TITLE' : 'CONSUMPTION_MANAGE.HUNGER_AFTER_DIALOG_TITLE';
        const dialogRef = this.fdDialogService.open<MealSatietyLevelDialogComponent, SatietyLevelDialogData, number>(
            MealSatietyLevelDialogComponent,
            {
                size: 'lg',
                data: {
                    titleKey,
                    subtitleKey: 'CONSUMPTION_MANAGE.SATIETY_DIALOG_HINT',
                    value: currentValue,
                },
            },
        );

        dialogRef.afterClosed().subscribe(value => {
            if (typeof value !== 'number') {
                return;
            }

            if (kind === 'before') {
                this.preMealSatietyLevel.set(value);
            } else {
                this.postMealSatietyLevel.set(value);
            }
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
        return crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;
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

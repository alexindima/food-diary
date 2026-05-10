import { type CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { catchError, of } from 'rxjs';

import { ImageUploadFieldComponent } from '../../../../components/shared/image-upload-field/image-upload-field.component';
import { AiFoodService } from '../../../../shared/api/ai-food.service';
import { type FoodNutritionResponse, type FoodVisionItem } from '../../../../shared/models/ai.data';
import { type ImageSelection } from '../../../../shared/models/image-upload.data';
import { type MealAiSessionManageDto } from '../../models/meal.data';

type PhotoAiDialogData = {
    initialSelection?: ImageSelection | null;
    initialSession?: MealAiSessionManageDto | null;
    mode?: 'edit' | 'create';
};

type RecognizedItemView = {
    item: FoodVisionItem;
    displayName: string;
    amount: number;
    unit: string | null | undefined;
    unitKey: string | null;
};

type UnitOptionView = {
    value: string;
    labelKey: string;
};

type MacroSummaryItem = {
    key: 'calories' | 'protein' | 'fat' | 'carbs' | 'fiber' | 'alcohol';
    labelKey: string;
    value: number;
    unitKey: string;
    numberFormat: string;
};

type PhotoAiEditActionState = {
    variant: 'primary' | 'secondary';
    fill: 'solid' | 'outline';
    labelKey: string;
};

const UNIT_OPTIONS: readonly UnitOptionView[] = [
    { value: 'g', labelKey: 'GENERAL.UNITS.G' },
    { value: 'ml', labelKey: 'GENERAL.UNITS.ML' },
    { value: 'pcs', labelKey: 'GENERAL.UNITS.PCS' },
];

@Component({
    selector: 'fd-meal-photo-recognition-dialog',
    standalone: true,
    templateUrl: './meal-photo-recognition-dialog.component.html',
    styleUrls: ['./meal-photo-recognition-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiIconComponent,
        DragDropModule,
        ImageUploadFieldComponent,
    ],
})
export class MealPhotoRecognitionDialogComponent {
    private readonly dialogData = inject<PhotoAiDialogData>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    private readonly aiFoodService = inject(AiFoodService);
    private readonly dialogRef = inject(FdUiDialogRef<MealPhotoRecognitionDialogComponent, MealAiSessionManageDto | null>, {
        optional: true,
    });

    public readonly isLoading = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly results = signal<FoodVisionItem[]>([]);
    public readonly selection = signal<ImageSelection | null>(null);
    public readonly hasAnalyzed = signal(false);
    public readonly isNutritionLoading = signal(false);
    public readonly nutrition = signal<FoodNutritionResponse | null>(null);
    public readonly nutritionErrorKey = signal<string | null>(null);
    public readonly initialSelection = this.dialogData.initialSelection ?? null;
    public readonly isEditMode = signal(this.dialogData.mode === 'edit');
    public readonly isEditing = signal(false);
    public readonly editItems = signal<EditableAiItem[]>([]);
    private readonly sourceItems = signal<EditableAiItem[]>([]);
    private shouldSkipNextImageChange = Boolean(this.dialogData.initialSession);
    public readonly unitOptions = UNIT_OPTIONS;
    public readonly resultViews = computed<RecognizedItemView[]>(() =>
        this.results().map(item => ({
            item,
            displayName: this.toDisplayName(item),
            amount: item.amount,
            unit: item.unit,
            unitKey: this.resolveUnitKey(item.unit),
        })),
    );
    public readonly macroSummaryItems = computed<MacroSummaryItem[]>(() => {
        const nutrition = this.nutrition();
        if (!nutrition) {
            return [];
        }

        return [
            this.toMacroSummaryItem('calories', 'GENERAL.NUTRIENTS.CALORIES', nutrition.calories, 'GENERAL.UNITS.KCAL'),
            this.toMacroSummaryItem('protein', 'GENERAL.NUTRIENTS.PROTEIN', nutrition.protein, 'GENERAL.UNITS.G'),
            this.toMacroSummaryItem('fat', 'GENERAL.NUTRIENTS.FAT', nutrition.fat, 'GENERAL.UNITS.G'),
            this.toMacroSummaryItem('carbs', 'GENERAL.NUTRIENTS.CARB', nutrition.carbs, 'GENERAL.UNITS.G'),
            this.toMacroSummaryItem('fiber', 'GENERAL.NUTRIENTS.FIBER', nutrition.fiber, 'GENERAL.UNITS.G'),
            this.toMacroSummaryItem('alcohol', 'GENERAL.NUTRIENTS.ALCOHOL', nutrition.alcohol, 'GENERAL.UNITS.G'),
        ];
    });
    public readonly editActionState = computed<PhotoAiEditActionState>(() =>
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
    public readonly submitLabelKey = computed(() =>
        this.isEditMode() ? 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.SAVE' : 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ADD_TO_MEAL',
    );
    public readonly statusKey = computed(() => {
        if (!this.selection()) {
            return null;
        }

        if (this.isLoading()) {
            return 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.STATUS_ANALYZING';
        }

        if (this.isNutritionLoading()) {
            return 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.STATUS_NUTRITION';
        }

        if (this.hasAnalyzed()) {
            return 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.STATUS_DONE';
        }

        return null;
    });

    public constructor() {
        const session = this.dialogData.initialSession;
        if (session) {
            this.applyInitialSession(session);
        }
    }

    private toDisplayName(item: FoodVisionItem): string {
        const rawName = item.nameLocal?.trim() || item.nameEn;
        return this.capitalizeLabel(rawName);
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

    public onImageChanged(selection: ImageSelection | null): void {
        if (this.shouldSkipNextImageChange) {
            this.shouldSkipNextImageChange = false;
            this.selection.set(selection);
            return;
        }

        this.selection.set(selection);
        this.errorKey.set(null);
        this.results.set([]);
        this.hasAnalyzed.set(false);
        this.isNutritionLoading.set(false);
        this.nutrition.set(null);
        this.nutritionErrorKey.set(null);

        if (!selection?.assetId) {
            return;
        }

        this.runAnalysis(selection.assetId);
    }

    public onReanalyze(): void {
        const assetId = this.selection()?.assetId;
        if (!assetId || this.isLoading() || this.isEditing()) {
            return;
        }

        this.errorKey.set(null);
        this.results.set([]);
        this.hasAnalyzed.set(false);
        this.isNutritionLoading.set(false);
        this.nutrition.set(null);
        this.nutritionErrorKey.set(null);
        this.runAnalysis(assetId);
    }

    public addToMeal(): void {
        const session = this.buildSessionPayload();
        if (!session) {
            return;
        }
        this.dialogRef?.close(session);
    }

    public close(): void {
        this.dialogRef?.close(null);
    }

    private buildSessionPayload(): MealAiSessionManageDto | null {
        const nutrition = this.nutrition();
        if (!nutrition) {
            return null;
        }

        const assetId = this.selection()?.assetId ?? null;
        const items = nutrition.items.map(item => {
            const match = this.findVisionMatch(item.name);
            return {
                nameEn: match?.nameEn ?? item.name,
                nameLocal: match?.nameLocal ?? null,
                amount: item.amount,
                unit: item.unit,
                calories: item.calories,
                proteins: item.protein,
                fats: item.fat,
                carbs: item.carbs,
                fiber: item.fiber,
                alcohol: item.alcohol,
            };
        });

        return {
            imageAssetId: assetId,
            imageUrl: this.selection()?.url ?? null,
            recognizedAtUtc: new Date().toISOString(),
            notes: nutrition.notes ?? null,
            items,
        };
    }

    private findVisionMatch(name: string | null | undefined): FoodVisionItem | null {
        if (!name) {
            return null;
        }

        const normalized = name.trim().toLowerCase();
        return (
            this.results().find(
                item => item.nameEn.trim().toLowerCase() === normalized || item.nameLocal?.trim().toLowerCase() === normalized,
            ) ?? null
        );
    }

    private runAnalysis(assetId: string): void {
        this.isLoading.set(true);
        this.aiFoodService
            .analyzeFoodImage({ imageAssetId: assetId })
            .pipe(
                catchError(err => {
                    if (err?.status === 403) {
                        this.errorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM');
                    } else if (err?.status === 429) {
                        this.errorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
                    } else {
                        this.errorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_GENERIC');
                    }
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.isLoading.set(false);
                this.hasAnalyzed.set(true);
                if (!response) {
                    return;
                }
                const items = response.items;
                this.results.set(items);
                if (items.length) {
                    this.runNutrition(items);
                }
            });
    }

    private runNutrition(items: FoodVisionItem[]): void {
        this.isNutritionLoading.set(true);
        this.nutrition.set(null);
        this.nutritionErrorKey.set(null);

        this.aiFoodService
            .calculateNutrition({ items })
            .pipe(
                catchError(err => {
                    if (err?.status === 429) {
                        this.nutritionErrorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
                    } else {
                        this.nutritionErrorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR');
                    }
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.isNutritionLoading.set(false);
                if (!response) {
                    return;
                }
                this.nutrition.set(response);
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
        const normalized = edited.map(item => ({
            nameEn: item.nameEn.trim() || item.name.trim(),
            nameLocal: item.nameLocal && item.nameLocal.trim().length ? item.nameLocal.trim() : null,
            amount: item.amount,
            unit: item.unit,
            confidence: 1,
        }));

        const changes = this.analyzeEditChanges(this.sourceItems(), edited);
        this.results.set(normalized);
        this.hasAnalyzed.set(true);
        this.isEditing.set(false);

        if (changes.requiresAi) {
            this.runNutrition(normalized);
            return;
        }

        const recalculated = this.recalculateNutritionFromLocal(changes, edited);
        if (recalculated) {
            this.nutrition.set(recalculated);
        } else {
            this.runNutrition(normalized);
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

    private createEditId(): string {
        const cryptoLike = (globalThis as { crypto?: { randomUUID?: () => string } }).crypto;
        return cryptoLike?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;
    }

    private applyInitialSession(session: MealAiSessionManageDto): void {
        this.selection.set(
            session.imageUrl || session.imageAssetId
                ? {
                      url: session.imageUrl ?? null,
                      assetId: session.imageAssetId ?? null,
                  }
                : null,
        );
        this.results.set(
            session.items.map(item => ({
                nameEn: item.nameEn,
                nameLocal: item.nameLocal ?? null,
                amount: item.amount,
                unit: item.unit,
                confidence: 1,
            })),
        );
        this.hasAnalyzed.set(true);
        this.isLoading.set(false);
        this.errorKey.set(null);
        this.nutritionErrorKey.set(null);
        this.isNutritionLoading.set(false);
        this.nutrition.set(this.buildNutritionFromSession(session.items));
    }

    private buildNutritionFromSession(items: MealAiSessionManageDto['items']): FoodNutritionResponse {
        const totals = items.reduce(
            (acc, item) => ({
                calories: acc.calories + item.calories,
                protein: acc.protein + item.proteins,
                fat: acc.fat + item.fats,
                carbs: acc.carbs + item.carbs,
                fiber: acc.fiber + item.fiber,
                alcohol: acc.alcohol + item.alcohol,
            }),
            { calories: 0, protein: 0, fat: 0, carbs: 0, fiber: 0, alcohol: 0 },
        );

        return {
            ...totals,
            items: items.map(item => ({
                name: item.nameLocal ?? item.nameEn,
                amount: item.amount,
                unit: item.unit,
                calories: item.calories,
                protein: item.proteins,
                fat: item.fats,
                carbs: item.carbs,
                fiber: item.fiber,
                alcohol: item.alcohol,
            })),
        };
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

        return {
            requiresAi,
            removedIds,
            addedItems,
            amountChanges,
        };
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

        return {
            ...totals,
            items: updatedItems,
            notes: nutrition.notes ?? null,
        };
    }

    private normalizeName(value?: string | null): string {
        return (value ?? '').trim().toLowerCase();
    }

    private toMacroSummaryItem(key: MacroSummaryItem['key'], labelKey: string, value: number, unitKey: string): MacroSummaryItem {
        const hasFraction = Math.abs(value % 1) > 0.01;
        return {
            key,
            labelKey,
            value,
            unitKey,
            numberFormat: hasFraction ? '1.1-1' : '1.0-0',
        };
    }
}

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

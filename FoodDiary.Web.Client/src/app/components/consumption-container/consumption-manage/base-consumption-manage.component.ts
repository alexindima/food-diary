import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    FactoryProvider,
    computed,
    inject,
    input,
    OnInit,
    signal,
} from '@angular/core';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { NavigationService } from '../../../services/navigation.service';
import { RecipeService } from '../../../services/recipe.service';
import {
    ConsumptionItemSelectDialogComponent,
    ConsumptionItemSelectDialogData,
    ConsumptionItemSelection,
} from '../consumption-item-select-dialog/consumption-item-select-dialog.component';
import {
    Consumption,
    ConsumptionAiItemManageDto,
    ConsumptionAiSessionManageDto,
    ConsumptionItemManageDto,
    ConsumptionManageDto,
    ConsumptionSourceType,
} from '../../../types/consumption.data';
import { ConsumptionService } from '../../../services/consumption.service';
import { FormGroupControls } from '../../../types/common.data';
import { Product, MeasurementUnit } from '../../../types/product.data';
import { Recipe, RecipeIngredient } from '../../../types/recipe.data';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { nonEmptyArrayValidator } from '../../../validators/non-empty-array.validator';
import { NutrientData } from '../../../types/charts.data';
import {
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { FdUiFormErrorComponent, FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiNutrientInputComponent } from 'fd-ui-kit/nutrient-input/fd-ui-nutrient-input.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiTimeInputComponent } from 'fd-ui-kit/time-input/fd-ui-time-input.component';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import {
    SatietyLevelDialogComponent,
    SatietyLevelDialogData,
} from '../satiety-level-dialog/satiety-level-dialog.component';
import { DEFAULT_SATIETY_LEVELS } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    ConsumptionManageRedirectAction,
    ConsumptionManageSuccessDialogComponent,
    ConsumptionManageSuccessDialogData,
} from './success-dialog/consumption-manage-success-dialog.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { ImageUploadFieldComponent } from '../../shared/image-upload-field/image-upload-field.component';
import { ImageSelection } from '../../../types/image-upload.data';
import { ActivatedRoute, Router } from '@angular/router';
import { QuickConsumptionItem } from '../../../services/quick-consumption.service';
import { ConsumptionPhotoRecognitionDialogComponent } from '../consumption-photo-recognition-dialog/consumption-photo-recognition-dialog.component';
import { AiFoodService } from '../../../services/ai-food.service';
import { UserAiUsageResponse } from '../../../types/ai.data';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        nonEmptyArray: () => 'FORM_ERRORS.NON_EMPTY_ARRAY',
        min: (error?: unknown) => ({
            key: 'FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO',
            params: { min: (error as { min?: number } | undefined)?.min },
        }),
    }),
};

@Component({
    selector: 'fd-base-consumption-manage',
    templateUrl: './base-consumption-manage.component.html',
    styleUrls: ['./base-consumption-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [VALIDATION_ERRORS_PROVIDER],
    imports: [
        ReactiveFormsModule,
        FormsModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiCheckboxComponent,
        FdUiNutrientInputComponent,
        FdUiDateInputComponent,
        FdUiTimeInputComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        FdUiIconModule,
        FdUiFormErrorComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        ImageUploadFieldComponent,
    ],
})
export class BaseConsumptionManageComponent implements OnInit {
    private readonly consumptionService = inject(ConsumptionService);
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly recipeService = inject(RecipeService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly aiFoodService = inject(AiFoodService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly recipeServingWeightCache = new Map<string, number | null>();
    private readonly nutrientFillAlpha = 0.14;
    private readonly nutrientPalette = {
        calories: '#E11D48',
        proteins: '#0284C7',
        fats: '#C2410C',
        carbs: '#0F766E',
        fiber: '#7E22CE',
        alcohol: '#64748B',
    };
    public readonly nutrientFillColors = {
        calories: this.applyAlpha(this.nutrientPalette.calories, this.nutrientFillAlpha),
        fiber: this.applyAlpha(this.nutrientPalette.fiber, this.nutrientFillAlpha),
        proteins: this.applyAlpha(this.nutrientPalette.proteins, this.nutrientFillAlpha),
        fats: this.applyAlpha(this.nutrientPalette.fats, this.nutrientFillAlpha),
        carbs: this.applyAlpha(this.nutrientPalette.carbs, this.nutrientFillAlpha),
        alcohol: this.applyAlpha(this.nutrientPalette.alcohol, this.nutrientFillAlpha),
    };
    public readonly nutrientTextColors = {
        calories: this.nutrientPalette.calories,
        fiber: this.nutrientPalette.fiber,
        proteins: this.nutrientPalette.proteins,
        fats: this.nutrientPalette.fats,
        carbs: this.nutrientPalette.carbs,
    };

    public consumption = input<Consumption | null>();
    public totalCalories = signal<number>(0);
    public totalFiber = signal<number>(0);
    public totalAlcohol = signal<number>(0);
    public nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public globalError = signal<string | null>(null);
    public aiSessions = signal<ConsumptionAiSessionManageDto[]>([]);
    public aiUsage = signal<UserAiUsageResponse | null>(null);
    public aiQuotaExceeded = computed(() => {
        const usage = this.aiUsage();
        if (!usage) {
            return false;
        }
        return usage.inputUsed >= usage.inputLimit || usage.outputUsed >= usage.outputLimit;
    });

    public consumptionForm: FormGroup<ConsumptionFormData>;
    public readonly mealTypeOptions = ['BREAKFAST', 'LUNCH', 'DINNER', 'SNACK', 'OTHER'] as const;
    public mealTypeSelectOptions: FdUiSelectOption<string>[] = [];

    public constructor() {
        this.consumptionForm = new FormGroup<ConsumptionFormData>({
            date: new FormControl<string>(this.getDateInputValue(new Date()), {
                nonNullable: true,
                validators: Validators.required,
            }),
            time: new FormControl<string>(this.getTimeInputValue(new Date()), {
                nonNullable: true,
                validators: Validators.required,
            }),
            mealType: new FormControl<string | null>(null),
            items: new FormArray<FormGroup<ConsumptionItemFormData>>(
                [this.createConsumptionItem()],
                nonEmptyArrayValidator()
            ),
            comment: new FormControl<string | null>(null),
            imageUrl: new FormControl<ImageSelection | null>(null),
            isNutritionAutoCalculated: new FormControl<boolean>(true, { nonNullable: true }),
            manualCalories: new FormControl<number | null>(null),
            manualProteins: new FormControl<number | null>(null),
            manualFats: new FormControl<number | null>(null),
            manualCarbs: new FormControl<number | null>(null),
            manualFiber: new FormControl<number | null>(null),
            manualAlcohol: new FormControl<number | null>(null, [Validators.min(0)]),
            preMealSatietyLevel: new FormControl<number | null>(null),
            postMealSatietyLevel: new FormControl<number | null>(null),
        });

        this.buildMealTypeOptions();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildMealTypeOptions();
        });

        this.updateManualNutritionValidators(true);
        this.consumptionForm.controls.isNutritionAutoCalculated.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(isAuto => {
                this.updateManualNutritionValidators(isAuto);
                if (!isAuto) {
                    this.populateManualNutritionFromCurrentSummary();
                }
                this.updateSummary();
            });
    }

    public ngOnInit(): void {
        this.loadAiUsage();
        const presetMealType = this.resolvePresetMealType();
        if (presetMealType) {
            this.consumptionForm.controls.mealType.setValue(presetMealType);
        } else if (!this.consumption()) {
            this.setAutoMealTypeFromDate();
        }

        this.prefillFromNavigationState();

        const consumption = this.consumption();
        if (consumption) {
            this.populateForm(consumption);
            this.updateSummary();
        }

        if (!presetMealType && !this.consumption()) {
            this.consumptionForm.controls.date.valueChanges
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(() => {
                    this.setAutoMealTypeFromDate();
                });
            this.consumptionForm.controls.time.valueChanges
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(() => {
                    this.setAutoMealTypeFromDate();
                });
        }

        this.consumptionForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updateSummary();
            this.clearGlobalError();
        });
    }

    public async onCancel(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public get items(): FormArray<FormGroup<ConsumptionItemFormData>> {
        return this.consumptionForm.controls.items;
    }

    private resolvePresetMealType(): string | null {
        const stateMealType = (this.router.getCurrentNavigation()?.extras.state as { mealType?: string } | undefined)
            ?.mealType;
        const queryMealType = this.route.snapshot.queryParamMap.get('mealType');
        const raw = (stateMealType ?? queryMealType)?.toUpperCase();
        if (!raw) {
            return null;
        }
        const isValid = this.mealTypeOptions.includes(raw as (typeof this.mealTypeOptions)[number]);
        return isValid ? raw : null;
    }

    private setAutoMealTypeFromDate(): void {
        if (this.consumption()) {
            return;
        }

        const mealTypeControl = this.consumptionForm.controls.mealType;
        if (mealTypeControl.dirty) {
            return;
        }

        const date = this.buildDateTime();
        if (Number.isNaN(date.getTime())) {
            return;
        }

        mealTypeControl.setValue(this.resolveMealTypeByTime(date), { emitEvent: false });
    }

    private resolveMealTypeByTime(date: Date): string {
        const totalMinutes = date.getHours() * 60 + date.getMinutes();
        if (totalMinutes >= 300 && totalMinutes < 660) {
            return 'BREAKFAST';
        }
        if (totalMinutes >= 660 && totalMinutes < 1020) {
            return 'LUNCH';
        }
        if (totalMinutes >= 1020 && totalMinutes < 1320) {
            return 'DINNER';
        }
        return 'SNACK';
    }

    private prefillFromNavigationState(): void {
        if (this.consumption()) {
            return;
        }

        const navigationState = (this.router.getCurrentNavigation()?.extras.state as { quickConsumptionItems?: QuickConsumptionItem[] } | undefined)
            ?.quickConsumptionItems;
        const historyState = (window.history.state as { quickConsumptionItems?: QuickConsumptionItem[] } | undefined)
            ?.quickConsumptionItems;
        const draftItems = navigationState ?? historyState;

        if (!draftItems?.length) {
            return;
        }

        this.items.clear();
        draftItems.forEach(item => {
            const sourceType = item.type === 'recipe' ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product;
            const amount = item.amount ?? 0;
            this.items.push(this.createConsumptionItem(
                sourceType === ConsumptionSourceType.Product ? item.product ?? null : null,
                sourceType === ConsumptionSourceType.Recipe ? item.recipe ?? null : null,
                amount,
                sourceType,
            ));

            if (sourceType === ConsumptionSourceType.Recipe) {
                const currentIndex = this.items.length - 1;
                this.ensureRecipeWeightForExistingItem(currentIndex, amount, item.recipe ?? null);
            }
        });

        if (!this.items.length) {
            this.items.push(this.createConsumptionItem());
        }

        this.updateSummary();
    }

    public stringifyMealType = (value: string | null): string =>
        value ? this.translateService.instant('MEAL_TYPES.' + value) : '';

    public isProductItem(index: number): boolean {
        return this.items.at(index).controls.sourceType.value === ConsumptionSourceType.Product;
    }

    public isRecipeItem(index: number): boolean {
        return this.items.at(index).controls.sourceType.value === ConsumptionSourceType.Recipe;
    }

    public getProductName(index: number): string {
        const control = this.items.at(index).controls.product;
        return control.value?.name || '';
    }

    public getRecipeName(index: number): string {
        const control = this.items.at(index).controls.recipe;
        return control.value?.name || '';
    }

    public getAmountUnitLabel(index: number): string | null {
        if (this.isProductItem(index)) {
            const unit = this.items.at(index).controls.product.value?.baseUnit;
            return unit ? this.translateService.instant('PRODUCT_AMOUNT_UNITS.' + unit.toUpperCase()) : null;
        }

        if (this.isRecipeItem(index)) {
            return this.translateService.instant('PRODUCT_AMOUNT_UNITS.G');
        }

        return null;
    }

    public isProductInvalid(index: number): boolean {
        if (!this.isProductItem(index)) {
            return false;
        }
        const control = this.items.at(index).controls.product;
        return control.invalid && control.touched;
    }

    public isRecipeInvalid(index: number): boolean {
        if (!this.isRecipeItem(index)) {
            return false;
        }
        const control = this.items.at(index).controls.recipe;
        return control.invalid && control.touched;
    }

    public addConsumptionItem(): void {
        this.items.push(this.createConsumptionItem());
        const newIndex = this.items.length - 1;
        queueMicrotask(() => this.onItemSourceClick(newIndex));
    }

    public onAddConsumptionFromPhoto(): void {
        if (this.aiQuotaExceeded()) {
            return;
        }

        this.fdDialogService
            .open<ConsumptionPhotoRecognitionDialogComponent, never, ConsumptionAiSessionManageDto | null>(
                ConsumptionPhotoRecognitionDialogComponent,
                { size: 'lg' },
            )
            .afterClosed()
            .subscribe(session => {
                if (!session) {
                    return;
                }
                this.aiSessions.update(current => [...current, session]);
                this.updateSummary();
            });
    }

    public onDeleteAiSession(index: number): void {
        this.aiSessions.update(current => current.filter((_, currentIndex) => currentIndex !== index));
        this.updateSummary();
    }

    public onEditAiSession(index: number): void {
        const session = this.aiSessions()[index];
        const selection: ImageSelection | null = session?.imageUrl
            ? { url: session.imageUrl ?? null, assetId: session.imageAssetId ?? null }
            : null;

        this.fdDialogService
            .open<
                ConsumptionPhotoRecognitionDialogComponent,
                { initialSelection: ImageSelection | null; initialSession: ConsumptionAiSessionManageDto | null; mode: 'edit' },
                ConsumptionAiSessionManageDto | null
            >(
                ConsumptionPhotoRecognitionDialogComponent,
                { size: 'lg', data: { initialSelection: selection, initialSession: session ?? null, mode: 'edit' } },
            )
            .afterClosed()
            .subscribe(updated => {
                if (!updated) {
                    return;
                }
                this.aiSessions.update(current =>
                    current.map((item, currentIndex) => (currentIndex === index ? updated : item))
                );
                this.updateSummary();
            });
    }

    public formatAiAmount(amount: number, unit: string): string {
        const normalized = unit?.trim().toLowerCase();
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

        const unitKey = normalized ? unitMap[normalized] : null;
        const unitLabel = unitKey ? this.translateService.instant(unitKey) : unit;
        return unitLabel ? `${amount} ${unitLabel}`.trim() : `${amount}`.trim();
    }

    public formatAiName(name?: string | null): string {
        if (!name) {
            return '';
        }

        const trimmed = name.trim();
        if (!trimmed) {
            return '';
        }

        const [first, ...rest] = trimmed;
        return `${first.toLocaleUpperCase()}${rest.join('')}`;
    }

    public visibleAiItems(
        items: ConsumptionAiItemManageDto[],
        maxVisible: number,
    ): ConsumptionAiItemManageDto[] {
        return items.slice(0, Math.max(0, maxVisible));
    }

    public getHiddenAiItemsCount(items: ConsumptionAiItemManageDto[], maxVisible: number): number {
        return Math.max(0, items.length - Math.max(0, maxVisible));
    }

    public getAiSessionLabel(index: number): string {
        return this.translateService.instant('CONSUMPTION_MANAGE.ITEMS_AI_PHOTO_LABEL', { index: index + 1 });
    }

    public getAiSessionTotals(session: ConsumptionAiSessionManageDto): NutritionTotals {
        return session.items.reduce(
            (totals, item) => ({
                calories: totals.calories + (item.calories ?? 0),
                proteins: totals.proteins + (item.proteins ?? 0),
                fats: totals.fats + (item.fats ?? 0),
                carbs: totals.carbs + (item.carbs ?? 0),
                fiber: totals.fiber + (item.fiber ?? 0),
                alcohol: totals.alcohol + (item.alcohol ?? 0),
            }),
            { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 }
        );
    }

    public readonly aiPreviewMaxItems = 2;
    public readonly expandedAiSessions = signal<Set<number>>(new Set());

    public isAiSessionExpanded(index: number): boolean {
        return this.expandedAiSessions().has(index);
    }

    public toggleAiSessionExpanded(index: number): void {
        this.expandedAiSessions.update(current => {
            const next = new Set(current);
            if (next.has(index)) {
                next.delete(index);
            } else {
                next.add(index);
            }
            return next;
        });
    }

    public formatAiMacro(value: number, unitKey: string): string {
        const locale = this.translateService.currentLang || this.translateService.defaultLang || 'en';
        const hasFraction = Math.abs(value % 1) > 0.01;
        const formatter = new Intl.NumberFormat(locale, {
            maximumFractionDigits: hasFraction ? 1 : 0,
            minimumFractionDigits: hasFraction ? 1 : 0,
        });
        const unitLabel = this.translateService.instant(unitKey);
        return `${formatter.format(value)} ${unitLabel}`.trim();
    }

    private loadAiUsage(): void {
        this.aiFoodService.getUsageSummary().subscribe({
            next: usage => this.aiUsage.set(usage),
            error: error => console.error('Failed to load AI usage summary', error),
        });
    }

    public removeItem(index: number): void {
        this.items.removeAt(index);
    }

    public onItemSourceClick(index: number): void {
        const group = this.items.at(index);
        const initialType = group.controls.sourceType.value ?? ConsumptionSourceType.Product;
        this.openItemSelectDialog(index, initialType === ConsumptionSourceType.Recipe ? 'Recipe' : 'Product');
    }

    public getItemSourceName(index: number): string {
        if (this.isRecipeItem(index)) {
            return this.getRecipeName(index);
        }
        return this.getProductName(index);
    }

    public getItemSourceIcon(index: number): string {
        if (this.isRecipeItem(index) && this.items.at(index).controls.recipe.value) {
            return 'menu_book';
        }
        if (this.isProductItem(index) && this.items.at(index).controls.product.value) {
            return 'restaurant';
        }
        return 'search';
    }

    public getItemCardTitle(index: number): string {
        return this.translateService.instant('CONSUMPTION_MANAGE.ITEM_CARD_PLACEHOLDER', {
            index: index + 1,
        });
    }

    public getItemCardMeta(index: number): string | null {
        const group = this.items.at(index);
        if (group.controls.product.value) {
            return this.translateService.instant('CONSUMPTION_MANAGE.ITEM_CARD_META.PRODUCT');
        }

        if (group.controls.recipe.value) {
            return this.translateService.instant('CONSUMPTION_MANAGE.ITEM_CARD_META.RECIPE');
        }

        return null;
    }

    public getAmountPlaceholder(index: number): string {
        return this.isRecipeItem(index)
            ? 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_RECIPE'
            : 'CONSUMPTION_MANAGE.AMOUNT_PLACEHOLDER_PRODUCT';
    }

    public getControlError(controlName: keyof ConsumptionFormData): string | null {
        return this.resolveControlError(this.consumptionForm.controls[controlName]);
    }

    public getAmountControlError(index: number): string | null {
        return this.resolveControlError(this.items.at(index)?.controls.amount ?? null);
    }

    public isItemSourceInvalid(index: number): boolean {
        return this.isProductInvalid(index) || this.isRecipeInvalid(index);
    }

    public getItemSourceError(index: number): string | null {
        return this.isItemSourceInvalid(index)
            ? this.translateService.instant('CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR')
            : null;
    }

    public getSatietyLevelLabel(value: number | null): string {
        if (!value) {
            return this.translateService.instant('CONSUMPTION_MANAGE.SATIETY_NOT_SELECTED');
        }
        const title = this.translateService.instant(`HUNGER_SCALE.LEVEL_${value}.TITLE`);
        return `${value} â€” ${title}`;
    }

    public getSatietyLevelMeta(value: number | null): { label: string; description: string; gradient: string } {
        if (!value) {
            return {
                label: this.translateService.instant('CONSUMPTION_MANAGE.SATIETY_PLACEHOLDER_TITLE'),
                description: this.translateService.instant('CONSUMPTION_MANAGE.SATIETY_PLACEHOLDER_DESCRIPTION'),
                gradient: 'linear-gradient(135deg, #e2e8f0, #cbd5f5)',
            };
        }

        const config = DEFAULT_SATIETY_LEVELS.find(level => level.value === value);
        return {
            label: `${value} â€” ${this.translateService.instant(config?.titleKey ?? '')}`,
            description: this.translateService.instant(config?.descriptionKey ?? ''),
            gradient: config?.gradient ?? 'linear-gradient(135deg, #e2e8f0, #cbd5f5)',
        };
    }

    public openSatietyDialog(controlName: 'preMealSatietyLevel' | 'postMealSatietyLevel'): void {
        const control = this.consumptionForm.controls[controlName];
        if (!control) {
            return;
        }

        const titleKey =
            controlName === 'preMealSatietyLevel'
                ? 'CONSUMPTION_MANAGE.HUNGER_BEFORE_DIALOG_TITLE'
                : 'CONSUMPTION_MANAGE.HUNGER_AFTER_DIALOG_TITLE';

        const dialogRef = this.fdDialogService.open<SatietyLevelDialogComponent, SatietyLevelDialogData, number>(
            SatietyLevelDialogComponent,
            {
                size: 'lg',
                data: {
                    titleKey,
                    subtitleKey: 'CONSUMPTION_MANAGE.SATIETY_DIALOG_HINT',
                    value: control.value ?? null,
                },
            },
        );

        dialogRef
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                if (typeof value === 'number') {
                    control.setValue(value);
                    control.markAsDirty();
                    control.markAsTouched();
                }
            });
    }

    private openItemSelectDialog(index: number, initialTab: 'Product' | 'Recipe'): void {
        this.fdDialogService
            .open<ConsumptionItemSelectDialogComponent, ConsumptionItemSelectDialogData, ConsumptionItemSelection | null>(
                ConsumptionItemSelectDialogComponent,
                {
                    size: 'lg',
                    data: { initialTab },
                },
            )
            .afterClosed()
            .subscribe(selection => {
                if (!selection) {
                    return;
                }

                const group = this.items.at(index);

                if (selection.type === 'Product') {
                    group.patchValue({
                        product: selection.product,
                        recipe: null,
                    });
                    this.configureItemType(group, ConsumptionSourceType.Product);
                    return;
                }

                this.loadRecipeServingWeight(selection.recipe).subscribe();

                group.patchValue({
                    recipe: selection.recipe,
                    product: null,
                });
                this.configureItemType(group, ConsumptionSourceType.Recipe);
            });
    }

    public onSubmit(): void {
        this.markFormGroupTouched(this.consumptionForm);

        if (this.consumptionForm.invalid) {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            return;
        }

        const mealType = this.consumptionForm.controls.mealType.value;
        const comment = this.consumptionForm.controls.comment.value;
        const formItems = this.consumptionForm.controls.items.value;
        const consumptionDate = this.buildDateTime();

        const mappedItems: ConsumptionItemManageDto[] = [];

        formItems.forEach((item, index) => {
            const amountValue = Number(item.amount) || 0;
            const sourceType = item.sourceType ?? (item.recipe ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product);

            if (sourceType === ConsumptionSourceType.Product && item.product) {
                mappedItems.push({
                    productId: item.product.id,
                    recipeId: null,
                    amount: amountValue,
                });
                return;
            }

            if (sourceType === ConsumptionSourceType.Recipe && item.recipe) {
                const servingsAmount = this.convertRecipeGramsToServings(item.recipe, amountValue);
                mappedItems.push({
                    recipeId: item.recipe.id,
                    productId: null,
                    amount: servingsAmount,
                });
            }
        });

        const isNutritionAutoCalculated = this.consumptionForm.controls.isNutritionAutoCalculated.value;
        const manualTotals = this.getManualNutritionTotals();
        const preMealSatietyLevel = this.consumptionForm.controls.preMealSatietyLevel.value;
        const postMealSatietyLevel = this.consumptionForm.controls.postMealSatietyLevel.value;
        const image = this.consumptionForm.controls.imageUrl.value;

        const consumptionData: ConsumptionManageDto = {
            date: consumptionDate,
            mealType: mealType ?? undefined,
            comment: comment ?? undefined,
            imageUrl: image?.url ?? undefined,
            imageAssetId: image?.assetId ?? undefined,
            items: mappedItems,
            aiSessions: this.aiSessions(),
            isNutritionAutoCalculated,
            manualCalories: isNutritionAutoCalculated ? undefined : manualTotals.calories,
            manualProteins: isNutritionAutoCalculated ? undefined : manualTotals.proteins,
            manualFats: isNutritionAutoCalculated ? undefined : manualTotals.fats,
            manualCarbs: isNutritionAutoCalculated ? undefined : manualTotals.carbs,
            manualFiber: isNutritionAutoCalculated ? undefined : manualTotals.fiber,
            manualAlcohol: isNutritionAutoCalculated ? undefined : manualTotals.alcohol,
            preMealSatietyLevel: preMealSatietyLevel ?? undefined,
            postMealSatietyLevel: postMealSatietyLevel ?? undefined,
        };

        const consumption = this.consumption();
        consumption
            ? this.updateConsumption(consumption.id, consumptionData)
            : this.addConsumption(consumptionData);
    }

    private markFormGroupTouched(formGroup: FormGroup | FormArray): void {
        Object.values(formGroup.controls).forEach(control => {
            if (control instanceof FormGroup || control instanceof FormArray) {
                this.markFormGroupTouched(control);
            } else {
                control.markAllAsTouched();
                control.updateValueAndValidity();
            }
        });

        formGroup.markAllAsTouched();
    }

    private populateForm(consumption: Consumption): void {
        this.consumptionForm.patchValue({
            date: this.getDateInputValue(new Date(consumption.date)),
            time: this.getTimeInputValue(new Date(consumption.date)),
            mealType: consumption.mealType ?? null,
            comment: consumption.comment || null,
            imageUrl: {
                url: consumption.imageUrl ?? null,
                assetId: consumption.imageAssetId ?? null,
            },
            isNutritionAutoCalculated: consumption.isNutritionAutoCalculated,
            manualCalories: consumption.manualCalories ?? consumption.totalCalories,
            manualProteins: consumption.manualProteins ?? consumption.totalProteins,
            manualFats: consumption.manualFats ?? consumption.totalFats,
            manualCarbs: consumption.manualCarbs ?? consumption.totalCarbs,
            manualFiber: consumption.manualFiber ?? consumption.totalFiber,
            manualAlcohol: consumption.manualAlcohol ?? consumption.totalAlcohol,
            preMealSatietyLevel: consumption.preMealSatietyLevel ?? null,
            postMealSatietyLevel: consumption.postMealSatietyLevel ?? null,
        });
        this.aiSessions.set(consumption.aiSessions ?? []);

        const itemsArray = this.items;
        itemsArray.clear();

        if (consumption.items.length === 0) {
            itemsArray.push(this.createConsumptionItem());
            return;
        }

        consumption.items.forEach(item => {
            const sourceType = item.sourceType ?? (item.recipe ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product);
            const initialAmount =
                sourceType === ConsumptionSourceType.Recipe
                    ? this.convertRecipeServingsToGrams(item.recipe ?? null, item.amount ?? 0)
                    : item.amount;

            itemsArray.push(this.createConsumptionItem(
                sourceType === ConsumptionSourceType.Product ? item.product ?? null : null,
                sourceType === ConsumptionSourceType.Recipe ? item.recipe ?? null : null,
                initialAmount,
                sourceType,
            ));

            if (sourceType === ConsumptionSourceType.Recipe) {
                const currentIndex = itemsArray.length - 1;
                this.ensureRecipeWeightForExistingItem(currentIndex, item.amount ?? 0, item.recipe ?? null);
            }
        });
    }

    private updateSummary(): void {
        const autoTotals = this.calculateAutoNutritionTotals();
        const isAuto = this.consumptionForm.controls.isNutritionAutoCalculated.value;
        const summaryTotals = isAuto ? autoTotals : this.getManualNutritionTotals();
        this.applySummary({
            calories: this.roundNutrient(summaryTotals.calories),
            proteins: this.roundNutrient(summaryTotals.proteins),
            fats: this.roundNutrient(summaryTotals.fats),
            carbs: this.roundNutrient(summaryTotals.carbs),
            fiber: this.roundNutrient(summaryTotals.fiber),
            alcohol: this.roundNutrient(summaryTotals.alcohol),
        });
    }

    private calculateAutoNutritionTotals(): NutritionTotals {
        const aiTotals = this.getAiNutritionTotals();
        return this.items.controls.reduce(
            (totals, group) => {
                const sourceType = group.controls.sourceType.value;
                const amount = group.controls.amount.value || 0;

                if (sourceType === ConsumptionSourceType.Product) {
                    const food = group.controls.product.value as Product | null;
                    if (!food || food.baseAmount <= 0) {
                        return totals;
                    }
                    const multiplier = amount / food.baseAmount;
                    totals.calories += food.caloriesPerBase * multiplier;
                    totals.proteins += food.proteinsPerBase * multiplier;
                    totals.fats += food.fatsPerBase * multiplier;
                    totals.carbs += food.carbsPerBase * multiplier;
                    totals.fiber += (food.fiberPerBase ?? 0) * multiplier;
                    totals.alcohol += (food.alcoholPerBase ?? 0) * multiplier;
                    return totals;
                }

                const recipe = group.controls.recipe.value as Recipe | null;
                if (recipe && recipe.servings && recipe.servings > 0) {
                    const servings = recipe.servings <= 0 ? 1 : recipe.servings;
                    const caloriesPerServing = (recipe.totalCalories ?? 0) / servings;
                    const proteinsPerServing = (recipe.totalProteins ?? 0) / servings;
                    const fatsPerServing = (recipe.totalFats ?? 0) / servings;
                    const carbsPerServing = (recipe.totalCarbs ?? 0) / servings;
                    const fiberPerServing = (recipe.totalFiber ?? 0) / servings;
                    const alcoholPerServing = (recipe.totalAlcohol ?? 0) / servings;
                    const servingsAmount = this.convertRecipeGramsToServings(recipe, amount);

                    totals.calories += caloriesPerServing * servingsAmount;
                    totals.proteins += proteinsPerServing * servingsAmount;
                    totals.fats += fatsPerServing * servingsAmount;
                    totals.carbs += carbsPerServing * servingsAmount;
                    totals.fiber += fiberPerServing * servingsAmount;
                    totals.alcohol += alcoholPerServing * servingsAmount;
                }

                return totals;
            },
            { ...aiTotals }
        );
    }

    private getAiNutritionTotals(): NutritionTotals {
        return this.aiSessions().reduce(
            (totals, session) => session.items.reduce(
                (sessionTotals, item) => ({
                    calories: sessionTotals.calories + (item.calories ?? 0),
                    proteins: sessionTotals.proteins + (item.proteins ?? 0),
                    fats: sessionTotals.fats + (item.fats ?? 0),
                    carbs: sessionTotals.carbs + (item.carbs ?? 0),
                    fiber: sessionTotals.fiber + (item.fiber ?? 0),
                    alcohol: sessionTotals.alcohol + (item.alcohol ?? 0),
                }),
                totals,
            ),
            { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 },
        );
    }

    private getManualNutritionTotals(): NutritionTotals {
        return {
            calories: this.consumptionForm.controls.manualCalories.value ?? 0,
            proteins: this.consumptionForm.controls.manualProteins.value ?? 0,
            fats: this.consumptionForm.controls.manualFats.value ?? 0,
            carbs: this.consumptionForm.controls.manualCarbs.value ?? 0,
            fiber: this.consumptionForm.controls.manualFiber.value ?? 0,
            alcohol: this.consumptionForm.controls.manualAlcohol.value ?? 0,
        };
    }

    private applySummary(totals: NutritionTotals): void {
        if (this.totalCalories() !== totals.calories) {
            this.totalCalories.set(totals.calories);
        }

        if (this.totalFiber() !== totals.fiber) {
            this.totalFiber.set(totals.fiber);
        }

        if (this.totalAlcohol() !== totals.alcohol) {
            this.totalAlcohol.set(totals.alcohol);
        }

        const currentNutrientData = this.nutrientChartData();
        if (
            currentNutrientData.proteins !== totals.proteins ||
            currentNutrientData.fats !== totals.fats ||
            currentNutrientData.carbs !== totals.carbs
        ) {
            this.nutrientChartData.set({
                proteins: totals.proteins,
                fats: totals.fats,
                carbs: totals.carbs,
            });
        }
    }

    private populateManualNutritionFromCurrentSummary(): void {
        const totals = this.calculateAutoNutritionTotals();
        this.consumptionForm.patchValue({
            manualCalories: this.roundNutrient(totals.calories),
            manualProteins: this.roundNutrient(totals.proteins),
            manualFats: this.roundNutrient(totals.fats),
            manualCarbs: this.roundNutrient(totals.carbs),
            manualFiber: this.roundNutrient(totals.fiber),
            manualAlcohol: this.roundNutrient(totals.alcohol),
        }, { emitEvent: false });
    }

    private updateManualNutritionValidators(isAuto: boolean): void {
        const validators = isAuto ? [] : [Validators.required, Validators.min(0)];
        this.getManualNutritionControls().forEach(control => {
            control.setValidators(validators);
            control.updateValueAndValidity({ emitEvent: false });
        });
    }

    private applyAlpha(color: string, alpha: number): string {
        const trimmed = color.replace('#', '');
        const r = Number.parseInt(trimmed.slice(0, 2), 16);
        const g = Number.parseInt(trimmed.slice(2, 4), 16);
        const b = Number.parseInt(trimmed.slice(4, 6), 16);
        if ([r, g, b].some(channel => Number.isNaN(channel))) {
            return color;
        }
        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }

    private getManualNutritionControls(): Array<FormControl<number | null>> {
        return [
            this.consumptionForm.controls.manualCalories,
            this.consumptionForm.controls.manualProteins,
            this.consumptionForm.controls.manualFats,
            this.consumptionForm.controls.manualCarbs,
            this.consumptionForm.controls.manualFiber,
        ];
    }

    private async addConsumption(consumptionData: ConsumptionManageDto): Promise<void> {
        this.consumptionService.create(consumptionData).subscribe({
            next: response => this.handleSubmitResponse(response),
            error: error => this.handleSubmitError(error),
        });
    }

    private async updateConsumption(id: string, consumptionData: ConsumptionManageDto): Promise<void> {
        this.consumptionService.update(id, consumptionData).subscribe({
            next: response => this.handleSubmitResponse(response),
            error: error => this.handleSubmitError(error),
        });
    }

    private async handleSubmitResponse(response: Consumption | null): Promise<void> {
        if (response) {
                if (!this.consumption()) {
                    this.consumptionForm.reset({
                        date: this.getDateInputValue(new Date()),
                        time: this.getTimeInputValue(new Date()),
                        mealType: null,
                        comment: null,
                        isNutritionAutoCalculated: true,
                        manualCalories: null,
                        manualProteins: null,
                        manualFats: null,
                        manualCarbs: null,
                        manualFiber: null,
                        manualAlcohol: null,
                    });
                    this.items.clear();
                    this.items.push(this.createConsumptionItem());
                    this.aiSessions.set([]);
                    this.updateSummary();
                }
            await this.showConfirmDialog();
        } else {
            this.handleSubmitError();
        }
    }

    private handleSubmitError(error?: HttpErrorResponse): void {
        this.setGlobalError('FORM_ERRORS.UNKNOWN');
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private clearGlobalError(): void {
        this.globalError.set(null);
    }

    private showConfirmDialog(): void {
        this.fdDialogService
            .open<
                ConsumptionManageSuccessDialogComponent,
                ConsumptionManageSuccessDialogData,
                ConsumptionManageRedirectAction
            >(ConsumptionManageSuccessDialogComponent, {
                size: 'sm',
                data: {
                    isEdit: Boolean(this.consumption()),
                },
            })
            .afterClosed()
            .subscribe(redirectAction => {
                if (redirectAction === 'Home') {
                    this.navigationService.navigateToHome();
                } else if (redirectAction === 'ConsumptionList') {
                    this.navigationService.navigateToConsumptionList();
                }
            });
    }

    private configureItemType(
        group: FormGroup<ConsumptionItemFormData>,
        type: ConsumptionSourceType,
        clearSelection: boolean = false,
    ): void {
        group.controls.sourceType.setValue(type);

        if (type === ConsumptionSourceType.Product) {
            group.controls.product.setValidators([Validators.required]);
            group.controls.recipe.clearValidators();
            if (clearSelection) {
                group.controls.recipe.setValue(null);
            }
        } else {
            group.controls.recipe.setValidators([Validators.required]);
            group.controls.product.clearValidators();
            if (clearSelection) {
                group.controls.product.setValue(null);
            }
        }

        group.controls.product.updateValueAndValidity();
        group.controls.recipe.updateValueAndValidity();

        if (clearSelection) {
            group.controls.amount.setValue(null);
            group.controls.amount.markAsUntouched();
        }

        this.updateAmountControlState(group);
    }

    private createConsumptionItem(
        product: Product | null = null,
        recipe: Recipe | null = null,
        amount: number | null = null,
        sourceType: ConsumptionSourceType = ConsumptionSourceType.Product,
    ): FormGroup<ConsumptionItemFormData> {
        const group = new FormGroup<ConsumptionItemFormData>({
        sourceType: new FormControl<ConsumptionSourceType>(sourceType, { nonNullable: true }),
        product: new FormControl<Product | null>(product),
        recipe: new FormControl<Recipe | null>(recipe),
        amount: new FormControl<number | null>(amount, [Validators.required, Validators.min(0.01)]),
        });

        this.configureItemType(group, sourceType);
        this.updateAmountControlState(group);
        return group;
    }

    private updateAmountControlState(group: FormGroup<ConsumptionItemFormData>): void {
        const shouldDisable = !group.controls.product.value && !group.controls.recipe.value;
        if (shouldDisable && group.controls.amount.enabled) {
            group.controls.amount.disable({ emitEvent: false });
            return;
        }

        if (!shouldDisable && group.controls.amount.disabled) {
            group.controls.amount.enable({ emitEvent: false });
        }
    }

    private ensureRecipeWeightForExistingItem(index: number, servingsAmount: number, recipe: Recipe | null): void {
        if (!recipe) {
            return;
        }

        this.loadRecipeServingWeight(recipe).subscribe(servingWeight => {
            if (!servingWeight || servingsAmount == null) {
                return;
            }

            const grams = servingsAmount * servingWeight;
            const group = this.items.at(index);
            group.controls.amount.setValue(grams);
        });
    }

    private convertRecipeServingsToGrams(recipe: Recipe | null, servingsAmount: number): number {
        const servingWeight = recipe?.id ? this.recipeServingWeightCache.get(recipe.id) : null;
        if (servingWeight && servingWeight > 0) {
            return servingsAmount * servingWeight;
        }
        return servingsAmount;
    }

    private convertRecipeGramsToServings(recipe: Recipe | null, grams: number): number {
        const servingWeight = recipe?.id ? this.recipeServingWeightCache.get(recipe.id) : null;
        if (servingWeight && servingWeight > 0) {
            return grams / servingWeight;
        }
        return grams;
    }

    private buildMealTypeOptions(): void {
        this.mealTypeSelectOptions = this.mealTypeOptions.map(option => ({
            value: option,
            label: this.translateService.instant('MEAL_TYPES.' + option),
        }));
    }

    private getDateInputValue(date: Date): string {
        const year = date.getFullYear();
        const month = this.padNumber(date.getMonth() + 1);
        const day = this.padNumber(date.getDate());
        return `${year}-${month}-${day}`;
    }

    private getTimeInputValue(date: Date): string {
        const hours = this.padNumber(date.getHours());
        const minutes = this.padNumber(date.getMinutes());
        return `${hours}:${minutes}`;
    }

    private buildDateTime(): Date {
        const dateValue = this.consumptionForm.controls.date.value;
        const timeValue = this.consumptionForm.controls.time.value;
        const datePart = dateValue ?? this.getDateInputValue(new Date());
        const timePart = timeValue ?? this.getTimeInputValue(new Date());
        const combined = `${datePart}T${timePart}`;
        const parsed = new Date(combined);
        return Number.isNaN(parsed.getTime()) ? new Date() : parsed;
    }

    private padNumber(value: number): string {
        return value.toString().padStart(2, '0');
    }

    private roundNutrient(value: number): number {
        return Math.round(value * 100) / 100;
    }

    private loadRecipeServingWeight(recipe: Recipe | null): Observable<number | null> {
        if (!recipe || !recipe.id) {
            return of(null);
        }

        const cached = this.recipeServingWeightCache.get(recipe.id);
        if (cached !== undefined) {
            return of(cached);
        }

        const immediateWeight = this.calculateRecipeWeight(recipe);
        if (immediateWeight && recipe.servings > 0) {
            const servingWeight = immediateWeight / recipe.servings;
            this.recipeServingWeightCache.set(recipe.id, servingWeight);
            return of(servingWeight);
        }

        return this.recipeService.getById(recipe.id).pipe(
            map(fullRecipe => {
                const computedWeight = this.calculateRecipeWeight(fullRecipe);
                if (computedWeight && fullRecipe.servings > 0) {
                    const servingWeight = computedWeight / fullRecipe.servings;
                    this.recipeServingWeightCache.set(recipe.id, servingWeight);
                    return servingWeight;
                }
                this.recipeServingWeightCache.set(recipe.id, null);
                return null;
            }),
            catchError(() => {
                this.recipeServingWeightCache.set(recipe.id, null);
                return of(null);
            }),
        );
    }

    private calculateRecipeWeight(recipe: Recipe): number | null {
        if (!recipe.steps || recipe.steps.length === 0) {
            return null;
        }

        let total = 0;
        recipe.steps.forEach(step => {
            step.ingredients?.forEach(ingredient => {
                const weight = this.calculateIngredientWeight(ingredient);
                if (weight) {
                    total += weight;
                }
            });
        });

        return total > 0 ? total : null;
    }

    private calculateIngredientWeight(ingredient: RecipeIngredient): number | null {
        const amount = ingredient.amount ?? 0;
        if (amount <= 0) {
            return null;
        }

        const unitRaw = ingredient.productBaseUnit?.toString().toUpperCase();
        if (!unitRaw) {
            return null;
        }

        if (unitRaw === MeasurementUnit.G) {
            return amount;
        }

        if (unitRaw === MeasurementUnit.ML) {
            return amount;
        }

        return null;
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (!control || !control.invalid || !control.touched) {
            return null;
        }

        if (control.errors?.['required']) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (control.errors?.['min']) {
            const min = control.errors['min'].min ?? 0;
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }
}

type ConsumptionFormValues = {
    date: string;
    time: string;
    mealType: string | null;
    items: ConsumptionItemFormValues[];
    comment: string | null;
    imageUrl: ImageSelection | null;
    isNutritionAutoCalculated: boolean;
    manualCalories: number | null;
    manualProteins: number | null;
    manualFats: number | null;
    manualCarbs: number | null;
    manualFiber: number | null;
    manualAlcohol: number | null;
    preMealSatietyLevel: number | null;
    postMealSatietyLevel: number | null;
};

type ConsumptionItemFormValues = {
    sourceType: ConsumptionSourceType;
    product: Product | null;
    recipe: Recipe | null;
    amount: number | null;
};

type ConsumptionFormData = FormGroupControls<ConsumptionFormValues>;

type ConsumptionItemFormData = FormGroupControls<ConsumptionItemFormValues>;

type NutritionTotals = {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
};


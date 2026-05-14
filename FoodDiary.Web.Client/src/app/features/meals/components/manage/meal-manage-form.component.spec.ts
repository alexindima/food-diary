import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type AbstractControl, FormArray, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { EMPTY, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../../services/navigation.service';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
    createEmptyProductSnapshot,
} from '../../models/meal.data';
import { MealManageFormComponent } from './meal-manage-form.component';
import type { ConsumptionItemFormData, MealNutritionSummaryState, NutritionTotals } from './meal-manage-lib/meal-manage.types';

const PRODUCT_AMOUNT = 150;
const TOTAL_CALORIES = 300;
const UPDATED_TOTAL_CALORIES = 450;
const EMPTY_TOTALS: NutritionTotals = {
    calories: 0,
    proteins: 0,
    fats: 0,
    carbs: 0,
    fiber: 0,
    alcohol: 0,
};

type MealManageFacadeMock = {
    addAiSession: ReturnType<typeof vi.fn>;
    buildNutritionSummaryState: ReturnType<typeof vi.fn>;
    convertRecipeGramsToServings: ReturnType<typeof vi.fn>;
    convertRecipeServingsToGrams: ReturnType<typeof vi.fn>;
    createConsumptionItem: ReturnType<typeof vi.fn>;
    createItemsValidator: ReturnType<typeof vi.fn>;
    ensurePremiumAccess: ReturnType<typeof vi.fn>;
    getManualNutritionTotals: ReturnType<typeof vi.fn>;
    openEditAiPhotoSessionDialogAsync: ReturnType<typeof vi.fn>;
    removeAiSession: ReturnType<typeof vi.fn>;
    replaceAiSession: ReturnType<typeof vi.fn>;
    showSuccessRedirectAsync: ReturnType<typeof vi.fn>;
    submitConsumptionAsync: ReturnType<typeof vi.fn>;
    syncManualNutritionFromTotals: ReturnType<typeof vi.fn>;
    updateItemValidationRules: ReturnType<typeof vi.fn>;
    updateManualNutritionValidators: ReturnType<typeof vi.fn>;
};

type MealManageFormSetup = {
    component: MealManageFormComponent;
    fixture: ComponentFixture<MealManageFormComponent>;
    mealManageFacade: MealManageFacadeMock;
    navigationService: {
        navigateToConsumptionListAsync: ReturnType<typeof vi.fn>;
    };
};

describe('MealManageFormComponent input behavior', () => {
    it('should repopulate form when consumption with the same id is refreshed', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.buildNutritionSummaryState
            .mockReturnValueOnce(createNutritionSummaryStateWithCalories(TOTAL_CALORIES))
            .mockReturnValueOnce(createNutritionSummaryStateWithCalories(UPDATED_TOTAL_CALORIES));

        fixture.componentRef.setInput('consumption', createConsumption({ totalCalories: TOTAL_CALORIES }));
        fixture.detectChanges();
        expect(component.consumptionForm.controls.comment.value).toBe('Comment');

        fixture.componentRef.setInput('consumption', createConsumption({ totalCalories: UPDATED_TOTAL_CALORIES }));
        fixture.detectChanges();

        expect(component.consumptionForm.controls.comment.value).toBe('Updated comment');
    });
});

describe('MealManageFormComponent submit behavior', () => {
    it('should submit create DTO and reset add form after successful create', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.submitConsumptionAsync.mockResolvedValue(createConsumption({ totalCalories: TOTAL_CALORIES }));
        component.consumptionForm.patchValue({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'Breakfast',
            comment: 'Lunch',
        });
        component.items.at(0).patchValue({
            sourceType: ConsumptionSourceType.Product,
            product: { ...createEmptyProductSnapshot(), id: 'product-1', name: 'Apple' },
            amount: PRODUCT_AMOUNT,
        });
        clearValidators(component.consumptionForm);
        component.consumptionForm.updateValueAndValidity();
        expect(component.consumptionForm.valid).toBe(true);

        component.onSubmit();
        await Promise.resolve();

        expect(mealManageFacade.submitConsumptionAsync).toHaveBeenCalledWith(
            null,
            expect.objectContaining<Partial<ConsumptionManageDto>>({
                mealType: 'BREAKFAST',
                comment: 'Lunch',
                isNutritionAutoCalculated: true,
                items: [{ productId: 'product-1', recipeId: null, amount: PRODUCT_AMOUNT }],
            }),
        );
        expect(mealManageFacade.showSuccessRedirectAsync).toHaveBeenCalledWith(false);
        expect(component.aiSessions()).toEqual([]);
        expect(component.items.length).toBe(1);
    });

    it('should show global error and skip submit when form is invalid', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        component.consumptionForm.controls.date.setValue('');

        component.onSubmit();
        await Promise.resolve();

        expect(mealManageFacade.submitConsumptionAsync).not.toHaveBeenCalled();
        expect(component.globalError()).toBe('FORM_ERRORS.UNKNOWN');
    });
});

describe('MealManageFormComponent item and AI behavior', () => {
    it('should open manual item dialog for a reusable empty item', async () => {
        const { component } = await setupComponentAsync();

        component.addConsumptionItem();
        await Promise.resolve();

        expect(component.items.length).toBe(1);
    });

    it('should append AI sessions and remove them by index', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const session: ConsumptionAiSessionManageDto = { notes: 'recognized', items: [] };
        mealManageFacade.addAiSession.mockReturnValue([session]);
        mealManageFacade.removeAiSession.mockReturnValue([]);

        component.onAiMealRecognized({
            source: 'Photo',
            imageAssetId: null,
            imageUrl: null,
            recognizedAtUtc: '2026-04-05T10:30:00Z',
            notes: 'recognized',
            items: [],
        });
        expect(component.aiSessions()).toEqual([session]);

        component.onDeleteAiSession(0);

        expect(component.aiSessions()).toEqual([]);
    });
});

describe('MealManageFormComponent navigation', () => {
    it('should navigate to consumption list on cancel', async () => {
        const { component, navigationService } = await setupComponentAsync();

        await component.onCancelAsync();

        expect(navigationService.navigateToConsumptionListAsync).toHaveBeenCalled();
    });
});

async function setupComponentAsync(): Promise<MealManageFormSetup> {
    const mealManageFacade = createMealManageFacadeMock();
    const navigationService = {
        navigateToConsumptionListAsync: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
        imports: [MealManageFormComponent, TranslateModule.forRoot()],
        providers: [
            { provide: MealManageFacade, useValue: mealManageFacade },
            { provide: NavigationService, useValue: navigationService },
            {
                provide: FdUiDialogService,
                useValue: {
                    open: vi.fn().mockReturnValue({ afterClosed: () => of(true) }),
                },
            },
            {
                provide: Router,
                useValue: {
                    currentNavigation: vi.fn().mockReturnValue(null),
                    events: EMPTY,
                },
            },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        queryParamMap: {
                            get: vi.fn().mockReturnValue(null),
                        },
                    },
                },
            },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealManageFormComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
        mealManageFacade,
        navigationService,
    };
}

function createMealManageFacadeMock(): MealManageFacadeMock {
    return {
        addAiSession: vi.fn((_sessions: ConsumptionAiSessionManageDto[], session: ConsumptionAiSessionManageDto) => [session]),
        buildNutritionSummaryState: vi.fn((_form, _items, _aiSessions, _threshold) => createNutritionSummaryState()),
        convertRecipeGramsToServings: vi.fn((_recipe, amount: number) => amount),
        convertRecipeServingsToGrams: vi.fn((_recipe, amount: number) => amount),
        createConsumptionItem: vi.fn(createConsumptionItemGroup),
        createItemsValidator: vi.fn(() => Validators.nullValidator),
        ensurePremiumAccess: vi.fn().mockReturnValue(true),
        getManualNutritionTotals: vi.fn().mockReturnValue(EMPTY_TOTALS),
        openEditAiPhotoSessionDialogAsync: vi.fn().mockResolvedValue(null),
        removeAiSession: vi.fn((sessions: ConsumptionAiSessionManageDto[], index: number) =>
            sessions.filter((_session, currentIndex) => currentIndex !== index),
        ),
        replaceAiSession: vi.fn(),
        showSuccessRedirectAsync: vi.fn().mockResolvedValue(undefined),
        submitConsumptionAsync: vi.fn().mockResolvedValue(null),
        syncManualNutritionFromTotals: vi.fn(),
        updateItemValidationRules: vi.fn(),
        updateManualNutritionValidators: vi.fn(),
    };
}

function createConsumptionItemGroup(): FormGroup<ConsumptionItemFormData> {
    return new FormGroup<ConsumptionItemFormData>({
        sourceType: new FormControl(ConsumptionSourceType.Product, { nonNullable: true }),
        product: new FormControl(null),
        recipe: new FormControl(null),
        amount: new FormControl(null),
    });
}

function createNutritionSummaryState(): MealNutritionSummaryState {
    return createNutritionSummaryStateWithCalories(0);
}

function createNutritionSummaryStateWithCalories(calories: number): MealNutritionSummaryState {
    return {
        autoTotals: EMPTY_TOTALS,
        summaryTotals: {
            ...EMPTY_TOTALS,
            calories,
        },
        warning: null,
    };
}

function createConsumption(overrides: Partial<Consumption> = {}): Consumption {
    const totalCalories = overrides.totalCalories ?? TOTAL_CALORIES;
    return {
        id: 'consumption-1',
        date: '2026-04-05T10:30:00',
        mealType: 'Breakfast',
        comment: totalCalories === UPDATED_TOTAL_CALORIES ? 'Updated comment' : 'Comment',
        totalCalories,
        totalProteins: 20,
        totalFats: 10,
        totalCarbs: 30,
        totalFiber: 5,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        items: [],
        ...overrides,
    };
}

function clearValidators(control: AbstractControl): void {
    control.clearValidators();
    control.clearAsyncValidators();

    if (control instanceof FormGroup || control instanceof FormArray) {
        Object.values(control.controls).forEach(child => {
            clearValidators(child);
        });
    }

    control.updateValueAndValidity({ emitEvent: false });
}

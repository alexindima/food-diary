import { HttpErrorResponse } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { EMPTY, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { NavigationService } from '../../../../services/navigation.service';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import {
    createEmptyProductSnapshot,
    type Meal,
    type MealAiSessionManageDto,
    type MealManageDto,
    MealSourceType,
} from '../../models/meal.data';
import { MealManageFormComponent } from './meal-manage-form';
import type { MealItemFormValues, MealNutritionSummaryState, NutritionTotals } from './meal-manage-lib/meal-manage.types';
import { createMealItemValue } from './meal-manage-lib/meal-manage-form.mapper';

const PRODUCT_AMOUNT = 150;
const TOTAL_CALORIES = 300;
const UPDATED_TOTAL_CALORIES = 450;
const NORMALIZED_SATIETY_LEVEL = 5;
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
    buildManualNutritionPatchFromTotals: ReturnType<typeof vi.fn>;
    buildNutritionSummaryStateFromValues: ReturnType<typeof vi.fn>;
    convertRecipeGramsToServings: ReturnType<typeof vi.fn>;
    convertRecipeServingsToGrams: ReturnType<typeof vi.fn>;
    configureItemType: ReturnType<typeof vi.fn>;
    confirmDiscardChangesAsync: ReturnType<typeof vi.fn>;
    createMealItem: ReturnType<typeof vi.fn>;
    ensurePremiumAccess: ReturnType<typeof vi.fn>;
    getManualNutritionTotalsFromValue: ReturnType<typeof vi.fn>;
    openEditAiPhotoSessionDialogAsync: ReturnType<typeof vi.fn>;
    removeAiSession: ReturnType<typeof vi.fn>;
    replaceAiSession: ReturnType<typeof vi.fn>;
    showSuccessToastAndRedirectAsync: ReturnType<typeof vi.fn>;
    submitMealAsync: ReturnType<typeof vi.fn>;
};

type MealManageFormSetup = {
    component: MealManageFormComponent;
    fixture: ComponentFixture<MealManageFormComponent>;
    mealManageFacade: MealManageFacadeMock;
    navigationService: {
        navigateToMealListAsync: ReturnType<typeof vi.fn>;
    };
};

describe('MealManageFormComponent input behavior', () => {
    it('should repopulate form when meal with the same id is refreshed', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.buildNutritionSummaryStateFromValues
            .mockReturnValueOnce(createNutritionSummaryStateWithCalories(TOTAL_CALORIES))
            .mockReturnValueOnce(createNutritionSummaryStateWithCalories(UPDATED_TOTAL_CALORIES));

        fixture.componentRef.setInput('meal', createMeal({ totalCalories: TOTAL_CALORIES }));
        fixture.detectChanges();
        expect(component['mealFormModel']().comment).toBe('Comment');

        fixture.componentRef.setInput('meal', createMeal({ totalCalories: UPDATED_TOTAL_CALORIES }));
        fixture.detectChanges();

        expect(component['mealFormModel']().comment).toBe('Updated comment');
    });
});

describe('MealManageFormComponent native submit behavior', () => {
    it('should prevent native form submit when saving', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.submitMealAsync.mockResolvedValue(createMeal({ totalCalories: TOTAL_CALORIES }));
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: 'product-1', name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
    });
});

describe('MealManageFormComponent submit behavior', () => {
    it('should submit create DTO and reset add form after successful create', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.submitMealAsync.mockResolvedValue(createMeal({ totalCalories: TOTAL_CALORIES }));
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            comment: 'Lunch',
        });
        component['patchMealFormModel']({
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: 'product-1', name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledWith(
            null,
            expect.objectContaining<Partial<MealManageDto>>({
                mealType: 'BREAKFAST',
                comment: 'Lunch',
                isNutritionAutoCalculated: true,
                items: [{ productId: 'product-1', recipeId: null, amount: PRODUCT_AMOUNT, origin: 'Manual' }],
            }),
        );
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).toHaveBeenCalledWith(false);
        expect(component['aiSessions']()).toEqual([]);
        expect(component['items'].length).toBe(1);
    });

    it('should show global error and skip submit when form is invalid', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        component['patchMealFormModel']({
            date: '',
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: 'product-1', name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect(component['globalError']()).toBe('FORM_ERRORS.UNKNOWN');
    });

    it('should show backend validation message when submit fails', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const serverMessage = 'Product is not accessible.';
        mealManageFacade.submitMealAsync.mockRejectedValue(new HttpErrorResponse({ error: { message: serverMessage } }));
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
        });
        component['patchMealFormModel']({
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: 'product-1', name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();
        await waitForAsyncTasksAsync();

        expect(component['globalError']()).toBe(serverMessage);
    });
});

describe('MealManageFormComponent item validation', () => {
    it('should show a specific error and skip submit when no items are added', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            items: [],
        });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect(component['globalError']()).toBe('FORM_ERRORS.NON_EMPTY_ARRAY');
    });
});

describe('MealManageFormComponent duplicate submit guard', () => {
    it('should ignore repeated submit while create is in progress', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        let resolveSubmit: ((value: Meal) => void) | undefined;
        mealManageFacade.submitMealAsync.mockReturnValue(
            new Promise<Meal>(resolve => {
                resolveSubmit = resolve;
            }),
        );
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: 'product-1', name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        const firstSubmit = component['onSubmitAsync']();
        await component['onSubmitAsync']();

        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
        expect(component['isSubmitting']()).toBe(true);

        resolveSubmit?.(createMeal({ totalCalories: TOTAL_CALORIES }));
        await firstSubmit;

        expect(component['isSubmitting']()).toBe(false);
    });
});

describe('MealManageFormComponent item and AI behavior', () => {
    it('should open manual item dialog for a reusable empty item', async () => {
        const { component } = await setupComponentAsync();

        component['addMealItem']();
        await waitForAsyncTasksAsync();

        expect(component['items'].length).toBe(1);
    });

    it('should append AI sessions and remove them by index', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const session: MealAiSessionManageDto = { notes: 'recognized', items: [] };
        mealManageFacade.addAiSession.mockReturnValue([session]);
        mealManageFacade.removeAiSession.mockReturnValue([]);

        component['onAiMealRecognized']({
            source: 'Photo',
            imageAssetId: null,
            imageUrl: null,
            recognizedAtUtc: '2026-04-05T10:30:00Z',
            notes: 'recognized',
            items: [],
        });
        expect(component['aiSessions']()).toEqual([session]);

        component['onDeleteAiSession'](0);

        expect(component['aiSessions']()).toEqual([]);
    });

    it('should skip AI session edit when premium access is rejected', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.ensurePremiumAccess.mockReturnValueOnce(false);
        component['aiSessions'].set([{ notes: 'recognized', items: [] }]);

        component['onEditAiSession'](0);
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.openEditAiPhotoSessionDialogAsync).not.toHaveBeenCalled();
        expect(mealManageFacade.replaceAiSession).not.toHaveBeenCalled();
    });

    it('should replace AI session after successful edit', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const session: MealAiSessionManageDto = { notes: 'recognized', items: [] };
        const updatedSession: MealAiSessionManageDto = { notes: 'updated', items: [] };
        component['aiSessions'].set([session]);
        mealManageFacade.openEditAiPhotoSessionDialogAsync.mockResolvedValueOnce(updatedSession);
        mealManageFacade.replaceAiSession.mockReturnValueOnce([updatedSession]);

        component['onEditAiSession'](0);
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.openEditAiPhotoSessionDialogAsync).toHaveBeenCalledWith(session);
        expect(component['aiSessions']()).toEqual([updatedSession]);
    });
});

describe('MealManageFormComponent nutrition and satiety behavior', () => {
    it('should switch to manual nutrition and populate manual values from current totals', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.buildNutritionSummaryStateFromValues.mockReturnValue(createNutritionSummaryStateWithCalories(TOTAL_CALORIES));

        component['onNutritionModeChange']('manual');

        expect(component['nutritionMode']()).toBe('manual');
        expect(component['mealFormModel']().isNutritionAutoCalculated).toBe(false);
        expect(component['mealFormModel']().manualCalories).toBe(TOTAL_CALORIES);
    });

    it('should normalize satiety level changes and mark control dirty', async () => {
        const { component } = await setupComponentAsync();

        component['onSatietyLevelChange']('preMealSatietyLevel', NORMALIZED_SATIETY_LEVEL);

        expect(component['mealFormModel']().preMealSatietyLevel).toBe(NORMALIZED_SATIETY_LEVEL);
        expect(component['preMealSatietyLevel']()).toBe(NORMALIZED_SATIETY_LEVEL);
    });
});

describe('MealManageFormComponent navigation', () => {
    it('should navigate to meal list on cancel', async () => {
        const { component, navigationService } = await setupComponentAsync();

        await component['onCancelAsync']();

        expect(navigationService.navigateToMealListAsync).toHaveBeenCalled();
    });

    it('should stay on the form when discarding dirty changes is cancelled', async () => {
        const { component, mealManageFacade, navigationService } = await setupComponentAsync();
        component['mealSignalForm'].comment().markAsDirty();
        mealManageFacade.confirmDiscardChangesAsync.mockResolvedValueOnce(false);

        await component['onCancelAsync']();

        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        expect(navigationService.navigateToMealListAsync).not.toHaveBeenCalled();
    });

    it('should leave the form when discarding dirty changes is confirmed', async () => {
        const { component, mealManageFacade, navigationService } = await setupComponentAsync();
        component['mealSignalForm'].comment().markAsDirty();

        await component['onCancelAsync']();

        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        expect(navigationService.navigateToMealListAsync).toHaveBeenCalledOnce();
    });
});

async function setupComponentAsync(): Promise<MealManageFormSetup> {
    const mealManageFacade = createMealManageFacadeMock();
    const navigationService = {
        navigateToMealListAsync: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
        imports: [MealManageFormComponent],
        providers: [
            provideTranslateTesting(),
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
        addAiSession: vi.fn((_sessions: MealAiSessionManageDto[], session: MealAiSessionManageDto) => [session]),
        buildManualNutritionPatchFromTotals: vi.fn((totals: NutritionTotals) => ({
            manualCalories: totals.calories,
            manualProteins: totals.proteins,
            manualFats: totals.fats,
            manualCarbs: totals.carbs,
            manualFiber: totals.fiber,
            manualAlcohol: totals.alcohol,
        })),
        buildNutritionSummaryStateFromValues: vi.fn((_formValue, _aiSessions, _threshold) => createNutritionSummaryState()),
        confirmDiscardChangesAsync: vi.fn().mockResolvedValue(true),
        configureItemType: vi.fn((item: MealItemFormValues) => item),
        convertRecipeGramsToServings: vi.fn((_recipe, amount: number) => amount),
        convertRecipeServingsToGrams: vi.fn((_recipe, amount: number) => amount),
        createMealItem: vi.fn(() => createMealItemValue()),
        ensurePremiumAccess: vi.fn().mockReturnValue(true),
        getManualNutritionTotalsFromValue: vi.fn().mockReturnValue(EMPTY_TOTALS),
        openEditAiPhotoSessionDialogAsync: vi.fn().mockResolvedValue(null),
        removeAiSession: vi.fn((sessions: MealAiSessionManageDto[], index: number) =>
            sessions.filter((_session, currentIndex) => currentIndex !== index),
        ),
        replaceAiSession: vi.fn(),
        showSuccessToastAndRedirectAsync: vi.fn().mockResolvedValue(void 0),
        submitMealAsync: vi.fn().mockResolvedValue(null),
    };
}

function createNutritionSummaryState(): MealNutritionSummaryState {
    return createNutritionSummaryStateWithCalories(0);
}

function createNutritionSummaryStateWithCalories(calories: number): MealNutritionSummaryState {
    return {
        autoTotals: {
            ...EMPTY_TOTALS,
            calories,
        },
        summaryTotals: {
            ...EMPTY_TOTALS,
            calories,
        },
        warning: null,
    };
}

function createMeal(overrides: Partial<Meal> = {}): Meal {
    const totalCalories = overrides.totalCalories ?? TOTAL_CALORIES;
    return {
        id: 'meal-1',
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

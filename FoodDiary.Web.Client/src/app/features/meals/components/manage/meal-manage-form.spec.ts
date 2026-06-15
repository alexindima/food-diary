import { HttpErrorResponse } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { EMPTY, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { NavigationService } from '../../../../services/navigation.service';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
    createEmptyProductSnapshot,
} from '../../models/meal.data';
import { MealManageFormComponent } from './meal-manage-form';
import type { ConsumptionItemFormValues, MealNutritionSummaryState, NutritionTotals } from './meal-manage-lib/meal-manage.types';
import { createConsumptionItemValue } from './meal-manage-lib/meal-manage-form.mapper';

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
    createConsumptionItem: ReturnType<typeof vi.fn>;
    ensurePremiumAccess: ReturnType<typeof vi.fn>;
    getManualNutritionTotalsFromValue: ReturnType<typeof vi.fn>;
    openEditAiPhotoSessionDialogAsync: ReturnType<typeof vi.fn>;
    removeAiSession: ReturnType<typeof vi.fn>;
    replaceAiSession: ReturnType<typeof vi.fn>;
    showSuccessRedirectAsync: ReturnType<typeof vi.fn>;
    submitConsumptionAsync: ReturnType<typeof vi.fn>;
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
        mealManageFacade.buildNutritionSummaryStateFromValues
            .mockReturnValueOnce(createNutritionSummaryStateWithCalories(TOTAL_CALORIES))
            .mockReturnValueOnce(createNutritionSummaryStateWithCalories(UPDATED_TOTAL_CALORIES));

        fixture.componentRef.setInput('consumption', createConsumption({ totalCalories: TOTAL_CALORIES }));
        fixture.detectChanges();
        expect(component['consumptionFormModel']().comment).toBe('Comment');

        fixture.componentRef.setInput('consumption', createConsumption({ totalCalories: UPDATED_TOTAL_CALORIES }));
        fixture.detectChanges();

        expect(component['consumptionFormModel']().comment).toBe('Updated comment');
    });
});

describe('MealManageFormComponent submit behavior', () => {
    it('should submit create DTO and reset add form after successful create', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.submitConsumptionAsync.mockResolvedValue(createConsumption({ totalCalories: TOTAL_CALORIES }));
        component['patchConsumptionFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            comment: 'Lunch',
        });
        component['patchConsumptionFormModel']({
            items: [
                createConsumptionItemValue(
                    { ...createEmptyProductSnapshot(), id: 'product-1', name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    ConsumptionSourceType.Product,
                ),
            ],
        });

        component['onSubmit']();
        await Promise.resolve();

        expect(mealManageFacade.submitConsumptionAsync).toHaveBeenCalledWith(
            null,
            expect.objectContaining<Partial<ConsumptionManageDto>>({
                mealType: 'BREAKFAST',
                comment: 'Lunch',
                isNutritionAutoCalculated: true,
                items: [{ productId: 'product-1', recipeId: null, amount: PRODUCT_AMOUNT, origin: 'Manual' }],
            }),
        );
        expect(mealManageFacade.showSuccessRedirectAsync).toHaveBeenCalledWith(false);
        expect(component['aiSessions']()).toEqual([]);
        expect(component['items'].length).toBe(1);
    });

    it('should show global error and skip submit when form is invalid', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        component['patchConsumptionFormModel']({ date: '' });

        component['onSubmit']();
        await Promise.resolve();

        expect(mealManageFacade.submitConsumptionAsync).not.toHaveBeenCalled();
        expect(component['globalError']()).toBe('FORM_ERRORS.UNKNOWN');
    });

    it('should show backend validation message when submit fails', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const serverMessage = 'Product is not accessible.';
        mealManageFacade.submitConsumptionAsync.mockRejectedValue(new HttpErrorResponse({ error: { message: serverMessage } }));
        component['patchConsumptionFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
        });
        component['patchConsumptionFormModel']({
            items: [
                createConsumptionItemValue(
                    { ...createEmptyProductSnapshot(), id: 'product-1', name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    ConsumptionSourceType.Product,
                ),
            ],
        });

        component['onSubmit']();
        await Promise.resolve();
        await Promise.resolve();

        expect(component['globalError']()).toBe(serverMessage);
    });
});

describe('MealManageFormComponent item and AI behavior', () => {
    it('should open manual item dialog for a reusable empty item', async () => {
        const { component } = await setupComponentAsync();

        component['addConsumptionItem']();
        await Promise.resolve();

        expect(component['items'].length).toBe(1);
    });

    it('should append AI sessions and remove them by index', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const session: ConsumptionAiSessionManageDto = { notes: 'recognized', items: [] };
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
        await Promise.resolve();

        expect(mealManageFacade.openEditAiPhotoSessionDialogAsync).not.toHaveBeenCalled();
        expect(mealManageFacade.replaceAiSession).not.toHaveBeenCalled();
    });

    it('should replace AI session after successful edit', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const session: ConsumptionAiSessionManageDto = { notes: 'recognized', items: [] };
        const updatedSession: ConsumptionAiSessionManageDto = { notes: 'updated', items: [] };
        component['aiSessions'].set([session]);
        mealManageFacade.openEditAiPhotoSessionDialogAsync.mockResolvedValueOnce(updatedSession);
        mealManageFacade.replaceAiSession.mockReturnValueOnce([updatedSession]);

        component['onEditAiSession'](0);
        await Promise.resolve();

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
        expect(component['consumptionFormModel']().isNutritionAutoCalculated).toBe(false);
        expect(component['consumptionFormModel']().manualCalories).toBe(TOTAL_CALORIES);
    });

    it('should normalize satiety level changes and mark control dirty', async () => {
        const { component } = await setupComponentAsync();

        component['onSatietyLevelChange']('preMealSatietyLevel', NORMALIZED_SATIETY_LEVEL);

        expect(component['consumptionFormModel']().preMealSatietyLevel).toBe(NORMALIZED_SATIETY_LEVEL);
        expect(component['preMealSatietyLevel']()).toBe(NORMALIZED_SATIETY_LEVEL);
    });
});

describe('MealManageFormComponent navigation', () => {
    it('should navigate to consumption list on cancel', async () => {
        const { component, navigationService } = await setupComponentAsync();

        await component['onCancelAsync']();

        expect(navigationService.navigateToConsumptionListAsync).toHaveBeenCalled();
    });
});

async function setupComponentAsync(): Promise<MealManageFormSetup> {
    const mealManageFacade = createMealManageFacadeMock();
    const navigationService = {
        navigateToConsumptionListAsync: vi.fn().mockResolvedValue(true),
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
        addAiSession: vi.fn((_sessions: ConsumptionAiSessionManageDto[], session: ConsumptionAiSessionManageDto) => [session]),
        buildManualNutritionPatchFromTotals: vi.fn((totals: NutritionTotals) => ({
            manualCalories: totals.calories,
            manualProteins: totals.proteins,
            manualFats: totals.fats,
            manualCarbs: totals.carbs,
            manualFiber: totals.fiber,
            manualAlcohol: totals.alcohol,
        })),
        buildNutritionSummaryStateFromValues: vi.fn((_formValue, _aiSessions, _threshold) => createNutritionSummaryState()),
        configureItemType: vi.fn((item: ConsumptionItemFormValues) => item),
        convertRecipeGramsToServings: vi.fn((_recipe, amount: number) => amount),
        convertRecipeServingsToGrams: vi.fn((_recipe, amount: number) => amount),
        createConsumptionItem: vi.fn(() => createConsumptionItemValue()),
        ensurePremiumAccess: vi.fn().mockReturnValue(true),
        getManualNutritionTotalsFromValue: vi.fn().mockReturnValue(EMPTY_TOTALS),
        openEditAiPhotoSessionDialogAsync: vi.fn().mockResolvedValue(null),
        removeAiSession: vi.fn((sessions: ConsumptionAiSessionManageDto[], index: number) =>
            sessions.filter((_session, currentIndex) => currentIndex !== index),
        ),
        replaceAiSession: vi.fn(),
        showSuccessRedirectAsync: vi.fn().mockResolvedValue(void 0),
        submitConsumptionAsync: vi.fn().mockResolvedValue(null),
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

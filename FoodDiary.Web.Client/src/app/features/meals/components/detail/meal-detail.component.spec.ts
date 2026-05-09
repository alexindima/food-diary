import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MeasurementUnit, type Product, ProductVisibility } from '../../../products/models/product.data';
import { ConsumptionSourceType, type Meal } from '../../models/meal.data';
import { MealDetailComponent } from './meal-detail.component';

const createProduct = (id: string, name: string): Product => ({
    id,
    name,
    baseUnit: MeasurementUnit.G,
    baseAmount: 100,
    defaultPortionAmount: 100,
    caloriesPerBase: 0,
    proteinsPerBase: 0,
    fatsPerBase: 0,
    carbsPerBase: 0,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    usageCount: 0,
    visibility: ProductVisibility.Private,
    createdAt: new Date('2026-05-06T00:00:00Z'),
    isOwnedByCurrentUser: true,
    qualityScore: 0,
    qualityGrade: 'green',
});

const mockMeal: Meal = {
    id: '1',
    date: '2024-03-15T08:30:00Z',
    mealType: 'breakfast',
    comment: null,
    imageUrl: null,
    imageAssetId: null,
    totalCalories: 500,
    totalProteins: 30,
    totalFats: 20,
    totalCarbs: 50,
    totalFiber: 5,
    totalAlcohol: 0,
    isNutritionAutoCalculated: true,
    preMealSatietyLevel: null,
    postMealSatietyLevel: null,
    items: [],
    aiSessions: [],
};

describe('MealDetailComponent', () => {
    let component: MealDetailComponent;
    let fixture: ComponentFixture<MealDetailComponent>;

    const mockDialogRef = {
        close: vi.fn(),
    };

    const mockConfirmDialogRef = {
        afterClosed: vi.fn().mockReturnValue(of(true)),
    };

    const mockFdDialogService = {
        open: vi.fn().mockReturnValue(mockConfirmDialogRef),
    };

    beforeEach(async () => {
        vi.clearAllMocks();

        await TestBed.configureTestingModule({
            imports: [MealDetailComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: FD_UI_DIALOG_DATA, useValue: mockMeal },
                { provide: FdUiDialogRef, useValue: mockDialogRef },
                { provide: FdUiDialogService, useValue: mockFdDialogService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(MealDetailComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should extract nutrition values from meal', () => {
        expect(component.calories).toBe(500);
        expect(component.proteins).toBe(30);
        expect(component.fats).toBe(20);
        expect(component.carbs).toBe(50);
        expect(component.fiber).toBe(5);
        expect(component.alcohol).toBe(0);
    });

    it('should have summary and nutrients tabs', () => {
        expect(component.tabs.length).toBe(2);
        expect(component.tabs[0].value).toBe('summary');
        expect(component.tabs[1].value).toBe('nutrients');
    });

    it('should change active tab', () => {
        expect(component.activeTab).toBe('summary');

        component.onTabChange('nutrients');
        expect(component.activeTab).toBe('nutrients');

        component.onTabChange('summary');
        expect(component.activeTab).toBe('summary');
    });

    it('should not change tab for invalid value', () => {
        component.onTabChange('invalid');
        expect(component.activeTab).toBe('summary');
    });

    it('should close dialog with edit action on onEdit', () => {
        component.onEdit();

        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Edit' }));
    });

    it('should open confirm dialog on delete and close with delete action', () => {
        component.onDelete();

        expect(mockFdDialogService.open).toHaveBeenCalled();
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Delete' }));
    });

    it('should store the consumption data', () => {
        expect(component.consumption).toEqual(mockMeal);
    });

    it('should count items', () => {
        expect(component.itemsCount).toBe(0);
    });

    it('should include ai items in item count and preview', async () => {
        const meal: Meal = {
            ...mockMeal,
            items: [
                {
                    id: 'item-1',
                    consumptionId: '1',
                    amount: 180,
                    sourceType: ConsumptionSourceType.Product,
                    product: createProduct('p1', 'Manual item'),
                },
            ],
            aiSessions: [
                {
                    id: 'session-1',
                    consumptionId: '1',
                    imageAssetId: null,
                    imageUrl: null,
                    recognizedAtUtc: '2026-05-06T19:00:00Z',
                    items: [
                        {
                            id: 'ai-1',
                            sessionId: 'session-1',
                            nameEn: 'AI item',
                            nameLocal: 'ИИ позиция',
                            amount: 100,
                            unit: 'g',
                            calories: 0,
                            proteins: 0,
                            fats: 0,
                            carbs: 0,
                            fiber: 0,
                            alcohol: 0,
                        },
                    ],
                },
            ],
        };

        TestBed.resetTestingModule();
        await TestBed.configureTestingModule({
            imports: [MealDetailComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: FD_UI_DIALOG_DATA, useValue: meal },
                { provide: FdUiDialogRef, useValue: mockDialogRef },
                { provide: FdUiDialogService, useValue: mockFdDialogService },
            ],
        }).compileComponents();

        const customFixture = TestBed.createComponent(MealDetailComponent);
        const customComponent = customFixture.componentInstance;

        expect(customComponent.itemsCount).toBe(2);
        expect(customComponent.itemPreview.map(item => item.name)).toEqual(['Manual item', 'ИИ позиция']);
    });

    it('should build macro blocks', () => {
        expect(component.macroBlocks.length).toBe(5);
        expect(component.macroBlocks[0].value).toBe(30); // proteins
        expect(component.macroBlocks[1].value).toBe(20); // fats
        expect(component.macroBlocks[2].value).toBe(50); // carbs
    });

    it('should collapse item preview and expand hidden items', async () => {
        const meal: Meal = {
            ...mockMeal,
            items: [
                {
                    id: 'item-1',
                    consumptionId: '1',
                    amount: 100,
                    sourceType: ConsumptionSourceType.Product,
                    product: createProduct('p1', 'First item'),
                },
                {
                    id: 'item-2',
                    consumptionId: '1',
                    amount: 120,
                    sourceType: ConsumptionSourceType.Product,
                    product: createProduct('p2', 'Second item'),
                },
                {
                    id: 'item-3',
                    consumptionId: '1',
                    amount: 140,
                    sourceType: ConsumptionSourceType.Product,
                    product: createProduct('p3', 'Third item'),
                },
            ],
            aiSessions: [],
        };

        TestBed.resetTestingModule();
        await TestBed.configureTestingModule({
            imports: [MealDetailComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: FD_UI_DIALOG_DATA, useValue: meal },
                { provide: FdUiDialogRef, useValue: mockDialogRef },
                { provide: FdUiDialogService, useValue: mockFdDialogService },
            ],
        }).compileComponents();

        const customFixture = TestBed.createComponent(MealDetailComponent);
        const customComponent = customFixture.componentInstance;

        expect(customComponent.visibleItemPreview().map(item => item.name)).toEqual(['First item', 'Second item']);
        expect(customComponent.hiddenItemPreviewCount()).toBe(1);

        customComponent.toggleItemPreviewExpanded();

        expect(customComponent.visibleItemPreview().map(item => item.name)).toEqual(['First item', 'Second item', 'Third item']);
    });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';

import { RecipeDetailComponent } from './recipe-detail.component';
import { Recipe, RecipeVisibility } from '../../models/recipe.data';
import { RecipeService } from '../../api/recipe.service';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

const mockRecipe: Recipe = {
    id: '1',
    name: 'Test Recipe',
    comment: null,
    isOwnedByCurrentUser: true,
    prepTime: 10,
    cookTime: 20,
    servings: 4,
    totalCalories: 800,
    totalProteins: 40,
    totalFats: 30,
    totalCarbs: 80,
    totalFiber: 8,
    totalAlcohol: 0,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: '2024-01-01',
    isNutritionAutoCalculated: true,
    steps: [
        {
            id: 's1',
            stepNumber: 1,
            instruction: 'Mix ingredients',
            ingredients: [
                {
                    id: 'i1',
                    amount: 100,
                    productId: 'p1',
                    productName: 'Flour',
                    productBaseAmount: 100,
                    productCaloriesPerBase: 364,
                    productProteinsPerBase: 10,
                    productFatsPerBase: 1,
                    productCarbsPerBase: 76,
                    productFiberPerBase: 2.7,
                    productAlcoholPerBase: 0,
                },
                {
                    id: 'i2',
                    amount: 50,
                    productId: 'p2',
                    productName: 'Sugar',
                    productBaseAmount: 100,
                    productCaloriesPerBase: 387,
                    productProteinsPerBase: 0,
                    productFatsPerBase: 0,
                    productCarbsPerBase: 100,
                    productFiberPerBase: 0,
                    productAlcoholPerBase: 0,
                },
            ],
        },
    ],
};

describe('RecipeDetailComponent', () => {
    let component: RecipeDetailComponent;
    let fixture: ComponentFixture<RecipeDetailComponent>;

    const mockDialogRef = {
        close: vi.fn(),
    };

    const mockConfirmDialogRef = {
        afterClosed: vi.fn().mockReturnValue(of(true)),
    };

    const mockFdDialogService = {
        open: vi.fn().mockReturnValue(mockConfirmDialogRef),
    };

    const mockRecipeService = {
        duplicate: vi.fn().mockReturnValue(of({ ...mockRecipe, id: '2', name: 'Test Recipe (Copy)' })),
    };

    beforeEach(async () => {
        vi.clearAllMocks();

        await TestBed.configureTestingModule({
            imports: [RecipeDetailComponent, TranslateModule.forRoot()],
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                provideNoopAnimations(),
                { provide: FD_UI_DIALOG_DATA, useValue: mockRecipe },
                { provide: FdUiDialogRef, useValue: mockDialogRef },
                { provide: FdUiDialogService, useValue: mockFdDialogService },
                { provide: RecipeService, useValue: mockRecipeService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(RecipeDetailComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should extract nutrition from recipe', () => {
        expect(component.calories).toBe(800);
        expect(component.proteins).toBe(40);
        expect(component.fats).toBe(30);
        expect(component.carbs).toBe(80);
        expect(component.fiber).toBe(8);
        expect(component.alcohol).toBe(0);
    });

    it('should calculate total time', () => {
        expect(component.totalTime).toBe(30);
    });

    it('should count ingredients across steps', () => {
        expect(component.ingredientCount).toBe(2);
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

    it('should emit edit action', () => {
        component.onEdit();

        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Edit' }));
    });

    it('should emit delete action after confirmation', () => {
        component.onDelete();

        expect(mockFdDialogService.open).toHaveBeenCalled();
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Delete' }));
    });

    it('should not emit delete when delete is disabled', () => {
        Object.defineProperty(component, 'isDeleteDisabled', { get: () => true });

        component.onDelete();

        expect(mockFdDialogService.open).not.toHaveBeenCalled();
        expect(mockDialogRef.close).not.toHaveBeenCalled();
    });

    it('should not emit edit when edit is disabled', () => {
        Object.defineProperty(component, 'isEditDisabled', { get: () => true });

        component.onEdit();

        expect(mockDialogRef.close).not.toHaveBeenCalled();
    });

    it('should handle duplicate', () => {
        component.onDuplicate();

        expect(mockRecipeService.duplicate).toHaveBeenCalledWith('1');
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '2', action: 'Duplicate' }));
    });

    it('should detect canModify based on ownership and usage', () => {
        expect(component.canModify).toBe(true);
    });

    it('should build macro blocks', () => {
        expect(component.macroBlocks.length).toBe(5);
    });
});

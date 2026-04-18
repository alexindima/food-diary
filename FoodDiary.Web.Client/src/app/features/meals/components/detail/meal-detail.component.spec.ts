import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';

import { MealDetailComponent } from './meal-detail.component';
import { Meal } from '../../models/meal.data';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

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

    it('should build macro blocks', () => {
        expect(component.macroBlocks.length).toBe(5);
        expect(component.macroBlocks[0].value).toBe(30); // proteins
        expect(component.macroBlocks[1].value).toBe(20); // fats
        expect(component.macroBlocks[2].value).toBe(50); // carbs
    });
});

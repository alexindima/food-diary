import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UsdaFoodSearchFacade } from '../../lib/usda-food-search.facade';
import type { UsdaFood } from '../../models/usda.data';
import { UsdaFoodSearchDialogComponent } from './usda-food-search-dialog.component';

const FDC_ID = 17_000;
const FOOD: UsdaFood = {
    fdcId: FDC_ID,
    description: 'Apple',
    foodCategory: 'Fruit',
};

type DialogRefMock = {
    close: ReturnType<typeof vi.fn>;
};

type UsdaFoodSearchFacadeMock = {
    searchQuery: ReturnType<typeof signal<string>>;
    results: ReturnType<typeof signal<UsdaFood[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    selectedFood: ReturnType<typeof signal<UsdaFood | null>>;
    reset: ReturnType<typeof vi.fn>;
    updateSearchQuery: ReturnType<typeof vi.fn>;
    selectFood: ReturnType<typeof vi.fn>;
};

let dialogRef: DialogRefMock;
let facade: UsdaFoodSearchFacadeMock;

beforeEach(() => {
    dialogRef = {
        close: vi.fn(),
    };
    facade = {
        searchQuery: signal(''),
        results: signal([]),
        isLoading: signal(false),
        selectedFood: signal(null),
        reset: vi.fn(),
        updateSearchQuery: vi.fn(),
        selectFood: vi.fn(),
    };
});

describe('UsdaFoodSearchDialogComponent', () => {
    it('resets search state on create and delegates search changes', () => {
        const { component } = setupComponent();

        expect(facade.reset).toHaveBeenCalled();

        component.onSearchChange('apple');

        expect(facade.updateSearchQuery).toHaveBeenCalledWith('apple');
    });

    it('delegates selection and closes with selected food', () => {
        const { component } = setupComponent();

        component.selectFood(FOOD);
        expect(facade.selectFood).toHaveBeenCalledWith(FOOD);

        facade.selectedFood.set(FOOD);
        component.onConfirm();

        expect(dialogRef.close).toHaveBeenCalledWith(FOOD);
    });

    it('closes with null on cancel', () => {
        const { component } = setupComponent();

        component.onCancel();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

function setupComponent(): {
    component: UsdaFoodSearchDialogComponent;
    fixture: ComponentFixture<UsdaFoodSearchDialogComponent>;
} {
    TestBed.configureTestingModule({
        imports: [UsdaFoodSearchDialogComponent, FormsModule, TranslateModule.forRoot()],
        providers: [
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: UsdaFoodSearchFacade, useValue: facade },
        ],
    });

    const fixture = TestBed.createComponent(UsdaFoodSearchDialogComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

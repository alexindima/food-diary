import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { ConsumptionSourceType } from '../../../models/meal.data';
import type { ConsumptionItemFormData } from '../meal-manage-lib/meal-manage.types';
import { MealItemsSectionComponent } from './meal-items-section.component';

const ITEM_INDEX = 1;
const SESSION_INDEX = 2;

describe('MealItemsSectionComponent', () => {
    it('should emit add item requests', async () => {
        const { component } = await setupComponentAsync();
        const handler = vi.fn();
        component.addItem.subscribe(handler);

        component.addItem.emit();

        expect(handler).toHaveBeenCalled();
    });

    it('should emit item action requests', async () => {
        const { component } = await setupComponentAsync();
        const editHandler = vi.fn();
        const removeHandler = vi.fn();
        const openHandler = vi.fn();
        component.editItem.subscribe(editHandler);
        component.removeItem.subscribe(removeHandler);
        component.openItemSelect.subscribe(openHandler);

        component.editItem.emit(ITEM_INDEX);
        component.removeItem.emit(ITEM_INDEX);
        component.openItemSelect.emit(ITEM_INDEX);

        expect(editHandler).toHaveBeenCalledWith(ITEM_INDEX);
        expect(removeHandler).toHaveBeenCalledWith(ITEM_INDEX);
        expect(openHandler).toHaveBeenCalledWith(ITEM_INDEX);
    });

    it('should emit session action requests', async () => {
        const { component } = await setupComponentAsync();
        const editHandler = vi.fn();
        const deleteHandler = vi.fn();
        component.editSession.subscribe(editHandler);
        component.deleteSession.subscribe(deleteHandler);

        component.editSession.emit(SESSION_INDEX);
        component.deleteSession.emit(SESSION_INDEX);

        expect(editHandler).toHaveBeenCalledWith(SESSION_INDEX);
        expect(deleteHandler).toHaveBeenCalledWith(SESSION_INDEX);
    });
});

type MealItemsSectionSetup = {
    component: MealItemsSectionComponent;
    fixture: ComponentFixture<MealItemsSectionComponent>;
};

async function setupComponentAsync(): Promise<MealItemsSectionSetup> {
    await TestBed.configureTestingModule({
        imports: [MealItemsSectionComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealItemsSectionComponent);
    fixture.componentRef.setInput('items', createItemsFormArray());
    fixture.componentRef.setInput('aiSessions', []);
    fixture.componentRef.setInput('selectedMealType', null);
    fixture.componentRef.setInput('isProcessing', false);
    fixture.componentRef.setInput('renderVersion', 0);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createItemsFormArray(): FormArray<FormGroup<ConsumptionItemFormData>> {
    return new FormArray<FormGroup<ConsumptionItemFormData>>([
        new FormGroup<ConsumptionItemFormData>({
            sourceType: new FormControl(ConsumptionSourceType.Product, { nonNullable: true }),
            product: new FormControl(null),
            recipe: new FormControl(null),
            amount: new FormControl(null),
        }),
    ]);
}

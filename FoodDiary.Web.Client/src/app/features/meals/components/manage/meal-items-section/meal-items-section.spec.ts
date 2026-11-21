import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { createEmptyProductSnapshot, type MealAiSessionManageDto, MealSourceType } from '../../../../../shared/models/meal.data';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { UNKNOWN_SERVING_MASS } from '../../../lib/recipe-serving/recipe-display-amount';
import type { MealItemsListItemState } from '../meal-items-list/meal-items-list';
import type { MealItemFormValues } from '../meal-manage-lib/meal-manage.types';
import { MealItemsSectionComponent } from './meal-items-section';

const ITEM_INDEX = 1;

describe('MealItemsSectionComponent', () => {
    it('should emit add item requests', async () => {
        const { component } = await setupComponentAsync();
        const handler = vi.fn();
        component['addItem'].subscribe(handler);

        component['addItem'].emit();

        expect(handler).toHaveBeenCalled();
    });

    it('should emit item action requests', async () => {
        const { component, fixture } = await setupComponentAsync();
        const amount = 100;
        const selected = createItemState({
            product: { ...createEmptyProductSnapshot(), id: entityId<'product'>('apple'), name: 'Apple' },
            amount,
        });
        fixture.componentRef.setInput('items', [selected]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const editHandler = vi.fn();
        const removeHandler = vi.fn<(rendered: MealItemFormValues) => void>();
        const openHandler = vi.fn();
        component['editItem'].subscribe(editHandler);
        component['removeItem'].subscribe(removeHandler);
        component['openItemSelect'].subscribe(openHandler);

        const edit = host.querySelector<HTMLButtonElement>('button[aria-label="MEAL_MANAGE.MANUAL_ITEM_EDIT"]');
        const remove = host.querySelector<HTMLButtonElement>('button[aria-label="MEAL_MANAGE.REMOVE_ITEM"]');
        if (edit === null || remove === null) {
            throw new Error('Expected the rendered manual item actions');
        }
        edit.click();
        remove.click();
        component['openItemSelect'].emit(ITEM_INDEX);

        expect(editHandler).toHaveBeenCalledWith(0);
        expect(removeHandler).toHaveBeenCalledOnce();
        expect(removeHandler.mock.calls[0][0]).toBe(selected.formItem);
        expect(openHandler).toHaveBeenCalledWith(ITEM_INDEX);
    });

    it('forwards the rendered session action requests', async () => {
        const { component, fixture } = await setupComponentAsync();
        const session: MealAiSessionManageDto = { items: [] };
        fixture.componentRef.setInput('aiSessions', [session]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const editHandler = vi.fn<(rendered: MealAiSessionManageDto) => void>();
        const deleteHandler = vi.fn<(rendered: MealAiSessionManageDto) => void>();
        component.editSession.subscribe(editHandler);
        component.deleteSession.subscribe(request => {
            deleteHandler(request.session);
        });
        const edit = host.querySelector<HTMLButtonElement>('button[aria-label="MEAL_MANAGE.ITEMS_AI_EDIT"]');
        const remove = host.querySelector<HTMLButtonElement>('button[aria-label="MEAL_MANAGE.ITEMS_AI_DELETE"]');
        if (edit === null || remove === null) {
            throw new Error('Expected native edit and delete actions for the recognition');
        }

        edit.click();
        remove.click();

        expect(editHandler).toHaveBeenCalledOnce();
        expect(editHandler.mock.calls[0][0]).toBe(session);
        expect(deleteHandler).toHaveBeenCalledOnce();
        expect(deleteHandler.mock.calls[0][0]).toBe(session);
    });
});

type MealItemsSectionSetup = {
    component: MealItemsSectionComponent;
    fixture: ComponentFixture<MealItemsSectionComponent>;
};

async function setupComponentAsync(): Promise<MealItemsSectionSetup> {
    await TestBed.configureTestingModule({
        imports: [MealItemsSectionComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealItemsSectionComponent);
    fixture.componentRef.setInput('items', [createItemState()]);
    fixture.componentRef.setInput('aiSessions', []);
    fixture.componentRef.setInput('selectedMealType', null);
    fixture.componentRef.setInput('isProcessing', false);
    fixture.componentRef.setInput('itemsError', null);
    fixture.componentRef.setInput('renderVersion', 0);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createItemState(values: Partial<MealItemFormValues> = {}): MealItemsListItemState {
    return {
        formItem: {
            sourceType: MealSourceType.Product,
            product: null,
            recipe: null,
            amount: null,
            recipeMass: UNKNOWN_SERVING_MASS,
            ...values,
        },
        amountError: null,
        productInvalid: false,
        recipeInvalid: false,
        sourceError: null,
    };
}

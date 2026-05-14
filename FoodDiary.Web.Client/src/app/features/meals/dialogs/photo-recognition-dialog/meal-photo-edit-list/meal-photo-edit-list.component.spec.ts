import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { describe, expect, it, vi } from 'vitest';

import type { EditableAiItem, UnitOptionView } from '../meal-photo-recognition-dialog-lib/meal-photo-recognition-dialog.types';
import { MealPhotoEditListComponent } from './meal-photo-edit-list.component';

describe('MealPhotoEditListComponent', () => {
    it('should emit item updates from name, amount and unit controls', async () => {
        const { component, fixture } = await setupComponentAsync();
        const updateSpy = vi.fn();
        component.itemUpdated.subscribe(updateSpy);

        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        setInputValue(host.querySelector('.photo-ai-dialog__edit-input--name'), 'Updated apple');
        setInputValue(host.querySelector('.photo-ai-dialog__edit-input--amount'), '125');
        setSelectValue(host.querySelector('.photo-ai-dialog__edit-select'), 'ml');

        expect(updateSpy).toHaveBeenCalledWith({ index: 0, field: 'name', value: 'Updated apple' });
        expect(updateSpy).toHaveBeenCalledWith({ index: 0, field: 'amount', value: '125' });
        expect(updateSpy).toHaveBeenCalledWith({ index: 0, field: 'unit', value: 'ml' });
    });

    it('should emit remove and add actions', async () => {
        const { component, fixture } = await setupComponentAsync();
        const removedSpy = vi.fn();
        const addedSpy = vi.fn();
        component.itemRemoved.subscribe(removedSpy);
        component.itemAdded.subscribe(addedSpy);

        fixture.detectChanges();
        const buttons = fixture.debugElement.queryAll(By.directive(FdUiButtonComponent));
        buttons[0].triggerEventHandler('click');
        buttons[1].triggerEventHandler('click');

        expect(removedSpy).toHaveBeenCalledWith(0);
        expect(addedSpy).toHaveBeenCalledOnce();
    });

    it('should render unit options', async () => {
        const { fixture } = await setupComponentAsync({
            unitOptions: [
                { value: 'g', labelKey: 'PRODUCT_AMOUNT_UNITS.G' },
                { value: 'ml', labelKey: 'PRODUCT_AMOUNT_UNITS.ML' },
            ],
        });

        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const options = Array.from(host.querySelectorAll('option')).map(option => option.textContent.trim());
        expect(options).toEqual(['PRODUCT_AMOUNT_UNITS.G', 'PRODUCT_AMOUNT_UNITS.ML']);
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        items: EditableAiItem[];
        unitOptions: readonly UnitOptionView[];
    }> = {},
): Promise<{
    component: MealPhotoEditListComponent;
    fixture: ComponentFixture<MealPhotoEditListComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealPhotoEditListComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealPhotoEditListComponent);
    fixture.componentRef.setInput('items', overrides.items ?? [createEditableItem()]);
    fixture.componentRef.setInput('unitOptions', overrides.unitOptions ?? [{ value: 'ml', labelKey: 'PRODUCT_AMOUNT_UNITS.ML' }]);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createEditableItem(overrides: Partial<EditableAiItem> = {}): EditableAiItem {
    return {
        id: 'item-1',
        name: 'Apple',
        nameEn: 'Apple',
        nameLocal: null,
        amount: 100,
        unit: 'g',
        ...overrides,
    };
}

function setInputValue(input: Element | null, value: string): void {
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected input element.');
    }

    input.value = value;
    input.dispatchEvent(new Event('input'));
}

function setSelectValue(select: Element | null, value: string): void {
    if (!(select instanceof HTMLSelectElement)) {
        throw new Error('Expected select element.');
    }

    select.value = value;
    select.dispatchEvent(new Event('change'));
}

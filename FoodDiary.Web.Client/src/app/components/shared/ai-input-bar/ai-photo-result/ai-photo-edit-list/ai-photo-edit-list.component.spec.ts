import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { EditableAiItem } from '../ai-photo-result-lib/ai-photo-result.types';
import { AiPhotoEditListComponent } from './ai-photo-edit-list.component';

const ITEM_AMOUNT = 120;
const UPDATED_AMOUNT = '150';

const editableItem: EditableAiItem = {
    id: 'item-1',
    name: 'Apple',
    nameEn: 'Apple',
    nameLocal: null,
    amount: ITEM_AMOUNT,
    unit: 'g',
};

async function setupAiPhotoEditListAsync(): Promise<ComponentFixture<AiPhotoEditListComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoEditListComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoEditListComponent);
    fixture.componentRef.setInput('items', [editableItem]);
    fixture.componentRef.setInput('unitOptions', [{ value: 'g', label: 'g' }]);
    return fixture;
}

describe('AiPhotoEditListComponent', () => {
    it('emits item updates from inputs', async () => {
        const fixture = await setupAiPhotoEditListAsync();
        const updateSpy = vi.fn();
        fixture.componentInstance.itemUpdated.subscribe(updateSpy);
        fixture.detectChanges();

        const amountInput = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.ai-photo-result__edit-input--amount');
        if (amountInput === null) {
            throw new Error('Amount input was not rendered.');
        }

        amountInput.value = UPDATED_AMOUNT;
        amountInput.dispatchEvent(new Event('input'));

        expect(updateSpy).toHaveBeenCalledWith({ index: 0, field: 'amount', value: UPDATED_AMOUNT });
    });

    it('emits remove and add actions', async () => {
        const fixture = await setupAiPhotoEditListAsync();
        const removeSpy = vi.fn();
        const addSpy = vi.fn();
        fixture.componentInstance.itemRemoved.subscribe(removeSpy);
        fixture.componentInstance.itemAdded.subscribe(addSpy);
        fixture.detectChanges();

        (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.ai-photo-result__edit-remove')?.click();
        (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.ai-photo-result__edit-add')?.click();

        expect(removeSpy).toHaveBeenCalledWith(0);
        expect(addSpy).toHaveBeenCalledOnce();
    });
});

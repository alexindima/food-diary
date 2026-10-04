import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import enTranslations from '../../../../../../../assets/i18n/en/app.json';
import ruTranslations from '../../../../../../../assets/i18n/ru/app.json';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { EditableAiItem } from '../ai-photo-result-lib/ai-photo-result.types';
import { AiPhotoEditListComponent } from './ai-photo-edit-list';

const ITEM_AMOUNT = 120;
const UPDATED_AMOUNT = '150';

const editableItem: EditableAiItem = {
    id: 'item-1',
    name: 'Apple',
    nameEn: 'Apple',
    nameLocal: null,
    amount: ITEM_AMOUNT,
    unit: 'g',
    confidence: 1,
    resolution: 'Accepted',
};

async function setupAiPhotoEditListAsync(): Promise<ComponentFixture<AiPhotoEditListComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoEditListComponent],
        providers: [provideTranslateTesting()],
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
        fixture.componentInstance['itemUpdated'].subscribe(updateSpy);
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
        fixture.componentInstance['itemRemoved'].subscribe(removeSpy);
        fixture.componentInstance['itemAdded'].subscribe(addSpy);
        fixture.detectChanges();

        (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.ai-photo-result__edit-remove')?.click();
        (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.ai-photo-result__edit-add')?.click();

        expect(removeSpy).toHaveBeenCalledWith(0);
        expect(addSpy).toHaveBeenCalledOnce();
    });
});

describe('AiPhotoEditListComponent accessibility', () => {
    it.each([
        ['en', enTranslations, ['Name', 'Amount', 'Unit'], 'Remove Apple from analysis'],
        ['ru', ruTranslations, ['Название', 'Количество', 'Единица'], 'Удалить Apple из анализа'],
    ] as const)('labels editor controls and interpolates the product name in %s', async (language, translations, labels, removeLabel) => {
        const fixture = await setupAiPhotoEditListAsync();
        const translateService = TestBed.inject(TranslateService);
        translateService.setTranslation(language, translations);
        translateService.use(language);
        fixture.detectChanges();

        const element = fixture.nativeElement as HTMLElement;
        const controls = Array.from(element.querySelectorAll<HTMLInputElement | HTMLSelectElement>('input, select'));
        const controlLabels = controls.map(control => control.labels?.item(0).textContent.trim());
        expect(controlLabels).toEqual(labels);
        expect(element.querySelector('.ai-photo-result__edit-remove button')?.getAttribute('aria-label')).toBe(removeLabel);
    });
});

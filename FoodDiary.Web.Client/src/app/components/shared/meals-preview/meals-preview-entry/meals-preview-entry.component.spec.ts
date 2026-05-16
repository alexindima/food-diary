import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { MealPreviewEntry } from '../meals-preview-lib/meals-preview.types';
import { MealsPreviewEntryComponent } from './meals-preview-entry.component';

async function setupMealsPreviewEntryAsync(entry: MealPreviewEntry): Promise<ComponentFixture<MealsPreviewEntryComponent>> {
    await TestBed.configureTestingModule({
        imports: [MealsPreviewEntryComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealsPreviewEntryComponent);
    fixture.componentRef.setInput('entry', entry);
    fixture.componentRef.setInput('showAddButtons', true);
    fixture.componentRef.setInput('showAiButtons', true);
    return fixture;
}

describe('MealsPreviewEntryComponent', () => {
    it('emits add for empty slot action', async () => {
        const fixture = await setupMealsPreviewEntryAsync({ slot: 'breakfast', meal: null });
        const component = fixture.componentInstance;
        const addSpy = vi.fn();
        component.add.subscribe(addSpy);
        fixture.detectChanges();

        const addButton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.meals-preview__placeholder-main');
        addButton?.click();

        expect(addSpy).toHaveBeenCalledWith('breakfast');
    });
});

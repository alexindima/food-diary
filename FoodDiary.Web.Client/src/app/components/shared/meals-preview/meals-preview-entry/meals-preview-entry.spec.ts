import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { MealPreviewEntry } from '../meals-preview-lib/meals-preview.types';
import { MealsPreviewEntryComponent } from './meals-preview-entry';

async function setupMealsPreviewEntryAsync(entry: MealPreviewEntry): Promise<ComponentFixture<MealsPreviewEntryComponent>> {
    await TestBed.configureTestingModule({
        imports: [MealsPreviewEntryComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealsPreviewEntryComponent);
    fixture.componentRef.setInput('entry', entry);
    fixture.componentRef.setInput('showAddButtons', true);
    fixture.componentRef.setInput('showAiButtons', false);
    return fixture;
}

describe('MealsPreviewEntryComponent', () => {
    it('emits add for empty slot action', async () => {
        const fixture = await setupMealsPreviewEntryAsync({ slot: 'breakfast', meal: null });
        const component = fixture.componentInstance;
        const addSpy = vi.fn();
        component['add'].subscribe(addSpy);
        fixture.detectChanges();

        const addButton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.meals-preview__placeholder-main');
        addButton?.click();

        expect(addSpy).toHaveBeenCalledWith('breakfast');
    });

    it('offers manual addition for an entry without a meal slot when AI is enabled', async () => {
        const fixture = await setupMealsPreviewEntryAsync({ slot: null });
        fixture.componentRef.setInput('showAiButtons', true);
        const add = vi.fn();
        const aiToggle = vi.fn();
        fixture.componentInstance.add.subscribe(add);
        fixture.componentInstance.aiToggle.subscribe(aiToggle);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLButtonElement>('.meals-preview__placeholder-main');
        expect(button).not.toBeNull();
        expect(button?.hasAttribute('aria-expanded')).toBe(false);
        expect(host.querySelector('.meals-preview__ai-panel')).toBeNull();
        button?.click();

        expect(add).toHaveBeenCalledExactlyOnceWith(null);
        expect(aiToggle).not.toHaveBeenCalled();
    });

    it('ignores a click after saving begins while the previous button is still rendered', async () => {
        const fixture = await setupMealsPreviewEntryAsync({ slot: 'breakfast' });
        fixture.componentRef.setInput('showAiButtons', true);
        const add = vi.fn();
        const aiToggle = vi.fn();
        fixture.componentInstance.add.subscribe(add);
        fixture.componentInstance.aiToggle.subscribe(aiToggle);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLButtonElement>('.meals-preview__placeholder-main');
        expect(button?.disabled).toBe(false);
        fixture.componentRef.setInput('isAiMealSaving', true);
        expect(button?.disabled).toBe(false);
        button?.click();
        fixture.detectChanges();

        expect(button?.disabled).toBe(true);
        expect(add).not.toHaveBeenCalled();
        expect(aiToggle).not.toHaveBeenCalled();
    });
});

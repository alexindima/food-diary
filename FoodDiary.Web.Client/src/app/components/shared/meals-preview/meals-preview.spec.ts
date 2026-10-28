import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import type { AiInputBarResult } from '../ai-input-bar/ai-input-bar.types';
import { MealsPreviewComponent } from './meals-preview';

async function setupMealsPreviewAsync(): Promise<ComponentFixture<MealsPreviewComponent>> {
    await TestBed.configureTestingModule({
        imports: [MealsPreviewComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealsPreviewComponent);
    fixture.componentRef.setInput('entries', []);
    return fixture;
}

describe('MealsPreviewComponent header', () => {
    it('opens the list from the visible all action', async () => {
        const fixture = await setupMealsPreviewAsync();
        const viewAll = vi.fn();
        fixture.componentInstance.viewAll.subscribe(viewAll);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        host.querySelector<HTMLButtonElement>('.meals-preview__header button')?.click();

        expect(viewAll).toHaveBeenCalledExactlyOnceWith(undefined);
    });

    it('supports previews without a list action', async () => {
        const fixture = await setupMealsPreviewAsync();
        fixture.componentRef.setInput('showViewAll', false);
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('.meals-preview__header button')).toBeNull();
    });
});

describe('MealsPreviewComponent AI panel', () => {
    it('toggles expanded AI slot', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component['toggleAi']('breakfast');
        expect(component['expandedAiSlot']()).toBe('breakfast');

        component['toggleAi']('breakfast');
        expect(component['expandedAiSlot']()).toBeNull();
    });

    it('normalizes missing slot to null', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component['toggleAi']();

        expect(component['expandedAiSlot']()).toBeNull();
    });

    it('keeps AI slot open while meal create is pending and emits created meal result', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        const emitSpy = vi.fn<(result: AiInputBarResult) => void>();
        const result: AiInputBarResult = {
            source: 'Text',
            recognizedAtUtc: '2026-05-17T00:00:00Z',
            items: [],
        };
        component['aiMealCreateRequested'].subscribe(resultValue => {
            emitSpy(resultValue);
        });
        component['toggleAi']('lunch');
        fixture.detectChanges();

        component['createMealFromAiResult'](result);

        expect(component['expandedAiSlot']()).toBe('lunch');
        expect(emitSpy).toHaveBeenCalledWith(result);
    });

    it('collapses AI slot when clear token changes after successful create', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component['toggleAi']('lunch');
        fixture.componentRef.setInput('aiMealClearToken', 1);
        fixture.detectChanges();

        expect(component['expandedAiSlot']()).toBeNull();
    });
});

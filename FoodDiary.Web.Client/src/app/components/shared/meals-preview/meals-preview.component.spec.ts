import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { AiInputBarResult } from '../ai-input-bar/ai-input-bar.types';
import { MealsPreviewComponent } from './meals-preview.component';

async function setupMealsPreviewAsync(): Promise<ComponentFixture<MealsPreviewComponent>> {
    await TestBed.configureTestingModule({
        imports: [MealsPreviewComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealsPreviewComponent);
    fixture.componentRef.setInput('entries', []);
    return fixture;
}

describe('MealsPreviewComponent AI panel', () => {
    it('toggles expanded AI slot', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component.toggleAi('breakfast');
        expect(component.expandedAiSlot()).toBe('breakfast');

        component.toggleAi('breakfast');
        expect(component.expandedAiSlot()).toBeNull();
    });

    it('normalizes missing slot to null', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component.toggleAi();

        expect(component.expandedAiSlot()).toBeNull();
    });

    it('collapses AI slot and emits created meal result', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        const emitSpy = vi.fn<(result: AiInputBarResult) => void>();
        const result: AiInputBarResult = {
            source: 'Text',
            recognizedAtUtc: '2026-05-17T00:00:00Z',
            items: [],
        };
        component.aiMealCreateRequested.subscribe(resultValue => {
            emitSpy(resultValue);
        });
        component.toggleAi('lunch');
        fixture.detectChanges();

        component.handleAiMealCreateRequested(result);

        expect(component.expandedAiSlot()).toBeNull();
        expect(emitSpy).toHaveBeenCalledWith(result);
    });
});

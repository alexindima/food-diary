import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { AiPhotoNutritionSummaryComponent } from './ai-photo-nutrition-summary';

async function setupAiPhotoNutritionSummaryAsync(): Promise<ComponentFixture<AiPhotoNutritionSummaryComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoNutritionSummaryComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoNutritionSummaryComponent);
    fixture.componentRef.setInput('items', [{ labelKey: 'CALORIES', value: '100 kcal' }]);
    return fixture;
}

describe('AiPhotoNutritionSummaryComponent', () => {
    it('renders summary items', async () => {
        const fixture = await setupAiPhotoNutritionSummaryAsync();
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).textContent).toContain('100 kcal');
    });
});

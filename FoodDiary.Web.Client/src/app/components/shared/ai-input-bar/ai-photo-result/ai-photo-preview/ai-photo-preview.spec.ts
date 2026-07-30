import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { AiPhotoAnnotation } from '../ai-photo-result-lib/ai-photo-result.types';
import { AiPhotoPreviewComponent } from './ai-photo-preview';

const SIX_PRODUCTS = 6;
const SEVEN_PRODUCTS = 7;
const CENTER_X_START = 10;
const CENTER_X_STEP = 8;
const CENTER_Y_START = 20;
const CENTER_Y_STEP = 5;
const RIGHT_CARD_X = 70;
const CARD_Y_START = 4;
const CARD_Y_STEP = 20;

function createAnnotations(count: number): AiPhotoAnnotation[] {
    return Array.from({ length: count }, (_, index) => ({
        id: `food-${index}`,
        name: `Food ${index + 1}`,
        amountLabel: '100 g',
        centerX: CENTER_X_START + index * CENTER_X_STEP,
        centerY: CENTER_Y_START + index * CENTER_Y_STEP,
        cardX: index % 2 === 0 ? 2 : RIGHT_CARD_X,
        cardY: CARD_Y_START + Math.floor(index / 2) * CARD_Y_STEP,
        calories: 100,
        protein: 10,
        fat: 5,
        carbs: 15,
    }));
}

async function setupAiPhotoPreviewAsync(): Promise<ComponentFixture<AiPhotoPreviewComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoPreviewComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoPreviewComponent);
    fixture.componentRef.setInput('imageUrl', null);
    fixture.componentRef.setInput('sourceText', 'eggs');
    fixture.componentRef.setInput('sourceTextLabelKey', 'AI_INPUT_BAR.TEXT_PREVIEW_LABEL');
    fixture.componentRef.setInput('isAnalyzing', false);
    fixture.componentRef.setInput('isNutritionLoading', false);
    return fixture;
}

describe('AiPhotoPreviewComponent', () => {
    it('renders source text preview when image is absent', async () => {
        const fixture = await setupAiPhotoPreviewAsync();
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).textContent).toContain('eggs');
    });

    it('shows every annotation card when there are six products', async () => {
        const fixture = await setupAiPhotoPreviewAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/meal.png');
        fixture.componentRef.setInput('sourceText', null);
        fixture.componentRef.setInput('annotations', createAnnotations(SIX_PRODUCTS));
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelectorAll('.ai-photo-result__annotation-card')).toHaveLength(SIX_PRODUCTS);
        expect((fixture.nativeElement as HTMLElement).querySelectorAll('.ai-photo-result__annotation-marker')).toHaveLength(0);
    });

    it('shows one active card and compact markers above six products', async () => {
        const fixture = await setupAiPhotoPreviewAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/meal.png');
        fixture.componentRef.setInput('sourceText', null);
        fixture.componentRef.setInput('annotations', createAnnotations(SEVEN_PRODUCTS));
        fixture.componentRef.setInput('activeAnnotationId', 'food-2');
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelectorAll('.ai-photo-result__annotation-card')).toHaveLength(1);
        expect((fixture.nativeElement as HTMLElement).querySelector('.ai-photo-result__annotation-card')?.textContent).toContain('Food 3');
        expect((fixture.nativeElement as HTMLElement).querySelectorAll('.ai-photo-result__annotation-marker')).toHaveLength(SIX_PRODUCTS);
    });
});

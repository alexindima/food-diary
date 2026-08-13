import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { AiPhotoAnnotation } from '../ai-photo-result-lib/ai-photo-result.types';
import { AiPhotoPreviewComponent } from './ai-photo-preview';

const SIX_PRODUCTS = 6;
const SEVEN_PRODUCTS = 7;
const MOBILE_CARD_COUNT = 4;
const MOBILE_MARKER_COUNT = 3;
const CENTER_X_START = 10;
const CENTER_X_STEP = 8;
const CENTER_Y_START = 20;
const CENTER_Y_STEP = 5;
const RIGHT_CARD_X = 70;
const CARD_Y_START = 4;
const CARD_Y_STEP = 20;
const CARD_WIDTH = 28;
const CARD_HEIGHT = 15;

function createAnnotations(count: number): AiPhotoAnnotation[] {
    return Array.from({ length: count }, (_, index) => ({
        id: `food-${index}`,
        name: `Food ${index + 1}`,
        amountLabel: '100 g',
        centerX: CENTER_X_START + index * CENTER_X_STEP,
        centerY: CENTER_Y_START + index * CENTER_Y_STEP,
        cardX: index % 2 === 0 ? 2 : RIGHT_CARD_X,
        cardY: CARD_Y_START + Math.floor(index / 2) * CARD_Y_STEP,
        cardWidth: CARD_WIDTH,
        cardHeight: CARD_HEIGHT,
        connectorPoints: [
            { x: CENTER_X_START + index * CENTER_X_STEP, y: CENTER_Y_START + index * CENTER_Y_STEP },
            { x: index % 2 === 0 ? CARD_WIDTH + 2 : RIGHT_CARD_X, y: CARD_Y_START + index * CARD_Y_STEP },
        ],
        connectorPath: '',
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
    it('shows a preparation status over the local image preview', async () => {
        const fixture = await setupAiPhotoPreviewAsync();
        fixture.componentRef.setInput('imageUrl', 'blob:local-preview');
        fixture.componentRef.setInput('sourceText', null);
        fixture.componentRef.setInput('isPreparing', true);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.ai-photo-result__scan-overlay')).not.toBeNull();
        expect(host.querySelector('[role="status"]')?.textContent).toContain('MEAL_MANAGE.PHOTO_AI_DIALOG.STATUS_PREPARING');
    });

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
        fixture.componentRef.setInput('mobileAnnotations', createAnnotations(SIX_PRODUCTS));
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelectorAll('.ai-photo-result__annotation-card.ai-photo-result__annotation-desktop')).toHaveLength(SIX_PRODUCTS);
        expect(host.querySelectorAll('.ai-photo-result__annotation-point.ai-photo-result__annotation-desktop')).toHaveLength(SIX_PRODUCTS);
        expect(host.querySelectorAll('.ai-photo-result__annotation-connector-outline.ai-photo-result__annotation-desktop')).toHaveLength(
            SIX_PRODUCTS,
        );
        expect(host.querySelectorAll('.ai-photo-result__annotation-connector-core.ai-photo-result__annotation-desktop')).toHaveLength(
            SIX_PRODUCTS,
        );
        expect(host.querySelectorAll('.ai-photo-result__annotation-marker.ai-photo-result__annotation-desktop')).toHaveLength(0);
    });

    it('shows one active card and compact markers above six products', async () => {
        const fixture = await setupAiPhotoPreviewAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/meal.png');
        fixture.componentRef.setInput('sourceText', null);
        fixture.componentRef.setInput('annotations', createAnnotations(SEVEN_PRODUCTS));
        fixture.componentRef.setInput('mobileAnnotations', createAnnotations(SEVEN_PRODUCTS));
        fixture.componentRef.setInput('activeAnnotationId', 'food-2');
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelectorAll('.ai-photo-result__annotation-card.ai-photo-result__annotation-desktop')).toHaveLength(1);
        expect(host.querySelector('.ai-photo-result__annotation-card.ai-photo-result__annotation-desktop')?.textContent).toContain(
            'Food 3',
        );
        expect((fixture.nativeElement as HTMLElement).querySelector('.ai-photo-result__annotation-card--active')).not.toBeNull();
        expect(host.querySelectorAll('.ai-photo-result__annotation-marker.ai-photo-result__annotation-desktop')).toHaveLength(SIX_PRODUCTS);
    });

    it('renders the fitted mobile cards and turns the remaining products into markers', async () => {
        const fixture = await setupAiPhotoPreviewAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/meal.png');
        fixture.componentRef.setInput('sourceText', null);
        fixture.componentRef.setInput('annotations', createAnnotations(SEVEN_PRODUCTS));
        fixture.componentRef.setInput('mobileAnnotations', createAnnotations(SEVEN_PRODUCTS).slice(0, MOBILE_CARD_COUNT));
        fixture.componentRef.setInput('activeAnnotationId', 'food-5');
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const mobileCards = host.querySelectorAll('.ai-photo-result__annotation-card--mobile');
        expect(mobileCards).toHaveLength(MOBILE_CARD_COUNT);
        expect(mobileCards[0].textContent).toContain('Food 1');
        expect(host.querySelectorAll('.ai-photo-result__annotation-marker.ai-photo-result__annotation-mobile')).toHaveLength(
            MOBILE_MARKER_COUNT,
        );
    });

    it('highlights the selected card when all annotations are expanded', async () => {
        const fixture = await setupAiPhotoPreviewAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/meal.png');
        fixture.componentRef.setInput('sourceText', null);
        fixture.componentRef.setInput('annotations', createAnnotations(SIX_PRODUCTS));
        fixture.componentRef.setInput('mobileAnnotations', createAnnotations(SIX_PRODUCTS));
        fixture.componentRef.setInput('activeAnnotationId', 'food-4');
        fixture.detectChanges();

        const activeCard = (fixture.nativeElement as HTMLElement).querySelector('.ai-photo-result__annotation-card--active');
        expect(activeCard?.textContent).toContain('Food 5');
    });
});

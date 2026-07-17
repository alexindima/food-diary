import { HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import { AiInputBarFacade } from './ai-input-bar.facade';

describe('AiInputBarFacade', () => {
    const item: FoodVisionItem = { nameEn: 'Apple', amount: 100, unit: 'g', confidence: 1 };
    const nutrition: FoodNutritionResponse = {
        calories: 52,
        protein: 0.3,
        fat: 0.2,
        carbs: 14,
        fiber: 2.4,
        alcohol: 0,
        items: [],
    };
    const aiFoodFacade = {
        parseFoodText: vi.fn(() => of({ items: [item] })),
        analyzeFoodImage: vi.fn(() => of({ items: [item] })),
        calculateNutrition: vi.fn(() => of(nutrition)),
    };
    let facade: AiInputBarFacade;

    beforeEach(() => {
        vi.clearAllMocks();
        TestBed.configureTestingModule({
            providers: [AiInputBarFacade, { provide: AiFoodFacade, useValue: aiFoodFacade }],
        });
        facade = TestBed.inject(AiInputBarFacade);
    });

    it('runs text recognition and nutrition as one state transition', () => {
        facade.analyzeText('apple');

        expect(aiFoodFacade.parseFoodText).toHaveBeenCalledWith({ text: 'apple' });
        expect(aiFoodFacade.calculateNutrition).toHaveBeenCalledWith({ items: [item] });
        expect(facade.text.results()).toEqual([item]);
        expect(facade.text.nutrition()).toBe(nutrition);
        expect(facade.text.analyzing()).toBe(false);
        expect(facade.text.nutritionLoading()).toBe(false);
    });

    it('maps authorization and quota failures to channel-specific errors', () => {
        aiFoodFacade.parseFoodText.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.Forbidden })));
        aiFoodFacade.analyzeFoodImage.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.TooManyRequests })));

        facade.analyzeText('apple');
        facade.analyzePhoto('asset-1');

        expect(facade.text.errorKey()).toBe('AI_INPUT_BAR.TEXT_ERROR_PREMIUM');
        expect(facade.photo.errorKey()).toBe('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
    });

    it('handles empty edit results and can clear the complete channel state', () => {
        facade.setEditResult(facade.text, [], null);

        expect(facade.text.errorKey()).toBe('AI_INPUT_BAR.EMPTY_ITEMS_ERROR');

        facade.clear(facade.text);

        expect(facade.text.errorKey()).toBeNull();
        expect(facade.text.results()).toEqual([]);
        expect(facade.text.nutrition()).toBeNull();
    });
});

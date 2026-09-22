import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AiFoodService } from '../api/ai-food.service';
import { FoodRecognitionService } from '../api/food-recognition.service';
import type { FoodVisionResponse } from '../models/ai.data';
import { AiFoodFacade } from './ai-food.facade';

describe('AiFoodFacade transport delegation', () => {
    const ai = { analyzeFoodImage: vi.fn(), parseFoodText: vi.fn(), calculateNutrition: vi.fn() };
    const jobs = { resume: vi.fn(), list: vi.fn() };
    let facade: AiFoodFacade;
    beforeEach(() => {
        vi.resetAllMocks();
        TestBed.configureTestingModule({
            providers: [AiFoodFacade, { provide: AiFoodService, useValue: ai }, { provide: FoodRecognitionService, useValue: jobs }],
        });
        facade = TestBed.inject(AiFoodFacade);
    });
    it('preserves the request and response for all AI entry points', () => {
        const response = of({ items: [] });
        ai.analyzeFoodImage.mockReturnValue(response);
        ai.parseFoodText.mockReturnValue(response);
        ai.calculateNutrition.mockReturnValue(response);
        jobs.resume.mockReturnValue(response);
        jobs.list.mockReturnValue(response);
        expect(facade.analyzeFoodImage({ imageAssetId: 'asset' })).toBe(response);
        expect(ai.analyzeFoodImage).toHaveBeenCalledWith({ imageAssetId: 'asset' });
        expect(facade.parseFoodText({ text: 'apple' })).toBe(response);
        expect(ai.parseFoodText).toHaveBeenCalledWith({ text: 'apple' });
        expect(facade.calculateNutrition({ items: [] })).toBe(response);
        expect(ai.calculateNutrition).toHaveBeenCalledWith({ items: [] });
        expect(facade.resumeRecognition('job')).toBe(response);
        expect(jobs.resume).toHaveBeenCalledWith('job');
        expect(facade.listRecognitions()).toBe(response);
        expect(jobs.list).toHaveBeenCalledOnce();
    });
    it('passes errors through and lets callers cancel a pending recognition', () => {
        const pending = new Subject<FoodVisionResponse>();
        jobs.resume.mockReturnValue(pending);
        const error = vi.fn();
        facade.resumeRecognition('job').subscribe({ error });
        const failure = new Error('offline');
        pending.error(failure);
        expect(error).toHaveBeenCalledWith(failure);
        const next = new Subject<FoodVisionResponse>();
        jobs.resume.mockReturnValue(next);
        const subscription = facade.resumeRecognition('next').subscribe();
        subscription.unsubscribe();
        expect(next.observed).toBe(false);
    });
});

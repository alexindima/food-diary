import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import type { FoodRecognitionJob } from '../../../shared/models/food-recognition.data';
import { FoodRecognitionHistoryComponent } from './food-recognition-history';

const listRecognitions = vi.fn();
beforeEach(() => {
    listRecognitions.mockReset();
    TestBed.configureTestingModule({
        imports: [FoodRecognitionHistoryComponent],
        providers: [{ provide: AiFoodFacade, useValue: { listRecognitions } }],
    });
    TestBed.overrideComponent(FoodRecognitionHistoryComponent, { set: { template: '' } });
});
describe('recognition history recovery', () => {
    it.each([true, false])('keeps product-label and meal recognition histories separate: %s', productLabel => {
        const meal: FoodRecognitionJob = {
            id: 'meal', imageAssetId: 'image', imageUrl: 'photo.jpg', description: null,
            status: 'Succeeded', createdOnUtc: '2026-09-24T00:00:00Z', updatedOnUtc: '2026-09-24T00:00:00Z',
            vision: null, nutrition: null, errorCode: null, nutritionErrorCode: null,
        };
        const entries: FoodRecognitionJob[] = [meal, { ...meal, id: 'label', isProductLabel: true }];
        listRecognitions.mockReturnValue(of(entries));
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        fixture.componentRef.setInput('productLabel', productLabel);
        fixture.componentInstance['load']();
        expect(fixture.componentInstance['jobs']().map(job => job.id)).toEqual([productLabel ? 'label' : 'meal']);
    });
    it('distinguishes a failed request from empty history and permits retry', () => {
        listRecognitions.mockReturnValueOnce(throwError(() => new Error('offline'))).mockReturnValueOnce(of([]));
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        const component = fixture.componentInstance;
        component['load']();
        expect(component['failed']()).toBe(true);
        expect(component['loading']()).toBe(false);
        component['load']();
        expect(component['failed']()).toBe(false);
        expect(component['loaded']()).toBe(true);
        expect(component['jobs']()).toEqual([]);
    });
    it('prevents duplicate history requests and unsubscribes on destruction', () => {
        const pending = new Subject<FoodRecognitionJob[]>();
        listRecognitions.mockReturnValue(pending);
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        fixture.componentInstance['load']();
        fixture.componentInstance['load']();
        expect(listRecognitions).toHaveBeenCalledTimes(1);
        fixture.destroy();
        expect(pending.observed).toBe(false);
    });
    it('does not request history while its parent is busy', () => {
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        fixture.componentRef.setInput('disabled', true);
        fixture.componentInstance['load']();
        expect(listRecognitions).not.toHaveBeenCalled();
    });
});

import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import type { FoodRecognitionJob } from '../../../shared/models/food-recognition.data';
import type { PageOf } from '../../../shared/models/page-of.data';
import { FoodRecognitionHistoryComponent } from './food-recognition-history';

const PAGE_SIZE = 20;
const TOTAL_ITEMS = 41;
const listRecognitions = vi.fn();
const deleteRecognition = vi.fn();
beforeEach(() => {
    listRecognitions.mockReset().mockReturnValue(of(pageOf([])));
    deleteRecognition.mockReset();
    TestBed.configureTestingModule({
        imports: [FoodRecognitionHistoryComponent],
        providers: [provideTranslateService(), { provide: AiFoodFacade, useValue: { listRecognitions, deleteRecognition } }],
    });
    TestBed.overrideComponent(FoodRecognitionHistoryComponent, { set: { template: '' } });
});
describe('recognition history recovery', () => {
    it.each([true, false])('keeps product-label and meal recognition histories separate: %s', productLabel => {
        const meal: FoodRecognitionJob = {
            id: 'meal',
            imageAssetId: 'image',
            imageUrl: 'photo.jpg',
            description: null,
            status: 'Succeeded',
            createdOnUtc: '2026-09-24T00:00:00Z',
            updatedOnUtc: '2026-09-24T00:00:00Z',
            vision: null,
            nutrition: null,
            errorCode: null,
            nutritionErrorCode: null,
        };
        const entries: FoodRecognitionJob[] = [meal, { ...meal, id: 'label', isProductLabel: true }];
        listRecognitions.mockReturnValue(of(pageOf(entries.filter(job => (job.isProductLabel ?? false) === productLabel))));
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        fixture.componentRef.setInput('productLabel', productLabel);
        fixture.componentInstance['load']();
        expect(listRecognitions).toHaveBeenCalledWith(1, PAGE_SIZE, productLabel);
        expect(fixture.componentInstance['jobs']().map(job => job.id)).toEqual([productLabel ? 'label' : 'meal']);
    });
    it('distinguishes a failed request from empty history and permits retry', () => {
        listRecognitions.mockReturnValueOnce(throwError(() => new Error('offline'))).mockReturnValueOnce(of(pageOf([])));
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
        const pending = new Subject<PageOf<FoodRecognitionJob>>();
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

describe('recognition history deletion', () => {
    it('only removes a completed result after server success and keeps it on failure', () => {
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        const component = fixture.componentInstance;
        const job: FoodRecognitionJob = {
            id: 'result',
            status: 'Succeeded',
            imageAssetId: 'asset',
            imageUrl: 'photo.jpg',
            description: null,
            createdOnUtc: '2026-09-25T00:00:00Z',
            updatedOnUtc: '2026-09-25T00:00:00Z',
            vision: null,
            nutrition: null,
            errorCode: null,
            nutritionErrorCode: null,
        };
        component['jobs'].set([job]);
        deleteRecognition.mockReturnValueOnce(throwError(() => new Error('offline')));
        component['remove'](job);
        expect(component['jobs']()).toEqual([job]);
        expect(component['deleteFailed']()).toBe(true);
        const pending = new Subject<void>();
        deleteRecognition.mockReturnValue(pending);
        component['remove'](job);
        component['remove'](job);
        expect(deleteRecognition).toHaveBeenCalledTimes(2);
        expect(component['jobs']()).toEqual([job]);
        component['pageIndex'].set(1);
        component['totalItems'].set(PAGE_SIZE + 1);
        listRecognitions.mockReturnValue(of(pageOf([], 1, PAGE_SIZE)));
        pending.next();
        pending.complete();
        expect(listRecognitions).toHaveBeenLastCalledWith(1, PAGE_SIZE, false);
        expect(component['pageIndex']()).toBe(0);
        expect(component['totalItems']()).toBe(PAGE_SIZE);
        expect(component['jobs']()).toEqual([]);
        expect(component['deleted']()).toBe(true);
        expect(component['deleting']()).toEqual([]);
    });
    it.each(['Queued', 'Running'] as const)('does not delete active jobs: %s', status => {
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        const job: FoodRecognitionJob = {
            id: 'active',
            status,
            imageAssetId: 'asset',
            imageUrl: 'photo.jpg',
            description: null,
            createdOnUtc: '',
            updatedOnUtc: '',
            vision: null,
            nutrition: null,
            errorCode: null,
            nutritionErrorCode: null,
        };
        fixture.componentInstance['remove'](job);
        expect(deleteRecognition).not.toHaveBeenCalled();
    });
});

function pageOf(data: FoodRecognitionJob[], page = 1, totalItems = data.length): PageOf<FoodRecognitionJob> {
    return { data, page, limit: PAGE_SIZE, totalItems, totalPages: Math.ceil(totalItems / PAGE_SIZE) };
}

describe('recognition history pagination', () => {
    it('requests the selected page and emits the server total', () => {
        listRecognitions.mockReturnValue(of(pageOf([], 2, TOTAL_ITEMS)));
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        fixture.componentRef.setInput('productLabel', true);
        const count = vi.fn();
        fixture.componentInstance.countChanged.subscribe(count);
        fixture.componentInstance['load'](1);
        expect(listRecognitions).toHaveBeenCalledWith(2, PAGE_SIZE, true);
        expect(fixture.componentInstance['pageIndex']()).toBe(1);
        expect(fixture.componentInstance['totalItems']()).toBe(TOTAL_ITEMS);
        expect(count).toHaveBeenCalledWith(TOTAL_ITEMS);
    });
    it('keeps the current page and total after a failed page request', () => {
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        const component = fixture.componentInstance;
        component['totalItems'].set(TOTAL_ITEMS);
        listRecognitions.mockReturnValue(throwError(() => new Error('offline')));
        component['load'](1);
        expect(component['pageIndex']()).toBe(0);
        expect(component['totalItems']()).toBe(TOTAL_ITEMS);
        expect(component['failed']()).toBe(true);
    });
});

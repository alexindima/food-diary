import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import type { FoodRecognitionJob } from '../../../shared/models/food-recognition.data';
import type { PageOf } from '../../../shared/models/page-of.data';
import { FoodRecognitionHistoryComponent } from './food-recognition-history';

const PAGE_SIZE = 20;
const EXPIRED_PAGE = 3;
const HISTORY_POLL_INTERVAL = 5000;
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

const historyJob: FoodRecognitionJob = {
    id: 'history-result',
    status: 'Succeeded',
    imageAssetId: 'asset',
    imageUrl: '/photo.jpg',
    description: null,
    createdOnUtc: '2026-09-25T00:00:00Z',
    updatedOnUtc: '2026-09-25T00:00:00Z',
    vision: null,
    nutrition: null,
    errorCode: null,
    nutritionErrorCode: null,
};

describe('recognition history rendered interactions', () => {
    it('shows empty state only after loading and returns through the actual button', () => {
        const pending = new Subject<PageOf<FoodRecognitionJob>>();
        listRecognitions.mockReturnValue(pending);
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        fixture.componentRef.setInput('screen', true);
        const back = vi.fn();
        fixture.componentInstance.backToRecognition.subscribe(back);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const empty = requiredElement(host, '.history-list__empty');
        expect(empty.style.display).toBe('none');
        pending.next(pageOf([]));
        pending.complete();
        fixture.detectChanges();
        expect(empty.style.display).not.toBe('none');
        requiredElement(empty, 'button').click();
        expect(back).toHaveBeenCalledOnce();
    });
    it('keeps deletion separate from opening a result and renders empty state after the last deletion', () => {
        listRecognitions.mockReturnValueOnce(of(pageOf([historyJob]))).mockReturnValue(of(pageOf([])));
        deleteRecognition.mockReturnValue(of(undefined));
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        fixture.componentRef.setInput('screen', true);
        const selected = vi.fn();
        fixture.componentInstance.recognitionSelected.subscribe(selected);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        requiredElement(host, '.history-list__open').click();
        expect(selected).toHaveBeenCalledWith(historyJob);
        selected.mockClear();
        requiredElement(host, '[aria-label="AI_RECOGNITION.DELETE"]').click();
        fixture.detectChanges();
        expect(selected).not.toHaveBeenCalled();
        expect(deleteRecognition).toHaveBeenCalledWith(historyJob.id);
        expect(host.querySelector('.history-list__row')).toBeNull();
        expect(requiredElement(host, '.history-list__empty').style.display).not.toBe('none');
        expect(fixture.componentInstance['totalItems']()).toBe(0);
    });
    it('refetches the last valid page when automatic cleanup removes the requested page', () => {
        listRecognitions
            .mockReturnValueOnce(of(pageOf([], EXPIRED_PAGE, PAGE_SIZE + 1)))
            .mockReturnValueOnce(of(pageOf([historyJob], 2, PAGE_SIZE + 1)));
        const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
        fixture.componentRef.setInput('productLabel', true);
        fixture.componentInstance['load'](2);
        expect(listRecognitions.mock.calls).toEqual([
            [EXPIRED_PAGE, PAGE_SIZE, true],
            [2, PAGE_SIZE, true],
        ]);
        expect(fixture.componentInstance['pageIndex']()).toBe(1);
        expect(fixture.componentInstance['jobs']()).toEqual([historyJob]);
    });
    it('polls the current page until completion and stops on destruction', () => {
        vi.useFakeTimers();
        try {
            listRecognitions
                .mockReturnValueOnce(of(pageOf([{ ...historyJob, status: 'Running' }], 2, PAGE_SIZE + 1)))
                .mockReturnValue(of(pageOf([historyJob], 2, PAGE_SIZE + 1)));
            const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
            fixture.componentInstance['load'](1);
            vi.advanceTimersByTime(HISTORY_POLL_INTERVAL);
            expect(listRecognitions).toHaveBeenLastCalledWith(2, PAGE_SIZE, false);
            expect(listRecognitions).toHaveBeenCalledTimes(2);
            vi.advanceTimersByTime(HISTORY_POLL_INTERVAL);
            expect(listRecognitions).toHaveBeenCalledTimes(2);
            fixture.destroy();
            vi.advanceTimersByTime(HISTORY_POLL_INTERVAL);
            expect(listRecognitions).toHaveBeenCalledTimes(2);
        } finally {
            vi.useRealTimers();
        }
    });
});

it('blocks pagination and duplicate mutations while deletion is pending, then permits retry after failure', () => {
    listRecognitions.mockReturnValue(of(pageOf([historyJob], 1, PAGE_SIZE + 1)));
    const pending = new Subject<void>();
    deleteRecognition.mockReturnValue(pending);
    const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
    fixture.componentRef.setInput('screen', true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    requiredElement(host, '[aria-label="AI_RECOGNITION.DELETE"]').click();
    fixture.detectChanges();
    expect(requiredElement(host, 'fd-ui-pagination').parentElement?.hasAttribute('inert')).toBe(true);
    fixture.componentInstance['load'](1);
    fixture.componentInstance['remove'](historyJob);
    expect(listRecognitions).toHaveBeenCalledTimes(1);
    expect(deleteRecognition).toHaveBeenCalledTimes(1);
    pending.error(new Error('offline'));
    fixture.detectChanges();
    expect(requiredElement(host, '[role="alert"]').hidden).toBe(false);
    expect(host.querySelectorAll('.history-list__row')).toHaveLength(1);
    expect(requiredElement(host, 'fd-ui-pagination').parentElement?.hasAttribute('inert')).toBe(false);
});

it('renders a retry instead of an empty history when loading fails', () => {
    listRecognitions.mockReturnValueOnce(throwError(() => new Error('offline'))).mockReturnValueOnce(of(pageOf([])));
    const fixture = TestBed.createComponent(FoodRecognitionHistoryComponent);
    fixture.componentRef.setInput('screen', true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(requiredElement(host, '.history-list__empty').style.display).toBe('none');
    const retry = Array.from(host.querySelectorAll<HTMLButtonElement>('button')).find(button =>
        button.textContent.includes('AI_RECOGNITION.RETRY'),
    );
    if (retry === undefined) {
        throw new Error('Retry button missing');
    }
    retry.click();
    fixture.detectChanges();
    expect(requiredElement(host, '.history-list__empty').style.display).not.toBe('none');
});

function requiredElement(host: Element, selector: string): HTMLElement {
    const element = host.querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Missing element: ${selector}`);
    }
    return element;
}

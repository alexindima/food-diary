import { TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { type FoodRecognitionJob, RECOGNITION_PAGE_SIZE } from '../../../shared/models/food-recognition.data';
import { FoodRecognitionHistoryDialogComponent } from './food-recognition-history-dialog';

describe('FoodRecognitionHistoryDialogComponent', () => {
    const close = vi.fn();
    const listRecognitions = vi.fn();
    const job: FoodRecognitionJob = {
        id: 'job-1',
        imageAssetId: 'asset-1',
        imageUrl: 'photo.jpg',
        description: null,
        status: 'Succeeded',
        createdOnUtc: '2026-05-17T00:00:00Z',
        updatedOnUtc: '2026-05-17T00:00:00Z',
        vision: { items: [{ nameEn: 'Apple', amount: 100, unit: 'g', confidence: 1 }] },
        nutrition: null,
        errorCode: null,
        nutritionErrorCode: null,
    };

    beforeEach(() => {
        close.mockReset();
        listRecognitions.mockReset().mockReturnValue(of({ data: [], page: 1, limit: 20, totalPages: 0, totalItems: 0 }));
        TestBed.configureTestingModule({
            imports: [FoodRecognitionHistoryDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: AiFoodFacade, useValue: { listRecognitions } },
                { provide: FdUiDialogRef, useValue: { close } },
            ],
        });
    });

    it('loads meal history on opening and lets the user return from the empty state', () => {
        const fixture = TestBed.createComponent(FoodRecognitionHistoryDialogComponent);
        fixture.detectChanges();

        expect(listRecognitions).toHaveBeenCalledWith(1, RECOGNITION_PAGE_SIZE, false);
        const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button'));
        buttons.find(button => button.textContent.includes('AI_RECOGNITION.EMPTY_BACK'))?.click();

        expect(close).toHaveBeenCalledWith();
    });

    it('returns the selected saved job without starting recognition', () => {
        listRecognitions.mockReturnValueOnce(of({ data: [job], page: 1, limit: 20, totalPages: 1, totalItems: 1 }));
        const fixture = TestBed.createComponent(FoodRecognitionHistoryDialogComponent);
        fixture.detectChanges();
        const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button'));
        buttons.find(button => button.textContent.includes('Apple'))?.click();

        expect(close).toHaveBeenCalledWith(job);
    });
});

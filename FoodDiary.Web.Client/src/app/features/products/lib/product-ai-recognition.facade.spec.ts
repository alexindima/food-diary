import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { AiFoodService } from '../../../shared/api/ai-food.service';
import { FoodRecognitionService } from '../../../shared/api/food-recognition.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import { ProductAiRecognitionFacade } from './product-ai-recognition.facade';

const TOTAL_RECOGNITIONS = 42;

it('requests only one product-label row and uses the total rather than the page length', () => {
    const list = vi.fn().mockReturnValue(of({ data: [], totalItems: TOTAL_RECOGNITIONS }));
    TestBed.configureTestingModule({
        providers: [
            ProductAiRecognitionFacade,
            { provide: AiFoodService, useValue: {} },
            { provide: ImageUploadService, useValue: {} },
            { provide: FoodRecognitionService, useValue: { list } },
        ],
    });
    const count = vi.fn();
    TestBed.inject(ProductAiRecognitionFacade).recognitionCount().subscribe(count);
    expect(list).toHaveBeenCalledWith(1, 1, true);
    expect(count).toHaveBeenCalledWith(TOTAL_RECOGNITIONS);
});

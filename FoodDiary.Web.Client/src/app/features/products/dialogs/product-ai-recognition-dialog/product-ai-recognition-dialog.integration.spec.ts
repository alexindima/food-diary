import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { FoodVisionResponse } from '../../../../shared/models/ai.data';
import { imageSelection } from '../../../../shared/models/image-upload.data';
import { MeasurementUnit } from '../../../../shared/models/product.data';
import { ProductAiRecognitionFacade } from '../../lib/product-ai-recognition.facade';
import { ProductAiRecognitionDialogComponent } from './product-ai-recognition-dialog';
import type { ProductAiDialogData, ProductAiRecognitionResult } from './product-ai-recognition-dialog.types';

it('shows label recognition progress and requires confirmation before applying edits and a cover', () => {
    const response = new Subject<FoodVisionResponse>();
    const analyzeFoodImage = vi.fn<ProductAiRecognitionFacade['analyzeFoodImage']>().mockReturnValue(response);
    const calculateNutrition = vi.fn<ProductAiRecognitionFacade['calculateNutrition']>();
    const recognitionCount = vi.fn<ProductAiRecognitionFacade['recognitionCount']>().mockReturnValue(of(0));
    const close = vi.fn<(result: ProductAiRecognitionResult | null) => void>();
    const photo = imageSelection('https://images.example.test/label.jpg', 'label-photo');
    const photoOfIngredients = imageSelection('https://images.example.test/ingredients.jpg', 'ingredients-photo');
    const data: ProductAiDialogData = {
        initialDescription: '  Nutrition label  ',
        initialPhotos: [photo, photoOfIngredients],
        hasExistingData: true,
    };
    TestBed.configureTestingModule({
        imports: [ProductAiRecognitionDialogComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: data },
            { provide: FdUiDialogRef, useValue: { close } },
            { provide: ProductAiRecognitionFacade, useValue: { analyzeFoodImage, calculateNutrition, recognitionCount } },
        ],
    });
    const fixture = TestBed.createComponent(ProductAiRecognitionDialogComponent);
    const dialog = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();

    action(dialog, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    fixture.detectChanges();

    expect(analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({
        imageAssetId: photo.assetId,
        additionalImageAssetIds: [photoOfIngredients.assetId],
        description: 'Nutrition label',
        isProductLabel: true,
    });
    expect(dialog.querySelector('.product-ai-dialog__status[role="status"]')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_ANALYZING');
    expect(dialog.querySelector<HTMLButtonElement>('.product-ai-dialog__history-link button')?.disabled).toBe(true);

    response.next({
        items: [],
        productLabel: {
            name: 'Yogurt',
            brand: 'Farm',
            baseAmount: 100,
            baseUnit: 'g',
            calories: 75,
            protein: 5,
            fat: 2,
            carbs: 9,
            fiber: null,
            alcohol: null,
            notes: 'Fiber not listed',
        },
    });
    response.complete();
    fixture.detectChanges();

    expect(calculateNutrition).not.toHaveBeenCalled();
    expect(dialog.querySelector('details')?.textContent).toContain('Fiber not listed');
    expect(dialog.querySelector<HTMLInputElement>('fd-product-ai-recognition-result input')?.value).toBe('Yogurt');
    expect(action(dialog, 'PRODUCT_AI_DIALOG.APPLY').disabled).toBe(true);
    expect(close).not.toHaveBeenCalled();

    dialog.querySelector<HTMLInputElement>('#product-ai-replace')?.click();
    dialog.querySelector<HTMLInputElement>('#product-ai-cover')?.click();
    fixture.detectChanges();
    action(dialog, 'PRODUCT_AI_DIALOG.APPLY').click();

    expect(close).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({
            name: 'Yogurt',
            brand: 'Farm',
            baseUnit: MeasurementUnit.G,
            caloriesPerBase: 75,
            fiberPerBase: null,
            image: photo,
            images: [photo, photoOfIngredients],
        }),
    );
});

function action(dialog: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(dialog.querySelectorAll<HTMLButtonElement>('.product-ai-dialog__footer button')).find(
        item => item.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Missing dialog action: ${label}`);
    }
    return button;
}

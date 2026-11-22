import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { imageSelection } from '../../../../../shared/models/image-upload.data';
import { ProductRecognitionPhotosComponent } from './product-recognition-photos';
const openPreview = vi.fn();
const photo = (index: number): ImageSelection => imageSelection(`https://example.test/${index}.jpg`, `image-${index}`);
beforeEach(() => {
    TestBed.configureTestingModule({
        imports: [ProductRecognitionPhotosComponent],
        providers: [{ provide: FdUiDialogService, useValue: { open: openPreview } }],
    });
    TestBed.overrideComponent(ProductRecognitionPhotosComponent, { set: { template: '' } });
});
describe('product recognition photos', () => {
    it('removes only the selected photo and preserves order', () => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput('photos', [photo(0), photo(1), photo(2)]);
        fixture.componentInstance['remove'](photo(1));
        expect(fixture.componentInstance.photos()).toEqual([photo(0), photo(2)]);
    });
    it('keeps photos when recognition or upload prevents deletion', () => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput('photos', [photo(0)]);
        fixture.componentRef.setInput('disabled', true);
        fixture.componentInstance['remove'](photo(0));
        expect(fixture.componentInstance.photos()).toEqual([photo(0)]);
        fixture.componentRef.setInput('disabled', false);
        fixture.componentInstance.uploading.set(true);
        fixture.componentInstance['remove'](photo(0));
        expect(fixture.componentInstance.photos()).toEqual([photo(0)]);
    });
    it('clears the cover when its photo is removed, without selecting another automatically', () => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput('photos', [photo(0), photo(1)]);
        fixture.componentInstance.cover.set(photo(0));
        fixture.componentInstance['remove'](photo(1));
        expect(fixture.componentInstance.cover()).toEqual(photo(0));
        fixture.componentInstance['remove'](photo(0));
        expect(fixture.componentInstance.cover()).toBeNull();
    });
});

describe('product editor photo slots', () => {
    it.each([
        { count: 0, empty: 0 },
        { count: 1, empty: 4 },
        { count: 2, empty: 3 },
        { count: 5, empty: 0 },
    ])('keeps four secondary slots with $count photos', ({ count, empty }) => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput(
            'photos',
            Array.from({ length: count }, (_, index) => photo(index)),
        );
        expect(fixture.componentInstance['emptySlots']()).toHaveLength(empty);
    });
});

import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { ProductRecognitionPhotosComponent } from './product-recognition-photos';
const MAX_PHOTOS = 5;
const openPreview = vi.fn();
const photo = (index: number): ImageSelection => ({ assetId: `image-${index}`, url: `https://example.test/${index}.jpg` });
beforeEach(() => {
    TestBed.configureTestingModule({
        imports: [ProductRecognitionPhotosComponent],
        providers: [{ provide: FdUiDialogService, useValue: { open: openPreview } }],
    });
    TestBed.overrideComponent(ProductRecognitionPhotosComponent, { set: { template: '' } });
});
describe('product recognition photos', () => {
    it('adds distinct photos in order, clears the uploader and stops at five', () => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput('photos', []);
        const component = fixture.componentInstance;
        for (let index = 0; index <= MAX_PHOTOS; index++) {
            component['add'](photo(index));
        }
        expect(component.photos()).toHaveLength(MAX_PHOTOS);
        expect(component.photos()[0]).toEqual(photo(0));
        component['add'](photo(0));
        expect(component.photos()).toHaveLength(MAX_PHOTOS);
        component['add'](null);
        expect(component.photos()).toHaveLength(MAX_PHOTOS);
    });
    it('removes only the selected photo and preserves order', () => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput('photos', [photo(0), photo(1), photo(2)]);
        fixture.componentInstance['remove'](photo(1));
        expect(fixture.componentInstance.photos()).toEqual([photo(0), photo(2)]);
    });
    it('does not mutate photos during recognition or remove a photo during upload', () => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput('photos', [photo(0)]);
        fixture.componentRef.setInput('disabled', true);
        fixture.componentInstance['add'](photo(1));
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

it('opens the selected original without changing the cover or photo order', () => {
    const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
    fixture.componentRef.setInput('photos', [photo(0), photo(1)]);
    fixture.componentInstance.cover.set(photo(0));
    fixture.componentInstance['preview'](1);
    expect(openPreview).toHaveBeenCalledWith(
        expect.anything(),
        expect.objectContaining({ data: { collageImages: [{ url: photo(0).url }, { url: photo(1).url }], initialIndex: 1 } }),
    );
    expect(fixture.componentInstance.cover()).toEqual(photo(0));
    expect(fixture.componentInstance.photos()).toEqual([photo(0), photo(1)]);
});

describe('product editor photo slots', () => {
    it.each([
        { count: 0, empty: 0 },
        { count: 1, empty: 4 },
        { count: 2, empty: 3 },
        { count: 5, empty: 0 },
    ])('keeps four secondary slots with $count photos', ({ count, empty }) => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput('editor', true);
        fixture.componentRef.setInput(
            'photos',
            Array.from({ length: count }, (_, index) => photo(index)),
        );
        expect(fixture.componentInstance['emptySlots']()).toHaveLength(empty);
    });
    it('does not add placeholder slots to recognition', () => {
        const fixture = TestBed.createComponent(ProductRecognitionPhotosComponent);
        fixture.componentRef.setInput('photos', [photo(0)]);
        expect(fixture.componentInstance['emptySlots']()).toEqual([]);
    });
});

import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { ProductRecognitionPhotosComponent } from './product-recognition-photos';
const MAX_PHOTOS = 5;
const photo = (index: number): ImageSelection => ({ assetId: `image-${index}`, url: `https://example.test/${index}.jpg` });
beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ProductRecognitionPhotosComponent] });
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

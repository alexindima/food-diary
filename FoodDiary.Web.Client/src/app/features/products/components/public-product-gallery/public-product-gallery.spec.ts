import { OverlayContainer } from '@angular/cdk/overlay';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { PublicProductGalleryComponent } from './public-product-gallery';

describe('Public product photo preview', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('opens the selected photo and falls back to the first image when the collection changes', async () => {
        vi.stubGlobal(
            'matchMedia',
            vi.fn(() => ({ matches: false })),
        );
        TestBed.configureTestingModule({ providers: [provideTranslateTesting()] });
        const fixture = TestBed.createComponent(PublicProductGalleryComponent);
        fixture.componentRef.setInput('images', ['/front.jpg', '/ingredients.jpg']);
        fixture.componentRef.setInput('name', 'Pears');
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        galleryButton(host, '.thumbnails button:nth-child(2)').click();
        fixture.detectChanges();
        expect(galleryButton(host, '.thumbnails button:nth-child(2)').getAttribute('aria-pressed')).toBe('true');
        expect(host.querySelector('.cover img')?.getAttribute('src')).toBe('/ingredients.jpg');

        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        galleryButton(host, '.cover').click();
        fixture.detectChanges();
        await fixture.whenStable();
        const largeImage = '.fd-ui-image-preview-dialog__image';
        expect(overlay.querySelector(largeImage)?.getAttribute('src')).toBe('/ingredients.jpg');
        expect(overlay.querySelector(largeImage)?.getAttribute('alt')).toBe('Pears');
        galleryButton(overlay, '.fd-ui-image-preview-dialog__arrow--previous button').click();
        fixture.detectChanges();
        expect(overlay.querySelector(largeImage)?.getAttribute('src')).toBe('/front.jpg');
        galleryButton(overlay, '.fd-ui-dialog__close-button').click();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(overlay.querySelector('fd-ui-image-preview-dialog')).toBeNull();
        expect(host.querySelector('.cover img')?.getAttribute('src')).toBe('/ingredients.jpg');

        fixture.componentRef.setInput('images', ['/new-front.jpg']);
        fixture.detectChanges();
        expect(host.querySelector('.cover img')?.getAttribute('src')).toBe('/new-front.jpg');
        expect(host.querySelector('.thumbnails')).toBeNull();
    });
});

function galleryButton(root: HTMLElement, selector: string): HTMLButtonElement {
    const button = root.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected the product gallery button ${selector}`);
    }
    return button;
}

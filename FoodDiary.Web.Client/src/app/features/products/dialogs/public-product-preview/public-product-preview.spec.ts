import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA, FdUiDialogRef, FdUiDialogService } from 'fd-ui-kit';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { PublicProductDetailsFacade } from '../../lib/public-product-details.facade';
import type { PublicProduct } from '../../models/public-product.data';
import { PublicProductPreviewComponent } from './public-product-preview';

const product: PublicProduct = {
    id: 'product',
    name: 'Granola',
    brand: null,
    imageUrl: null,
    baseUnit: 'G',
    baseAmount: 100,
    calories: 450,
    proteins: 10,
    fats: 15,
    carbs: 65,
    fiber: 8,
    alcohol: 0,
};

async function setupAsync(data: PublicProduct): Promise<{
    fixture: ComponentFixture<PublicProductPreviewComponent>;
    dialogs: { open: ReturnType<typeof vi.fn> };
    element: HTMLElement;
}> {
    const dialogs = { open: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [PublicProductPreviewComponent],
        providers: [
            ...provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: data.id },
            { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
            { provide: FdUiDialogService, useValue: dialogs },
            { provide: PublicProductDetailsFacade, useValue: { getById: vi.fn().mockReturnValue(of(data)) } },
        ],
    }).compileComponents();
    const fixture = TestBed.createComponent(PublicProductPreviewComponent);
    fixture.detectChanges();
    return { fixture, dialogs, element: fixture.nativeElement as HTMLElement };
}

describe('PublicProductPreviewComponent', () => {
    it('does not reserve a gallery column when no images exist', async () => {
        const { element } = await setupAsync(product);
        expect(element.querySelector('fd-public-product-gallery')).toBeNull();
        expect(element.querySelector('.with-photo')).toBeNull();
        expect(element.querySelector('.brand')?.hasAttribute('hidden')).toBe(true);
        expect(element.querySelector('.product-details h2')?.textContent).toContain('Granola');
    });

    it('shows the legacy cover without a thumbnail strip', async () => {
        const { element } = await setupAsync({ ...product, imageUrl: 'cover.webp' });
        expect(element.querySelector('.cover img')?.getAttribute('src')).toBe('cover.webp');
        expect(element.querySelector('.thumbnails')).toBeNull();
    });

    it('deduplicates the cover and opens the selected image in the full gallery', async () => {
        const { element, fixture, dialogs } = await setupAsync({
            ...product,
            imageUrl: 'cover.webp',
            images: ['cover.webp', 'back.webp', 'side.webp'],
            description: 'Crunchy oats',
        });
        const buttons = element.querySelectorAll<HTMLButtonElement>('.thumbnails button');
        expect(buttons.length).toBe(['cover.webp', 'back.webp', 'side.webp'].length);
        buttons[1].click();
        fixture.detectChanges();
        expect(element.querySelector('.cover img')?.getAttribute('src')).toBe('back.webp');
        expect(buttons[1].getAttribute('aria-pressed')).toBe('true');
        element.querySelector<HTMLButtonElement>('.cover')?.click();
        const config = dialogs.open.mock.calls[0]?.[1] as {
            data: { initialIndex: number; collageImages: Array<{ url: string; alt: string }> };
        };
        expect(config.data.initialIndex).toBe(1);
        expect(config.data.collageImages.map(image => image.url)).toEqual(['cover.webp', 'back.webp', 'side.webp']);
        expect(element.querySelector('.description')?.textContent).toBe('Crunchy oats');
    });
});

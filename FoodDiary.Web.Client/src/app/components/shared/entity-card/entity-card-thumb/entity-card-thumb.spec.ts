import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import type { EntityCardCollageState, EntityCardPreviewInteractionState } from '../entity-card-lib/entity-card.types';
import { EntityCardThumbComponent } from './entity-card-thumb';

function setupEntityCardThumb(hasPreviewImage: boolean): ComponentFixture<EntityCardThumbComponent> {
    TestBed.configureTestingModule({
        imports: [EntityCardThumbComponent],
    });

    const fixture = TestBed.createComponent(EntityCardThumbComponent);
    fixture.componentRef.setInput('imageAlt', 'Image');
    fixture.componentRef.setInput('imageIcon', 'restaurant');
    fixture.componentRef.setInput('collage', { images: [], count: 0, hasImages: false } satisfies EntityCardCollageState);
    fixture.componentRef.setInput('hasPreviewImage', hasPreviewImage);
    fixture.componentRef.setInput('previewInteraction', {
        hint: null,
        role: null,
        tabIndex: null,
        ariaLabel: null,
    } satisfies EntityCardPreviewInteractionState);
    return fixture;
}

describe('EntityCardThumbComponent', () => {
    it('emits preview only when preview image is available', () => {
        const fixture = setupEntityCardThumb(true);
        fixture.componentRef.setInput('imageUrl', '/preview.webp');
        const preview = vi.fn();
        fixture.componentInstance.preview.subscribe(preview);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        const parentClick = vi.fn();
        root.addEventListener('click', parentClick);
        const thumb = root.querySelector<HTMLElement>('.entity-card__thumb');
        expect(thumb?.querySelector('img')?.getAttribute('src')).toBe('/preview.webp');

        thumb?.click();

        expect(preview).toHaveBeenCalledOnce();
        expect(parentClick).not.toHaveBeenCalled();
    });

    it('does not emit preview when there is no preview image', () => {
        const fixture = setupEntityCardThumb(false);
        const component = fixture.componentInstance;
        const previewSpy = vi.fn();
        component['preview'].subscribe(previewSpy);
        fixture.detectChanges();

        component['previewCardImage']({ stopPropagation: vi.fn() } as unknown as Event);

        expect(previewSpy).not.toHaveBeenCalled();
    });
});

describe('EntityCardThumbComponent image recovery', () => {
    it('shows an icon after an image error and retries a replacement URL', () => {
        const fixture = setupEntityCardThumb(true);
        fixture.componentRef.setInput('imageUrl', '/missing-image.webp');
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        root.querySelector('img')?.dispatchEvent(new Event('error'));
        fixture.detectChanges();

        expect(root.querySelector('img')).toBeNull();
        expect(root.querySelector('.entity-card__placeholder-icon')).not.toBeNull();
        const preview = vi.fn();
        fixture.componentInstance.preview.subscribe(preview);
        root.querySelector<HTMLElement>('.entity-card__thumb')?.click();
        expect(preview).toHaveBeenCalledOnce();

        fixture.componentRef.setInput('imageUrl', '/replacement.webp');
        fixture.detectChanges();
        expect(root.querySelector('img')?.getAttribute('src')).toBe('/replacement.webp');
        expect(root.querySelector('.entity-card__placeholder-icon')).toBeNull();
    });

    it('retains healthy collage images and shows the fallback when all fail', () => {
        const fixture = setupEntityCardThumb(true);
        fixture.componentRef.setInput('collage', {
            images: [
                { url: '/missing.webp', alt: 'Missing' },
                { url: '/healthy.webp', alt: 'Healthy' },
            ],
            count: 2,
            hasImages: true,
        } satisfies EntityCardCollageState);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        root.querySelector('img')?.dispatchEvent(new Event('error'));
        fixture.detectChanges();

        expect(root.querySelectorAll('img')).toHaveLength(1);
        expect(root.querySelector('img')?.getAttribute('src')).toBe('/healthy.webp');
        expect(root.querySelector('.entity-card__collage--count-1')).not.toBeNull();
        root.querySelector('img')?.dispatchEvent(new Event('error'));
        fixture.detectChanges();
        expect(root.querySelector('img')).toBeNull();
        expect(root.querySelector('.entity-card__placeholder-icon')).not.toBeNull();
    });
});

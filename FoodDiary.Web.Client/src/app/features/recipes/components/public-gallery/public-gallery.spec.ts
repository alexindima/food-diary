import { TestBed } from '@angular/core/testing';
import { FdUiDialogService, FdUiImagePreviewDialogComponent } from 'fd-ui-kit';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { PublicRecipeGalleryComponent } from './public-gallery';

describe('PublicRecipeGalleryComponent', () => {
    it('selects thumbnails, previews all photos and resets a removed selection', () => {
        const dialogs = { open: vi.fn() };
        TestBed.configureTestingModule({ providers: [provideTranslateTesting(), { provide: FdUiDialogService, useValue: dialogs }] });
        const fixture = TestBed.createComponent(PublicRecipeGalleryComponent);
        fixture.componentRef.setInput('images', ['/first.jpg', '/second.jpg']);
        fixture.componentRef.setInput('name', 'Soup');
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        element.querySelectorAll<HTMLButtonElement>('.gallery-thumbs button')[1].click();
        fixture.detectChanges();
        expect(element.querySelector('.gallery-cover img')?.getAttribute('src')).toBe('/second.jpg');
        expect(element.querySelectorAll('.gallery-thumbs button')[1].getAttribute('aria-pressed')).toBe('true');
        element.querySelector<HTMLButtonElement>('.gallery-cover')?.click();
        expect(dialogs.open).toHaveBeenCalledWith(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            data: {
                collageImages: [
                    { url: '/first.jpg', alt: 'Soup' },
                    { url: '/second.jpg', alt: 'Soup' },
                ],
                alt: 'Soup',
                title: 'Soup',
            },
        });
        fixture.componentRef.setInput('images', ['/replacement.jpg']);
        fixture.detectChanges();
        expect(element.querySelector('.gallery-cover img')?.getAttribute('src')).toBe('/replacement.jpg');
        expect(element.querySelector('.gallery-thumbs')).toBeNull();
        fixture.componentRef.setInput('images', []);
        fixture.detectChanges();
        expect(element.querySelector('.gallery-cover')).toBeNull();
        expect(element.querySelector('.gallery-placeholder')).not.toBeNull();
    });
});

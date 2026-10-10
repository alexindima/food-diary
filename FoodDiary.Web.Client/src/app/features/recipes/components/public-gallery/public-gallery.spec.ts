import { OverlayContainer } from '@angular/cdk/overlay';
import { DOCUMENT } from '@angular/common';
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
        fixture.componentRef.setInput('aspect', 'wide');
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('.gallery-cover-wide')).not.toBeNull();
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
                initialIndex: 1,
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

describe('Public recipe gallery preview integration', () => {
    it('opens the selected thumbnail in the real preview dialog', async () => {
        TestBed.configureTestingModule({ providers: [provideTranslateTesting()] });
        const viewport = TestBed.inject(DOCUMENT).defaultView;
        if (viewport === null) {
            throw new Error('Gallery preview requires a browser window');
        }
        const matchMediaDescriptor = Object.getOwnPropertyDescriptor(viewport, 'matchMedia');
        Object.defineProperty(viewport, 'matchMedia', { configurable: true, value: vi.fn(() => ({ matches: false })) });
        try {
            const fixture = TestBed.createComponent(PublicRecipeGalleryComponent);
            fixture.componentRef.setInput('images', ['/first.jpg', '/second.jpg']);
            fixture.componentRef.setInput('name', 'Soup');
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            host.querySelectorAll<HTMLButtonElement>('.gallery-thumbs button')[1].click();
            fixture.detectChanges();
            host.querySelector<HTMLButtonElement>('.gallery-cover')?.click();
            fixture.detectChanges();
            await fixture.whenStable();
            const overlay = TestBed.inject(OverlayContainer).getContainerElement();
            expect(overlay.querySelector('fd-ui-image-preview-dialog img')?.getAttribute('src')).toBe('/second.jpg');
        } finally {
            if (matchMediaDescriptor === undefined) {
                Reflect.deleteProperty(viewport, 'matchMedia');
            } else {
                Object.defineProperty(viewport, 'matchMedia', matchMediaDescriptor);
            }
        }
    });
});
